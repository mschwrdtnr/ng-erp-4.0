﻿using System.Data.Entity;
using System.Linq;
using Master40.DB;
using Master40.DB.Data.Context;
using Master40.SimulationCore.Environment;
using Master40.SimulationCore.Environment.Options;
using Master40.SimulationCore.Helper.DistributionProvider;
using Xunit;

namespace Master40.XUnitTest.Online.Agents.Types
{
    public class OrderCounter
    {
        SimulationCore.Agents.SupervisorAgent.Types.OrderCounter _orderCounter = new SimulationCore.Agents.SupervisorAgent.Types.OrderCounter(maxQuantity: 2);
        
        [Fact]
        public void AddOrder()
        {
            bool addNewOrderSuccessful = _orderCounter.TryAddOne();
            Assert.True(condition: addNewOrderSuccessful);
        }


        [Fact]
        public void AddOrderOverMax()
        {
            //add 2
            _orderCounter.TryAddOne();
            bool addNewOrderSuccessful = _orderCounter.TryAddOne();
            Assert.True(condition: addNewOrderSuccessful);
            //add more than 2
            bool addNewOrderOverMaxExeeded = _orderCounter.TryAddOne();
            Assert.False(condition: addNewOrderOverMaxExeeded);
        }
    }
}
