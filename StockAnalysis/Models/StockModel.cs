using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StockAnalysis.Models
{
    public class StockModel
    {
        public string StockSymbol { get; set; }
        public string StockPrice { get; set; }
        public string PERatio { get; set; }
        public string MarketCap { get; set; }
        public string YearLow { get; set; }
        public string YearHigh { get; set; }
        public string Dividend { get; set; }
        public string TargetPrice { get; set; }

    }
}