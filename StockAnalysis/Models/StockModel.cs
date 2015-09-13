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
        public string Name { get; set; }
        //public static StockModel Header  => new StockModel()
        //{
        //    StockSymbol = nameof(StockSymbol),
        //    StockPrice = nameof(StockPrice),
        //    PERatio = nameof(PERatio),
        //    MarketCap = nameof(MarketCap),
        //    YearLow = nameof(YearLow),
        //    YearHigh = nameof(YearHigh),
        //    Dividend = nameof(Dividend),
        //    TargetPrice = nameof(TargetPrice),
        //    Name = nameof(Name)
        //};
    }

    [Serializable]
    public class UserStockModel
    {
        public string StockSymbol { get; set; }
        public float Performance { get; set; }
        public float CostBasis { get; set; }
        public Grade StockGrade { get; set; }
        public List<StockPurchase> StockPurchases {get;set;}
    }

    public class StockPurchase
    {
        public float StockPrice { get; set; }
        public float NumberOfShares { get; set; }
    }

    public class UserView
    {
        public List<StockModel> StockModels { get; set; }
        public List<UserStockModel> UserStockModels { get; set; }
    }

    public enum Grade
    {
        A,
        B,
        C,
        D,
        F
    }

    //public class StockSearchModel
    //{
    //    [Required]
    //    [Display(Name = "Stock Symbol")]
    //    public string StockSymbol { get; set; }
    //}
}