﻿using Akka.Actor;
using Master40.DB.Enums;
using Master40.SimulationCore.Agents.HubAgent;
using Master40.SimulationCore.Agents.ResourceAgent.Types;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Types;
using System;
using System.Linq;
using Master40.DB.DataModel;
using static FOperationResults;
using static FPostponeds;
using static FProposals;
using static FUpdateSimulationWorks;
using static FUpdateStartConditions;
using static IJobResults;
using static IJobs;
using static FOperations;
using static FCreateSimulationResourceSetups;
using static FResourceInformations;

namespace Master40.SimulationCore.Agents.ResourceAgent.Behaviour
{
    public class DefaultSetup : SimulationCore.Types.Behaviour
    {
        public DefaultSetup(int planingJobQueueLength, int fixedJobQueueSize, WorkTimeGenerator workTimeGenerator, int resourceId, ToolManager toolManager, SimulationType simulationType = SimulationType.None) : base(childMaker: null, simulationType: simulationType)
        {
            this._processingQueue = new JobQueueItemLimited(limit: fixedJobQueueSize);
            this._planingQueue = new JobQueueTimeLimited(limit: planingJobQueueLength);
            this._agentDictionary = new AgentDictionary();
            _workTimeGenerator = workTimeGenerator;
            _resourceId = resourceId;
            _toolManager = toolManager;
        }
        // TODO Implement a JobManager
        internal JobQueueTimeLimited _planingQueue { get; set; }
        internal JobQueueItemLimited _processingQueue { get; set; }
        internal JobInProgress _jobInProgress { get; set; } = new JobInProgress();
        internal ToolManager _toolManager { get; }
        internal WorkTimeGenerator _workTimeGenerator { get; }
        internal int _resourceId { get; }
        internal AgentDictionary _agentDictionary { get; }

        public override bool Action(object message)
        {
            switch (message)
            {
                case Resource.Instruction.SetHubAgent msg: SetHubAgent(hubAgent: msg.GetObjectFromMessage.Ref); break;
                case Resource.Instruction.RequestProposal msg: RequestProposal(jobItem: msg.GetObjectFromMessage); break;
                case Resource.Instruction.AcknowledgeProposal msg: AcknowledgeProposal(jobItem: msg.GetObjectFromMessage); break;
                case BasicInstruction.UpdateStartConditions msg: UpdateStartCondition(startCondition: msg.GetObjectFromMessage); break;
                case BasicInstruction.FinishJob msg: FinishJob(jobResult: msg.GetObjectFromMessage); break;
                // case BasicInstruction.ResourceBrakeDown msg: BreakDown((Resource)agent, msg.GetObjectFromMessage); break;
                default: return false;
            }
            return true;
        }

        public override bool AfterInit()
        {
            Agent.Send(instruction: Hub.Instruction.AddResourceToHub.Create(message: new FResourceInformation(resourceSetups: _toolManager.GetAllSetups()
                , requiredFor: Agent.Name, @ref: Agent.Context.Self), target: Agent.VirtualParent));
            return true;
        }

        /// <summary>
        /// Register the Resource in the System on Startup and Save the Hub agent.
        /// </summary>
        private void SetHubAgent(IActorRef hubAgent)
        {
            // Save to Value Store
            _agentDictionary.Add(key: hubAgent, value: "Default");
            // Debug Message
            Agent.DebugMessage(msg: "Successfully registered resource at : " + hubAgent.Path.Name);
        }

        /// <summary>
        /// Is Called from Hub Agent to get an Proposal when the item with a given priority can be scheduled.
        /// </summary>
        /// <param name="jobItem"></param>
        private void RequestProposal(IJob jobItem)
        {
            Agent.DebugMessage(msg: $"Asked by Hub for Proposal: " + jobItem.Name + " with Id: " + jobItem.Key + ")");

            SendProposalTo(jobItem: jobItem);
        }

        /// <summary>
        /// Send Proposal to Hub Client
        /// </summary>
        /// <param name="jobItem"></param>
        internal void SendProposalTo(IJob jobItem)
        {
            var setupDuration = GetSetupTime(jobItem: jobItem);

            var queuePosition = _planingQueue.GetQueueAbleTime(job: jobItem
                                                     , currentTime: Agent.CurrentTime
                                          , resourceIsBlockedUntil: _jobInProgress.ResourceIsBusyUntil
                                           , processingQueueLength: _processingQueue.SumDurations
                                                   , setupDuration: setupDuration);

            if (queuePosition.IsQueueAble) { 
                Agent.DebugMessage(msg: $"IsQueueable: {queuePosition.IsQueueAble} with EstimatedStart: {queuePosition.EstimatedStart}");
            }
            var fPostponed = new FPostponed(offset: queuePosition.IsQueueAble ? 0 : _planingQueue.Limit);

            if (fPostponed.IsPostponed) { 
                Agent.DebugMessage(msg: $"Postponed: { fPostponed.IsPostponed } with Offset: { fPostponed.Offset} ");
            }
            // calculate proposal
            var proposal = new FProposal(possibleSchedule: queuePosition.EstimatedStart
                , postponed: fPostponed
                , resourceAgent: Agent.Context.Self
                , jobKey: jobItem.Key);

            Agent.Send(instruction: Hub.Instruction.ProposalFromResource.Create(message: proposal, target: Agent.Context.Sender));
        }

       
        /// <summary>
        /// Is called after RequestProposal if the proposal is accepted by HubAgent
        /// </summary>
        public void AcknowledgeProposal(IJob jobItem)
        {
            Agent.DebugMessage(msg: $"Start Acknowledge proposal for: {jobItem.Name} {jobItem.Key}");

            var setupDuration = GetSetupTime(jobItem: jobItem);

            var queuePosition = _planingQueue.GetQueueAbleTime(job: jobItem
                                                     , currentTime: Agent.CurrentTime
                                          , resourceIsBlockedUntil: _jobInProgress.ResourceIsBusyUntil
                                           , processingQueueLength: _processingQueue.SumDurations
                                                   , setupDuration: setupDuration);
            // if not QueueAble
            if (!queuePosition.IsQueueAble)
            {
                Agent.DebugMessage(msg: $"Stop Acknowledge proposal for: {jobItem.Name} {jobItem.Key} and start requeue");
                Agent.Send(instruction: Hub.Instruction.EnqueueJob.Create(message: jobItem, target: jobItem.HubAgent));
                return;
            }


            jobItem = jobItem.UpdateEstimations(queuePosition.EstimatedStart, Agent.Context.Self);
            _planingQueue.Enqueue(item: jobItem);

            Agent.DebugMessage(msg: "AcknowledgeProposal Accepted Item: " + jobItem.Name + " with Id: " + jobItem.Key);
            UpdateAndRequeuePlanedJobs(jobItem: jobItem);
            UpdateProcessingQueue();
            DoWork();
        }

        private void RequeueAllRemainingJobs()
        {
            Agent.DebugMessage(msg: "Start to Requeue all remaining Jobs");
            var item = _planingQueue.jobs.FirstOrDefault();
            if (item != null)
            {
                UpdateAndRequeuePlanedJobs(jobItem: item);
            }
        }


        private void UpdateAndRequeuePlanedJobs(IJob jobItem)
        {
            Agent.DebugMessage(msg: "Old planning queue length = " + _planingQueue.Count);
            var toRequeue = _planingQueue.CutTail(currentTime: Agent.CurrentTime, job: jobItem);
            foreach (var job in toRequeue)
            {
                _planingQueue.RemoveJob(job: job);
                Agent.Send(instruction: Hub.Instruction.EnqueueJob.Create(message: job, target: job.HubAgent));
            }
            Agent.DebugMessage(msg: "New planning queue length = " + _planingQueue.Count);
        }


        private void UpdateProcessingQueue()
        {
            while (_processingQueue.CapacitiesLeft() && _planingQueue.HasQueueAbleJobs())
            {
                var job = _planingQueue.DequeueFirstSatisfied(currentTime: Agent.CurrentTime);
                Agent.DebugMessage(msg: $"Job to place in processingQueue: {job.Key} {job.Name} Try to start processing.");
                var ok = _processingQueue.Enqueue(item: job);
                if (!ok)
                {
                    throw new Exception(message: "Something wen wrong with Queueing!");
                }
                //TODO Withdraw at ProcessingQueue or DoWork?
                Agent.DebugMessage(msg: $"Start withdraw for article {job.Name} {job.Key}");
                Agent.Send(instruction: BasicInstruction.WithdrawRequiredArticles.Create(message: job.Key, target: job.HubAgent));
            }

            Agent.DebugMessage(msg: $"Jobs ready to start: {_processingQueue.Count} Try to start processing.");
        }

        private void UpdateStartCondition(FUpdateStartCondition startCondition)
        {
            Agent.DebugMessage(msg: $"UpdateArticleProvided for article: {startCondition.OperationKey} ArticleProvided: {startCondition.ArticlesProvided} && PreCondition {startCondition.PreCondition}");

            if (_planingQueue.UpdatePreCondition(startCondition: startCondition))
            {
                UpdateProcessingQueue();
                DoWork();
            }

        }

        /// <summary>
        /// Starts the next Job
        /// </summary>
        internal void DoWork()
        {
            if (_jobInProgress.IsSet)
            {
                Agent.DebugMessage(msg: "Im still working....");
                return; // Resource Agent is still working
            }

            var nextJobInProgress = _processingQueue.DequeueFirstSatisfied(currentTime: Agent.CurrentTime);

            // Wait if nothing more to do
            if (nextJobInProgress == null)
            {
                // No more work 
                Agent.DebugMessage(msg: "Nothing more Ready in Queue!");
                return;
            }

            UpdateProcessingQueue();

            _jobInProgress.Set(job: nextJobInProgress, currentTime: Agent.CurrentTime);

            //Start setup if necessary 
            // TODO decide were to find the resourcesSetup - IJob or by casting in method
            // TODO Do all those things in a method 
            var setupDuration = GetSetupTime(nextJobInProgress);

            if (setupDuration > 0)
            {
                Agent.DebugMessage(msg: $"Start with Setup for Job {nextJobInProgress.Name}  Key: {nextJobInProgress.Key} Duration is {setupDuration} and start with Job at {Agent.CurrentTime + setupDuration}");
                _toolManager.Mount(requiredResourceTool: nextJobInProgress.Tool);
                var pubSetup = new FCreateSimulationResourceSetup(workScheduleId: nextJobInProgress.Key.ToString(), duration: setupDuration, start: Agent.CurrentTime, machine: Agent.Name);
                Agent.Context.System.EventStream.Publish(@event: pubSetup);
            }

            //TODO Make a extra method so start Job pops up at the real starting time
            var randomizedWorkDuration = _workTimeGenerator.GetRandomWorkTime(duration: nextJobInProgress.Duration);
            Agent.DebugMessage(msg: $"Starting Job {nextJobInProgress.Name}  Key: {nextJobInProgress.Key} new Duration is {randomizedWorkDuration} total work time is {setupDuration + randomizedWorkDuration}");

            var pub = new FUpdateSimulationWork(workScheduleId: nextJobInProgress.Key.ToString(), duration: randomizedWorkDuration, start: Agent.CurrentTime + setupDuration, machine: Agent.Name);
            Agent.Context.System.EventStream.Publish(@event: pub);

            var fOperationResult = new FOperationResult(key: nextJobInProgress.Key
                                             , creationTime: 0
                                                    , start: Agent.CurrentTime
                                                      , end: Agent.CurrentTime + randomizedWorkDuration + setupDuration
                                         , originalDuration: nextJobInProgress.Duration
                                          , productionAgent: ActorRefs.Nobody
                                            , resourceAgent: Agent.Context.Self);

            Agent.Send(instruction: BasicInstruction.FinishJob.Create(message: fOperationResult, target: Agent.Context.Self), waitFor: randomizedWorkDuration + setupDuration);

        }

        private void FinishJob(IJobResult jobResult)
        {
            Agent.DebugMessage(msg: $"Finished Work with {_jobInProgress.Current.Name} {_jobInProgress.Current.Key} take next...");
            jobResult = jobResult.FinishedAt(Agent.CurrentTime);

            Agent.Send(instruction: BasicInstruction.FinishJob.Create(message: jobResult, target: _jobInProgress.Current.HubAgent));
            _jobInProgress.Reset();

            // then requeue processing queue if the item was delayed 
            if (jobResult.OriginalDuration != Agent.CurrentTime - jobResult.Start)
                RequeueAllRemainingJobs();

            // Do Work
            DoWork();
        }

        private int GetSetupTime(IJob jobItem)
        {
            var setupTime = 0;
            if (!_toolManager.AlreadyEquipped(jobItem.Tool))
            {
                setupTime = _toolManager.GetSetupDurationByTool(jobItem.Tool);
            }

            Agent.DebugMessage(
                msg: $"Has Tool: {_toolManager.GetToolName()} | require Tool: {jobItem.Tool.Name} with setupDuration {setupTime}");
            return setupTime;
        }

        /*
        private void BreakDown(Resource agent, FBreakDown breakDwon)
        {
            if (breakDwon.IsBroken)
            {
                Break(agent, breakDwon);
            }
            else
            {
                RecoverFromBreakDown(agent);
            }

        }

        private void Break(Resource agent, FBreakDown breakdown)
        {
            agent.Set(Resource.Properties.BROKEN, breakdown.IsBroken);
            // requeue all
            var queue = agent.Get<List<FWorkItem>>(Resource.Properties.QUEUE);
            var Processing = agent.Get<LimitedQueue<FWorkItem>>(Resource.Properties.PROCESSING_QUEUE);
            agent.CallToReQueue(Processing, new List<FWorkItem>(Processing));
            agent.CallToReQueue(queue, new List<FWorkItem>(queue));
            // set Self Recovery
            agent.Send(BasicInstruction.ResourceBrakeDown.Create(breakdown.SetIsBroken(false), agent.Context.Self), 1440);
        }

        private void RecoverFromBreakDown(Resource agent)
        {
            agent.Set(Resource.Properties.BROKEN, false);
            agent.Send(Hub.Instruction.AddMachineToHub.Create(new FHubInformation(ResourceType.Machine, agent.Name, agent.Context.Self), agent.VirtualParent, true));
        }

        */
    }
}
