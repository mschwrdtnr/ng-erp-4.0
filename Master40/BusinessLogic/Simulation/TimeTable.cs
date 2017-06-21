﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Master40.BusinessLogic.Simulation
{
    public class TimeTable<T>
    {
        public TimeTable()
        {
            this.Timer = 0;
            this.RecalculateTimer = 24;
            this.Initial = new List<T>();
            this.InProgress = new List<T>();
            this.Finished = new List<T>();
        }

        public List<T> Initial { get; set; }
        public List<T> InProgress { get; set; }
        public List<T> Finished { get; set; }

        public int Timer { get; set; }
        public int RecalculateTimer { get; set; }
        
    }
}
