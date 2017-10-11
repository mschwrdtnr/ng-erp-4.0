﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Master40.Agents.Agents;
using Master40.Agents.Agents.Internal;
using Master40.Agents.Agents.Model;
using Master40.DB.Data.Context;
using Master40.DB.Data.Helper;
using Master40.DB.Enums;
using Master40.DB.Models;
using Microsoft.EntityFrameworkCore;
using NSimulate;
using Master40.MessageSystem.SignalR;
using Master40.Tools.Simulation;

namespace Master40.Agents
{
    public class AgentSimulation 
    {
        private readonly ProductionDomainContext _productionDomainContext;
        public static List<SimulationWorkschedule> SimulationWorkschedules;

        private IMessageHub _messageHub;

        public AgentSimulation(ProductionDomainContext productionDomainContext, IMessageHub messageHub)
        {
            _productionDomainContext = productionDomainContext;
            _messageHub = messageHub;
            SimulationWorkschedules = new List<SimulationWorkschedule>();
        }

        public async Task<List<AgentStatistic>> RunSim(int simulationId, int simulationNumber)
        {
            AgentStatistic.Log = new List<string>();
            Debug.WriteLine("Simulation Starts");
            _messageHub.SendToAllClients("Simulation starts...");


            using (var context = new SimulationContext(isDefaultContextForProcess: true))
            {

                // initialise the model
                var system = CreateModel(context: context, simulationId: simulationId, simNr: simulationNumber);

                // instantate a new simulator
                var simulator = new Simulator();

                // run the simulation
                await simulator.SimulateAsync(0);

                // Debug
                Debuglog(context);

                var simulationNr = _productionDomainContext.GetSimulationNumber(simulationId, SimulationType.Decentral);
                Statistics.UpdateSimulationId(simulationId, SimulationType.Decentral, simulationNumber);
                _productionDomainContext.SimulationWorkschedules.AddRange(SimulationWorkschedules);
                _productionDomainContext.SaveChanges();
                SaveStockExchanges(simulationId, simulationNr, context);
                //UpdateStockExchanges(simulationId, simulationNr);

            }
            return Agent.AgentStatistics;
        }

        private object CreateModel(SimulationContext context,int simulationId, int simNr)
        {
            var simConfig = _productionDomainContext.SimulationConfigurations.Single(x => x.Id == simulationId);
            //context.Register(new SimulationEndTrigger(() => (context.TimePeriod > simConfig.SimulationEndTime)));

            var system = new SystemAgent(null, "System", false, _productionDomainContext, _messageHub, simConfig);
            var randomWorkTime = new WorkTimeGenerator(simConfig.Seed, simConfig.WorkTimeDeviation, simNr);
            // Create Directory Agent,
            var directoryAgent = new DirectoryAgent(system, "Directory", false);
            system.ChildAgents.Add(directoryAgent);

            // Create Machine Agents
            foreach (var machine in _productionDomainContext.Machines.Include(m => m.MachineGroup))
            {
                system.ChildAgents.Add(new MachineAgent(creator: system, 
                                                           name: "Machine: " + machine.Name, 
                                                          debug: false, 
                                                 directoryAgent: directoryAgent,
                                                        machine: machine,
                                              workTimeGenerator: randomWorkTime)); 
            }

            // Create Stock Agents
            foreach (var stock in _productionDomainContext.Stocks.AsNoTracking()
                                                          .Include(x => x.StockExchanges)
                                                          .Include(x => x.Article).ThenInclude(x => x.ArticleToBusinessPartners)
                                                                                  .ThenInclude(x => x.BusinessPartner))
            {
                system.ChildAgents.Add(new StorageAgent(creator: system, 
                                                           name: stock.Name, 
                                                          debug: false, 
                                                   stockElement: stock ));
            }

            system.PrepareAgents(simConfig, simNr);
            // Return System Agent to Context
            return system;
        }

        private void Debuglog(SimulationContext context)
        {
            var itemlist = from val in Agent.AgentStatistics
                group val by new { val.AgentType } into grouped
                select new { Agent = grouped.First().AgentType, ProcessingTime = grouped.Sum(x => x.ProcessingTime), Count = grouped.Count().ToString() };

            foreach (var item in itemlist)
            {
                Debug.WriteLine(" Agent (" + Agent.AgentCounter.Count(x => x == item.Agent) + "): " + item.Agent + " -> Runtime: " + item.ProcessingTime + " Milliseconds with " + item.Count + " Instructions Processed");
            }
            foreach (var machine in context.ActiveProcesses.Where(x => x.GetType() == typeof(MachineAgent)))
            {
                var item = ((MachineAgent)machine);
                Debug.WriteLine("Agent " + item.Name + " Queue Length:" + item.Queue.Count);
            }

            var jobs = SimulationWorkschedules.Count;
            Debug.WriteLine(jobs + " Jobs processed in {0} minutes", Agent.AgentStatistics.Max(x => x.Time));
            
            foreach (var stock in context.ActiveProcesses.Where(x => x.GetType() == typeof(StorageAgent)))
            {
                var item = ((StorageAgent)stock);
                var count = item.StockElement.StartValue + (item.StockElement.StockExchanges.Where(x => x.ExchangeType == ExchangeType.Insert)
                                 .Sum(x => x.Quantity) - item.StockElement.StockExchanges
                                 .Where(x => x.ExchangeType == ExchangeType.Withdrawal).Sum(x => x.Quantity));
                Debug.WriteLine("Storage (" + item.Name + "): In: " + count);
            }
           
        }

        private void SaveStockExchanges(int simId, int simNr, SimulationContext context)
        {
            foreach (var stock in context.ActiveProcesses.Where(x => x.GetType() == typeof(StorageAgent)))
            {
                var item = ((StorageAgent)stock);
                var stockExchanges = item.StockElement.StockExchanges.ToList();
                var stockExchangesToAdd = new List<StockExchange>();
                foreach (var se in stockExchanges)
                {
                    var toAdd = se.CopyDbPropertiesWithoutId();
                    toAdd.SimulationType = SimulationType.Decentral;
                    toAdd.SimulationConfigurationId = simId;
                    toAdd.SimulationNumber = simNr;
                    stockExchangesToAdd.Add(toAdd);
                }
                _productionDomainContext.StockExchanges.AddRange(stockExchangesToAdd);
                _productionDomainContext.SaveChanges();
            }
        }

        private void UpdateStockExchanges (int simId, int simNr)
        {
            var stockExchanges = _productionDomainContext.StockExchanges.Where(x => x.SimulationConfigurationId == 0);
            foreach (var se in stockExchanges)
            {
                se.SimulationType = SimulationType.Decentral;
                se.SimulationConfigurationId = simId;
                se.SimulationNumber = simNr;
            }
            _productionDomainContext.SaveChanges();
        }


    }
}