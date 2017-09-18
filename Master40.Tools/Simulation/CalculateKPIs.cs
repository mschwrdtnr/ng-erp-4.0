﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Master40.DB.Data.Context;
using Master40.DB.Enums;
using Master40.DB.Models;


namespace Master40.Tools.Simulation
{
    public static class CalculateKpis
    {
        /// <summary>
        /// calls all implemented Kpi-calculating methods
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        public static void CalculateAllKpis(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber)
        {
            CalculateLeadTime(context, simulationId,  simulationType,  simulationNumber);
            CalculateMachineUtilization(context, simulationId,  simulationType,  simulationNumber);
            CalculateTimeliness(context,simulationId,  simulationType,  simulationNumber);
        }

        /// <summary>
        /// must be called after filling SimulationSchedules!
        /// </summary>
        /// <param name="context"></param>
        /// <param name="simulationId"></param>
        public static void CalculateLeadTime(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber)
        {
            //calculate lead times for each product
            var leadTimes = new List<Kpi>();
            var finishedProducts = context.SimulationWorkschedules.Where(a => a.ParentId.Equals("[]") && a.SimulationConfigurationId == simulationId);
            foreach (var product in finishedProducts )
            {
                var endTime = product.End;
                var startTime = context.GetEarliestStart(context, product, simulationType);
                leadTimes.Add(new Kpi(){
                    Value = endTime - startTime,
                    Name = product.Article
                });
            }
            //calculate Average per article
            var leadTimesAverage = new List<Kpi>();
            while (leadTimes.Any())
            {
                var relevantItems = leadTimes.Where(a => a.Name.Equals(leadTimes.First().Name)).ToList();
                leadTimesAverage.Add(new Kpi()
                {
                    Name = relevantItems.First().Name,
                    Value = relevantItems.Sum(a => a.Value)/relevantItems.Count,
                    IsKpi = true,
                    KpiType = KpiType.LeadTime,
                    SimulationConfigurationId = simulationId,
                    SimulationType = simulationType,
                    SimulationNumber = simulationNumber
                });
                foreach (var item in relevantItems)
                {
                    leadTimes.Remove(item);
                }
            }
            //insert
            context.Kpis.AddRange(leadTimesAverage);
            context.SaveChanges();
        }

        public static void CalculateMachineUtilization(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber)
        {
            //get machines
            var machines = context.Machines.Select(a => a.Name).ToList();
            //get SimulationTime
            var simulationTime = context.SimulationWorkschedules.Max(a => a.End);
            //get working time

            var content = context.SimulationWorkschedules.Select(x => new { x.Start, x.End, x.Machine });
            var kpis = content.GroupBy(x => x.Machine).Select(g => new Kpi()
                                                                        {
                                                                            Value = (double)(g.Sum(x => x.End) - g.Sum(x => x.Start)) / simulationTime,
                                                                            Name = g.Key,
                                                                            IsKpi = true,
                                                                            KpiType = KpiType.MachineUtilization,
                                                                            SimulationConfigurationId = simulationId,
                                                                            SimulationType = simulationType,
                                                                            SimulationNumber = simulationNumber
                                                                        }).ToList();
            context.Kpis.AddRange(kpis);
            context.SaveChanges();
        }

        public static void CalculateTimeliness(ProductionDomainContext context, int simulationId, SimulationType simulationType, int simulationNumber)
        {
            var orderTimeliness = context.Orders.Where(a => a.State == State.Finished)
                                                .ToList()
                                                .Select(order => new Kpi()
            {
                Name = order.Name,
                Value = order.FinishingTime - order.DueTime
            }).ToList();
            if (!orderTimeliness.Any()) return;
            var kpis = new Kpi()
            {
                Name = "Timeliness",
                Value = 1-((double)orderTimeliness.Count(a => a.Value >= 0) / orderTimeliness.Count),
                IsKpi = true,
                KpiType = KpiType.Timeliness,
                SimulationConfigurationId = simulationId,
                SimulationType = simulationType,
                SimulationNumber = simulationNumber

            };

            context.Add(kpis);
            context.SaveChanges();
        }

    }
}
