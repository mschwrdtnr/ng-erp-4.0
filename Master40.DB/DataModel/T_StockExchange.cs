﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using Master40.DB.Enums;
using Master40.DB.Interfaces;

namespace Master40.DB.DataModel
{
    public class T_StockExchange : BaseEntity, IStockExchange, IDemand, IProvider
    {
        public int StockId { get; set; }
        public Guid TrakingGuid { get; set; }
        public int SimulationConfigurationId { get; set; }
        public SimulationType SimulationType { get; set; }
        public int SimulationNumber { get; set; }
        public M_Stock Stock { get; set; }
        public int RequiredOnTime { get; set; }
        public State State { get; set; }
        public decimal Quantity { get; set; }
        public int Time { get; set; }
        public ExchangeType ExchangeType { get; set; }
        [NotMapped]
        public Guid ProductionArticleKey { get; set; }
        public int? DemandID { get; set; }
        public T_Demand Demand { get; set; }
        public int? ProviderId { get; set; }
        public T_Provider Provider { get; set; }
    }
}