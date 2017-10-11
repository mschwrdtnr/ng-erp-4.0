﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Master40.DB.Data.Context;
using Master40.DB.Enums;
using Master40.DB.Models;
using Microsoft.EntityFrameworkCore;
using MathNet.Numerics.Statistics;

namespace Master40.Tools.Simulation
{
    public static class CalculateKpis
    {
        /// <summary>
        /// calls all implemented Kpi-calculating methods
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        /// <param name="simulationType"></param>
        /// <param name="simulationNumber"></param>
        /// <param name="final"></param>
        /// <param name="time"></param>
        public static void CalculateAllKpis(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber, bool final, int time = 0)
        {
            CalculateLeadTime(context, simulationId,  simulationType,  simulationNumber, final, time);
            CalculateMachineUtilization(context, simulationId,  simulationType,  simulationNumber, final, time);
            CalculateTimeliness(context,simulationId,  simulationType,  simulationNumber, final, time);
            ArticleStockEvolution(context, simulationId, simulationType, simulationNumber, final, time);
            CalculateLayTimes(context, simulationId, simulationType, simulationNumber, final, time);
        }

        public static void CalculateLayTimes(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber, bool final, int time)
        {
            var simConfig = context.SimulationConfigurations.Single(a => a.Id == simulationId);
            var stockEvoLutionsContext = context.StockExchanges.Include(x => x.Stock).ThenInclude(x => x.Article).ToList();
            //.Where(x => x.SimulationType == simulationType && x.SimulationConfigurationId == simulationId && x.SimulationNumber == simulationNumber);
            if (!final)
                stockEvoLutionsContext = stockEvoLutionsContext
                    .Where(a => a.Time >= time - simConfig.DynamicKpiTimeSpan).ToList();
            var stockEvoLutions = stockEvoLutionsContext.Select(x => new { x.ExchangeType,
                                                                           x.Time,
                                                                           x.Stock.Article.Name,
                                                                           x.Stock.Article.Price,
                                                                           x.Quantity})
                                                        .OrderBy(x => x.Time).ThenByDescending(x => x.ExchangeType).ToList();

            var articles = context.Articles.Include(a => a.Stock).ToList();
            foreach (var article in articles) //.Where(x => x.Name.Equals("Dump-Truck")))
            {
                var laytimeList = new List<double>();
                Queue<dynamic> inserts = new Queue<dynamic>(stockEvoLutions
                                                        .Where(a => a.ExchangeType == ExchangeType.Insert 
                                                                 && a.Name == article.Name)
                                                        .OrderBy(t => t.Time).ToList());
                Queue<dynamic> withdrawls = new Queue<dynamic>(stockEvoLutions
                                                        .Where(a => a.ExchangeType == ExchangeType.Withdrawal
                                                                 && a.Name == article.Name)
                                                        .OrderBy(t => t.Time).ToList());

                decimal restCount = 0
                      , insertAmount = 0;

                while (inserts.Any() && withdrawls.Any())
                {
                    var insert = inserts.Dequeue();
                    insertAmount = insert.Quantity;
                    while (insertAmount > 0 && withdrawls.Any())
                    {
                        var withdrawl = withdrawls.Dequeue();
                        var requiredAmount = withdrawl.Quantity + restCount;
                        restCount = insertAmount - requiredAmount;
                        if (restCount >= 0)
                        {
                            for (int i = 0; i < requiredAmount; i++)
                            {
                                if (insert.Time > simConfig.SettlingStart) laytimeList.Add(withdrawl.Time - insert.Time);
                            }
                            insertAmount = restCount;
                        } else
                        {
                            for (int i = 0; i < insertAmount; i++)
                            {
                                if (insert.Time > simConfig.SettlingStart) laytimeList.Add(withdrawl.Time - insert.Time);
                            }
                            restCount = restCount * -1;
                        }
                    }
                }

                if (!laytimeList.Any()) continue;
                var stat = laytimeList.FiveNumberSummary();
                context.Kpis.Add(new Kpi()
                {
                    Name = article.Name,
                    Value = Math.Round(stat[2], 2),
                    ValueMin = Math.Round(stat[0], 2),
                    ValueMax = Math.Round(stat[4], 2),
                    IsKpi = true,
                    KpiType = KpiType.LayTime,
                    SimulationConfigurationId = simulationId,
                    SimulationType = simulationType,
                    SimulationNumber = simulationNumber,
                    Time = time,
                    IsFinal = final
                });
                var interQuantileRange = stat[3] - stat[1];
                var upperFence = stat[3] + 1.5 * interQuantileRange;
                var lowerFence = stat[1] - 1.5 * interQuantileRange;

                // cut them from the sample
                var relevantItems = stat.Where(x => x >= lowerFence && x <= upperFence);

                // BoxPlot: without bounderys
                stat = relevantItems.FiveNumberSummary();

                foreach (var item in stat)
                {
                    context.Kpis.Add(new Kpi()
                    {
                        Name = article.Name,
                        Value = Math.Round(item, 2),
                        ValueMin = 0,
                        ValueMax = 0,
                        IsKpi = false,
                        KpiType = KpiType.LayTime,
                        SimulationConfigurationId = simulationId,
                        SimulationType = simulationType,
                        SimulationNumber = simulationNumber,
                        IsFinal = final
                    });
                }
            }
            context.SaveChanges();
        }

        /// <summary>
        /// must be called only after filling SimulationSchedules!
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        /// <param name="simulationType"></param>
        /// <param name="simulationNumber"></param>
        /// <param name="final"></param>
        /// <param name="time"></param>
        public static void CalculateLeadTime(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber, bool final, int time)
        {
            var simConfig = context.SimulationConfigurations.Single(a => a.Id == simulationId);
            //calculate lead times for each product
            var finishedProducts = final ? context.SimulationWorkschedules.Where(a => a.ParentId.Equals("[]") && a.Start > simConfig.SettlingStart).ToList()
                                         : context.SimulationWorkschedules.Where(a => a.ParentId.Equals("[]") && a.Start >= time - simConfig.DynamicKpiTimeSpan && a.End <= time - simConfig.DynamicKpiTimeSpan).ToList();
            var leadTimes = (from product in finishedProducts
                let endTime = product.End
                let startTime = context.GetEarliestStart(context, product, simulationType)
                select new Kpi()
                {
                    Value = endTime - startTime,
                    Name = product.Article,
                    IsFinal = final
                }).ToList();


            var products = leadTimes.Select(x => x.Name).Distinct();
            var leadTimesBoxPlot = new List<Kpi>();
            //calculate Average per article

            foreach (var product in products)
            {
                var relevantItems = leadTimes.Where(x => x.Name.Equals(product)).Select(x => x.Value);
                // BoxPlot: without bounderys
                var stat = relevantItems.FiveNumberSummary();
                leadTimesBoxPlot.Add(new Kpi()
                {
                    Name = product,
                    Value = Math.Round(stat[2], 2),
                    ValueMin = Math.Round(stat[0], 2),
                    ValueMax = Math.Round(stat[4], 2),
                    IsKpi = final,
                    KpiType = KpiType.LeadTime,
                    SimulationConfigurationId = simulationId,
                    SimulationType = simulationType,
                    SimulationNumber = simulationNumber,
                    Time = time,
                    IsFinal = final
                });

                // Recalculation for Diagram with cut of outliners
                // calculate bounderies using lower mild fence (1,5) -> source: http://www.statisticshowto.com/upper-and-lower-fences/
                var interQuantileRange = stat[3] - stat[1];
                var uperFence = stat[3] + 1.5 * interQuantileRange;
                var lowerFence = stat[1] - 1.5 * interQuantileRange;

                // cut them from the sample
                relevantItems = relevantItems.Where(x => x > lowerFence && x < uperFence);

                // BoxPlot: without bounderys
                stat = relevantItems.FiveNumberSummary();

                // Drop to Database
                foreach (var item in stat)
                {
                    leadTimesBoxPlot.Add(new Kpi()
                    {
                        Name = product,
                        Value = Math.Round(item, 2),
                        ValueMin = 0,
                        ValueMax = 0,
                        IsKpi = false,
                        KpiType = KpiType.LeadTime,
                        SimulationConfigurationId = simulationId,
                        SimulationType = simulationType,
                        SimulationNumber = simulationNumber,
                        IsFinal = final
                    });
                }
            }
            context.Kpis.AddRange(leadTimesBoxPlot);
            context.SaveChanges();

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        /// <param name="simulationType"></param>
        /// <param name="simulationNumber"></param>
        /// <param name="final"></param>
        /// <param name="time"></param>
        public static void CalculateMachineUtilization(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber, bool final, int time)
        {
            var simConfig = context.SimulationConfigurations.Single(a => a.Id == simulationId);
            
            //get working time
            var content = final
                ? context.SimulationWorkschedules.Where(a => a.Start > simConfig.SettlingStart).Select(x => new {x.Start, x.End, x.Machine}).ToList()
                : context.SimulationWorkschedules.Where(a => a.Start >= time - simConfig.DynamicKpiTimeSpan && a.End <= time)
                    .Select(x => new {x.Start, x.End, x.Machine}).ToList();
            //get SimulationTime
            var simulationTime = final ? context.SimulationWorkschedules.Max(a => a.End) - simConfig.SettlingStart
                                       : simConfig.DynamicKpiTimeSpan;

            var kpis = content.GroupBy(x => x.Machine).Select(g => new Kpi() {
                                        Value = Math.Round((double)(g.Sum(x => x.End) - g.Sum(x => x.Start)) / simulationTime, 2),
                                        Name = g.Key,
                                        IsKpi = final,
                                        KpiType = KpiType.MachineUtilization,
                                        SimulationConfigurationId = simulationId,
                                        SimulationType = simulationType,
                                        SimulationNumber = simulationNumber,
                                        Time = time,
                                        IsFinal = final
                                    }).ToList();
            context.Kpis.AddRange(kpis);
            context.SaveChanges();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        /// <param name="simulationType"></param>
        /// <param name="simulationNumber"></param>
        /// <param name="final"></param>
        /// <param name="time"></param>
        public static void CalculateTimeliness(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber, bool final, int time)
        {
            var simConfig = context.SimulationConfigurations.Single(a => a.Id == simulationId);
            var orderTimeliness = final ? context.Orders.Where(a => a.State == State.Finished && a.CreationTime > simConfig.SettlingStart)
                                            .Select(x => new {x.Name, x.FinishingTime, x.DueTime}).ToList()
                                        : context.Orders.Where(a => a.State == State.Finished && a.FinishingTime >= time - simConfig.DynamicKpiTimeSpan)
                                            .Select(x => new { x.Name, x.FinishingTime, x.DueTime }).ToList();
            if (!orderTimeliness.Any()) return;
            var kpis = orderTimeliness.GroupBy(g => g.Name).Select(o => new Kpi()
            {
                Name = o.Key,
                Value = Math.Round((o.Count(x => (x.DueTime - x.FinishingTime) > 0) / (double)o.Count()), 2),
                ValueMin = Math.Round((double)o.Min(m => m.FinishingTime), 2),
                ValueMax = Math.Round((double)o.Max(n => n.FinishingTime), 2),
                Count = o.Count(c => c.Name == o.Key),
                IsKpi = final,
                KpiType = KpiType.Timeliness,
                SimulationConfigurationId = simulationId,
                SimulationType = simulationType,
                SimulationNumber = simulationNumber,
                Time = time,
                IsFinal = final
            }).ToList();
            
            context.Kpis.AddRange(kpis);
            context.SaveChanges();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        /// <param name="simulationType"></param>
        /// <param name="simulationNumber"></param>
        /// <param name="final"></param>
        /// <param name="time"></param>
        public static void ArticleStockEvolution(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber, bool final, int time)
        {
            var simConfig = context.SimulationConfigurations.Single(a => a.Id == simulationId);
            var stockEvoLutionsContext = context.StockExchanges.Include(x => x.Stock).ThenInclude(x => x.Article).ToList();
                //.Where(x => x.SimulationType == simulationType && x.SimulationConfigurationId == simulationId && x.SimulationNumber == simulationNumber);
            if (!final)
                stockEvoLutionsContext = stockEvoLutionsContext.Where(a => a.Time >= time - simConfig.DynamicKpiTimeSpan).ToList();
            var stockEvoLutions = stockEvoLutionsContext.Select(x => new { x.ExchangeType,
                                                                           x.Time,
                                                                           x.Stock.Article.Name,
                                                                           x.Stock.Article.Price,
                                                                           x.Stock.StartValue,
                                                                           x.Quantity }).OrderBy(x => x.Time).ThenByDescending(x => x.ExchangeType);
            var articles = stockEvoLutions.Select(x => new { x.Name }).Distinct();

            var kpis = new List<Kpi>();


            foreach (var item in articles)
            {
                var value = 0.0;
                var lastKpi = new Kpi();
                var articleEvolutions = stockEvoLutions.Where(x => x.Name.Equals(item.Name));
                foreach (var articleEvolution in articleEvolutions)
                {
                    value = (articleEvolution.ExchangeType == ExchangeType.Insert)
                        ? value + (double)articleEvolution.Quantity
                        : value - (double)articleEvolution.Quantity;

                    if ((int)lastKpi.Count == articleEvolution.Time)
                    {
                        lastKpi.Value = value;
                    } else { 
                        lastKpi = new Kpi
                        {
                            Name = articleEvolution.Name,
                            Value = Math.Round(Convert.ToDouble(value), 2),
                            ValueMin = Math.Round(Convert.ToDouble(value * articleEvolution.Price), 2),
                            ValueMax = 0,
                            Count = Convert.ToDouble(articleEvolution.Time),
                            IsKpi = final,
                            KpiType = KpiType.StockEvolution,
                            SimulationConfigurationId = simulationId,
                            SimulationType = simulationType,
                            SimulationNumber = simulationNumber,
                            Time = time,
                            IsFinal = final
                        };
                        kpis.Add(lastKpi);
                    }
                }
            }

            context.Kpis.AddRange(kpis);
            context.SaveChanges();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        /// <param name="simulationType"></param>
        /// <param name="simulationNumber"></param>
        public static void MachineSattleTime(ProductionDomainContext context, int simulationId,
            SimulationType simulationType, int simulationNumber)
        {
            var simConfig = context.SimulationConfigurations.Single(a => a.Id == simulationId);
            var ts = simConfig.DynamicKpiTimeSpan;
            for (var i = ts; i < simConfig.SimulationEndTime; i=i+ts)
            {
                CalculateMachineUtilization(context, simulationId, simulationType, simulationNumber, false, i);
            }
        }
        public static Task ConsolidateRuns(MasterDBContext context, int simulationId)
        {
            var task = Task.Run(() =>
            {
                ConsolidateMachineWorkload(context, simulationId, SimulationType.Decentral);
                ConsolidateLeadTime(context, simulationId, SimulationType.Decentral);
                ConsolidateTimeliness(context, simulationId, SimulationType.Decentral);
                ConsolidateLayTime(context, simulationId, SimulationType.Decentral);
            });
            return task;
        }
        private static void ConsolidateMachineWorkload(MasterDBContext context, int simulationId, SimulationType simType)
        {

                var kpis = context.Kpis.Where(x => x.SimulationConfigurationId == simulationId
                                                   && x.SimulationType == simType
                                                   && x.KpiType == KpiType.MachineUtilization
                                                   && x.IsKpi && x.IsFinal).ToList();
                var machines = kpis.Select(x => x.Name).Distinct();
                var summary = (from machine in machines
                    let machineValues = kpis.Where(x => x.Name == machine).ToList()
                    select new Kpi()
                    {
                        Name = machine,
                        Value = Math.Round(machineValues.Sum(x => x.Value) / machineValues.Count, 2),
                        ValueMin = 0,
                        ValueMax = 0,
                        IsKpi = true,
                        KpiType = KpiType.MachineUtilization,
                        SimulationConfigurationId = simulationId,
                        SimulationType = simType,
                        SimulationNumber = 0,
                        IsFinal = true
                    }).ToList();

                context.Kpis.AddRange(summary);
                context.SaveChanges();
        }
        private static void ConsolidateTimeliness(MasterDBContext context, int simulationId, SimulationType simType)
        {

            var kpis = context.Kpis.Where(x => x.SimulationConfigurationId == simulationId
                                               && x.SimulationType == simType
                                               && x.KpiType == KpiType.Timeliness
                                               && x.IsKpi && x.IsFinal).ToList();
            var articles = kpis.Select(x => x.Name).Distinct();
            var summary = (from article in articles
                           let articleValues = kpis.Where(x => x.Name == article).ToList()
                select new Kpi()
                {
                    Name = article,
                    Value = Math.Round(articleValues.Sum(x => x.Value) / articleValues.Count, 2),
                    ValueMin = Math.Round(articleValues.Sum(x => x.Value) / articleValues.Count, 2),
                    ValueMax = Math.Round(articleValues.Sum(x => x.Value) / articleValues.Count, 2),
                    IsKpi = true,
                    KpiType = KpiType.Timeliness,
                    SimulationConfigurationId = simulationId,
                    SimulationType = simType,
                    SimulationNumber = 0,
                    IsFinal = true,
                }).ToList();

            context.Kpis.AddRange(summary);
            context.SaveChanges();
        }
        private static void ConsolidateLeadTime(MasterDBContext context, int simulationId, SimulationType simType)
        {
            var kpis = context.Kpis.Where(x => x.SimulationConfigurationId == simulationId
                                                && x.SimulationType == simType
                                                && x.KpiType == KpiType.LeadTime
                                                && x.IsFinal).ToList();
            var articles = kpis.Select(x => x.Name).Distinct();
            var summary = (from article in articles
                let articleValues = kpis.Where(x => x.Name == article && x.IsKpi).ToList()
                select new Kpi()
                {
                    Name = article,
                    Value = Math.Round(articleValues.Sum(x => x.Value) / articleValues.Count, 2),
                    ValueMin = Math.Round(articleValues.Sum(x => x.ValueMin) / articleValues.Count, 2),
                    ValueMax = Math.Round(articleValues.Sum(x => x.ValueMax) / articleValues.Count, 2),
                    IsKpi = true,
                    KpiType = KpiType.LeadTime,
                    SimulationConfigurationId = simulationId,
                    SimulationType = simType,
                    SimulationNumber = 0
                }).ToList();
            context.Kpis.AddRange(summary);
            context.SaveChanges();


            foreach (var article in articles)
            {
                var simulation = kpis.Where(x => x.Name == article && !x.IsKpi).Select(x => x.SimulationNumber).Distinct().ToList();
                var plots = new List<List<double>>();
                for (int i = 0; i < 5; i++)  plots.Add(new List<double>());
                    
                foreach (var run in simulation)
                {
                    var thisRun = kpis.Where(x => x.Name == article && !x.IsKpi && x.SimulationNumber == run).OrderBy(x => x.Value).ToList();
                    plots[0].Add(thisRun[0].Value);
                    plots[1].Add(thisRun[1].Value);
                    plots[2].Add(thisRun[2].Value);
                    plots[3].Add(thisRun[3].Value);
                    plots[4].Add(thisRun[4].Value);
                }

                foreach (var plot in plots)
                {
                    context.Kpis.Add(
                            new Kpi()
                            {
                                Name = article,
                                Value = Math.Round(plot.Sum() / plot.Count, 2),
                                ValueMin = 0,
                                ValueMax = 0,
                                IsKpi = true,
                                KpiType = KpiType.LeadTime,
                                SimulationConfigurationId = simulationId,
                                SimulationType = simType,
                                SimulationNumber = 0
                            });
                }
            }
            context.SaveChanges();
        }
        private static void ConsolidateLayTime(MasterDBContext context, int simulationId, SimulationType simType)
        {
            var kpis = context.Kpis.Where(x => x.SimulationConfigurationId == simulationId
                                               && x.SimulationType == simType
                                               && x.KpiType == KpiType.LayTime
                                               && x.IsFinal).ToList();
            var articles = kpis.Select(x => x.Name).Distinct();
            var summary = (from article in articles
                let articleValues = kpis.Where(x => x.Name == article && x.IsKpi).ToList()
                select new Kpi()
                {
                    Name = article,
                    Value = Math.Round(articleValues.Sum(x => x.Value) / articleValues.Count, 2),
                    ValueMin = Math.Round(articleValues.Sum(x => x.ValueMin) / articleValues.Count, 2),
                    ValueMax = Math.Round(articleValues.Sum(x => x.ValueMax) / articleValues.Count, 2),
                    IsKpi = true,
                    KpiType = KpiType.LayTime,
                    SimulationConfigurationId = simulationId,
                    SimulationType = simType,
                    SimulationNumber = 0,
                    IsFinal = true
                }).ToList();
            context.Kpis.AddRange(summary);
            context.SaveChanges();


            foreach (var article in articles)
            {
                var simulation = kpis.Where(x => x.Name == article && !x.IsKpi).Select(x => x.SimulationNumber).Distinct().ToList();
                var plots = new List<List<double>>();
                for (int i = 0; i < 5; i++) plots.Add(new List<double>());

                foreach (var run in simulation)
                {
                    var thisRun = kpis.Where(x => x.Name == article && !x.IsKpi && x.SimulationNumber == run).OrderBy(x => x.Value).ToList();
                    plots[0].Add(thisRun[0].Value);
                    plots[1].Add(thisRun[1].Value);
                    plots[2].Add(thisRun[2].Value);
                    plots[3].Add(thisRun[3].Value);
                    plots[4].Add(thisRun[4].Value);
                }

                foreach (var plot in plots)
                {
                    context.Kpis.Add(
                        new Kpi()
                        {
                            Name = article,
                            Value = Math.Round(plot.Sum() / plot.Count, 2),
                            ValueMin = 0,
                            ValueMax = 0,
                            IsKpi = true,
                            KpiType = KpiType.LayTime,
                            SimulationConfigurationId = simulationId,
                            SimulationType = simType,
                            SimulationNumber = 0,
                            IsFinal = true
                        });
                }
            }
            context.SaveChanges();
        }
    }
}

