﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleNet.D3.Models
{
    public class CalculatedStats
    {
        public ObservableCollection<CalculatedStat> BaseStats { get; set; }
        public ObservableCollection<CalculatedStat> DamageStats { get; set; }
        public ObservableCollection<CalculatedStat> DefenseStats { get; set; }
        public ObservableCollection<CalculatedStat> LifeStats { get; set; }
        public ObservableCollection<CalculatedStat> AdventureStats { get; set; }
        public double EHP { get; set; }
        public double DPS { get; set; }

        public CalculatedStats()
        {
            BaseStats = new ObservableCollection<CalculatedStat>();
            DamageStats = new ObservableCollection<CalculatedStat>();
            DefenseStats = new ObservableCollection<CalculatedStat>();
            LifeStats = new ObservableCollection<CalculatedStat>();
            AdventureStats = new ObservableCollection<CalculatedStat>();
        }
    }

    public class CalculatedStat
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string DisplayValue { get; set; }
        public double[] Values { get; set; }
        public string Format { get; set; }

        public CalculatedStat(String name, double[] values, string format)
        {
            Name = name;
            Value = values[0];
            Values = values;
            Format = format;

            object[] obj = new object[values.Length];
            for (int i = 0; i < values.Length; i++)
                obj[i] = values[i];

            DisplayValue = String.Format(format, obj);
        }
    }
}
