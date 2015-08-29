using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockAnalysis.Models
{
    public class StockModel
    {
        public string StockSymbol { get; set; }
        public float StockPrice { get; set; }
        public float PERatio { get; set; }
        public float MarketCap { get; set; }
        public float YearLow { get; set; }
        public float YearHigh { get; set; }
        public float Dividend { get; set; }
        public float TargetPrice { get; set; }

    }
}