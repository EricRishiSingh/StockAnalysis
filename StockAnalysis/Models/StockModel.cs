using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public static StockModel Header  => new StockModel()
        {
            StockSymbol = nameof(StockSymbol),
            StockPrice = nameof(StockPrice),
            PERatio = nameof(PERatio),
            MarketCap = nameof(MarketCap),
            YearLow = nameof(YearLow),
            YearHigh = nameof(YearHigh),
            Dividend = nameof(Dividend),
            TargetPrice = nameof(TargetPrice)
        };
    }

    public class StockSearchModel
    {
        [Required]
        [Display(Name = "Stock Symbol")]
        public string StockSymbol { get; set; }
    }
}