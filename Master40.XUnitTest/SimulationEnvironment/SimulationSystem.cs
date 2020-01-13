﻿using System;
using Akka.Actor;
using Akka.TestKit.Xunit;
using Master40.DB.Data.Context;
using Master40.DB.Data.Initializer;
using Master40.DB.Enums;
using Master40.Simulation.CLI;
using Master40.SimulationCore;
using Master40.SimulationCore.Environment.Options;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLitePCL;
using Xunit;

namespace Master40.XUnitTest.SimulationEnvironment
{
    public class SimulationSystem : TestKit
    {
        private string localresultdb = "Server=(localdb)\\mssqllocaldb;Database=TestResultContext;Trusted_Connection=True;MultipleActiveResultSets=true";
        private const string testResultCtxString = "Server=(localdb)\\mssqllocaldb;Database=TestResultContext;Trusted_Connection=True;MultipleActiveResultSets=true";
        private const string masterResultCtxString = "Server=(localdb)\\mssqllocaldb;Database=Master40Results;Trusted_Connection=True;MultipleActiveResultSets=true";

        ProductionDomainContext _ctx = new ProductionDomainContext(options: new DbContextOptionsBuilder<MasterDBContext>()
                                                            .UseInMemoryDatabase(databaseName: "InMemoryDB")
                                                            .Options);

        ProductionDomainContext _masterDBContext = new ProductionDomainContext(options: new DbContextOptionsBuilder<MasterDBContext>()
            .UseSqlServer(connectionString: "Server=(localdb)\\mssqllocaldb;Database=TestContext;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options);

        private ResultContext _ctxResult = ResultContext.GetContext(resultCon:
            "Server=(localdb)\\mssqllocaldb;Database=TestResultContext;Trusted_Connection=True;MultipleActiveResultSets=true");

        // new ResultContext(options: new DbContextOptionsBuilder<ResultContext>()
        // .UseInMemoryDatabase(databaseName: "InMemoryResults")
        // .Options);
        // 
        public SimulationSystem()
        {
            // _masterDBContext.Database.EnsureDeleted();
            // _masterDBContext.Database.EnsureCreated();
            // //MasterDbInitializerTable.DbInitialize(_masterDBContext);
            // MasterDBInitializerTruck.DbInitialize(context: _masterDBContext);

            // _ctxResult.Database.EnsureDeleted();
            // _ctxResult.Database.EnsureCreated();
            // ResultDBInitializerBasic.DbInitialize(context: _ctxResult);

        }

        //[Fact(Skip = "manual test")]
        [Theory]
        [InlineData(testResultCtxString)]
        [InlineData(masterResultCtxString)]
        public void ResetResultsDB(string connectionString)
        
        {
            ResultContext masterResults = ResultContext.GetContext(resultCon: connectionString);
            masterResults.Database.EnsureDeleted();
            masterResults.Database.EnsureCreated();

            _masterDBContext.Database.EnsureDeleted();
            _masterDBContext.Database.EnsureCreated();
            //MasterDbInitializerTable.DbInitialize(_masterDBContext);
            MasterDBInitializerTruck.DbInitialize(context: _masterDBContext);
        }

        [Theory]
        //[InlineData(SimulationType.None)]
        //[InlineData(SimulationType.DefaultSetup, 0, 60)]
        [InlineData(SimulationType.DefaultSetupStack, 1, 60)]
        //[InlineData(SimulationType.BucketScope, 3, 120)]
        //[InlineData(SimulationType.BucketScope, 4, 150)]
        //[InlineData(SimulationType.BucketScope, 5, 180)]
        //[InlineData(SimulationType.BucketScope, 6, 240)]
        //[InlineData(SimulationType.BucketScope, 7, 300)]
        //[InlineData(SimulationType.BucketScope, 8, 360)]
        //[InlineData(SimulationType.BucketScope, 9, 420)]
        //[InlineData(SimulationType.BucketScope, 10, 480)]
        [InlineData(SimulationType.BucketScope, 2, 600)]
        [InlineData(SimulationType.BucketScope, 3, 720)]
        [InlineData(SimulationType.BucketScope, 4, 840)]
        [InlineData(SimulationType.BucketScope, 5, 960)]
        [InlineData(SimulationType.BucketScope, 6, 1080)]
        [InlineData(SimulationType.BucketScope, 7, 1200)]
        [InlineData(SimulationType.BucketScope, 8, 1320)]
        [InlineData(SimulationType.BucketScope, 9, 1440)]
        [InlineData(SimulationType.BucketScope, 10, 1920)]
        [InlineData(SimulationType.BucketScope, 11, 2400)]
        [InlineData(SimulationType.BucketScope, 12, 2880)]
        [InlineData(SimulationType.BucketScope, 99, Int32.MaxValue)]
        public async Task SystemTestAsync(SimulationType simulationType, int simNr, int maxBucketSize)
        {
            //InMemoryContext.LoadData(source: _masterDBContext, target: _ctx);
            var simContext = new AgentSimulation(DBContext: _masterDBContext, messageHub: new ConsoleHub());

            var simConfig = SimulationCore.Environment.Configuration.Create(args: new object[]
                                                {
                                                    // set ResultDBString and set SaveToDB true
                                                    new DBConnectionString(value: localresultdb)
                                                    , new SimulationId(value: 1)
                                                    , new SimulationNumber(value: simNr)
                                                    , new SimulationKind(value: simulationType) // implements the used behaviour, if None --> DefaultBehaviour
                                                    , new OrderArrivalRate(value: 0.025)
                                                    , new OrderQuantity(value: Int32.MaxValue)
                                                    , new TransitionFactor(value: 3)
                                                    , new EstimatedThroughPut(value: 1920)
                                                    , new DebugAgents(value: false)
                                                    , new DebugSystem(value: false)
                                                    , new KpiTimeSpan(value: 480)
                                                    , new MaxBucketSize(value: maxBucketSize)
                                                    , new Seed(value: 1337)
                                                    , new MinDeliveryTime(value: 1440)
                                                    , new MaxDeliveryTime(value: 2880)
                                                    , new TimePeriodForThrougputCalculation(value: 3840)
                                                    , new SettlingStart(value: 4320)
                                                    , new SimulationEnd(value: 10080)
                                                    , new WorkTimeDeviation(value: 0.2)
                                                    , new SaveToDB(value: true)
                                                });

            var simulation = await simContext.InitializeSimulation(configuration: simConfig);

            emtpyResultDBbySimulationNumber(simNr: simConfig.GetOption<SimulationNumber>());


            var simWasReady = false;
            if (simulation.IsReady())
            {
                // set for Assert 
                simWasReady = true;
                // Start simulation
                var sim = simulation.RunAsync();

                AgentSimulation.Continuation(inbox: simContext.SimulationConfig.Inbox
                                            , sim: simulation
                                            , collectors: new List<IActorRef> { simContext.StorageCollector
                                                                    , simContext.WorkCollector
                                                                    , simContext.ContractCollector
                                            });
                await sim;
            }

            Assert.True(condition: simWasReady);
        }

        private void emtpyResultDBbySimulationNumber(SimulationNumber simNr)
        {
            var _simNr = simNr;
            using (_ctxResult)
            {
                _ctxResult.RemoveRange(entities: _ctxResult.SimulationJobs.Where(predicate: a => a.SimulationNumber.Equals(_simNr.Value)));
                _ctxResult.RemoveRange(entities: _ctxResult.Kpis.Where(predicate: a => a.SimulationNumber.Equals(_simNr.Value)));
                _ctxResult.RemoveRange(entities: _ctxResult.StockExchanges.Where(predicate: a => a.SimulationNumber.Equals(_simNr.Value)));
                _ctxResult.SaveChanges();
            }
        }
    }
}
