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
        public ObservableCollection<CalculatedStat> OtherStats { get; set; }
        public double EHP { get; set; }
        public double DPS { get; set; }

        public CalculatedStats()
        {
            BaseStats = new ObservableCollection<CalculatedStat>();
            DamageStats = new ObservableCollection<CalculatedStat>();
            DefenseStats = new ObservableCollection<CalculatedStat>();
            OtherStats = new ObservableCollection<CalculatedStat>();
        }
    }

    public class CalculatedStat
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string DisplayValue { get; set; }

        public CalculatedStat(String name, double value, string displayValue)
        {
            Name = name;
            Value = value;
            DisplayValue = displayValue;
        }
    }
}