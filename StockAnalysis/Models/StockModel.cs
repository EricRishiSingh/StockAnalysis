using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

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
        public List<StockPurchase> StockPurchases { get; set; }
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
        public List<SelectListItem> GetStockSymbols
        {
            get
            {
                var result = StockModels
                    .Select(i => new SelectListItem() { Text = i.StockSymbol, Value = i.StockSymbol })
                    .ToList();

                var firstItem = result.FirstOrDefault();
                if (firstItem != null)
                    firstItem.Selected = true;

                return result;
            }
        }
    }

    public enum Grade
    {
        A,
        B,
        C,
        D,
        F
    }

    public static class DropDownValues
    {
        public static List<SelectListItem> GetTimeRange
        {
            get
            {
                var now = DateTime.Now;
                return new List<SelectListItem>()
                {
                    new SelectListItem() { Text = "1 day", Value = now.AddDays(-1).ToString("MM/dd/yyyy") },
                    new SelectListItem() { Text = "5 day", Value = now.AddDays(-5).ToString("MM/dd/yyyy") },
                    new SelectListItem() { Text = "1 Month", Value = now.AddMonths(-1).ToString("MM/dd/yyyy") },
                    new SelectListItem() { Text = "3 Month", Value = now.AddMonths(-3).ToString("MM/dd/yyyy") },
                    new SelectListItem() { Text = "6 Month", Value = now.AddMonths(-6).ToString("MM/dd/yyyy") },
                    new SelectListItem() { Text = "1 Year", Value = now.AddYears(-1).ToString("MM/dd/yyyy"), Selected = true },
                    new SelectListItem() { Text = "5 Years", Value = now.AddYears(-5).ToString("MM/dd/yyyy") },
                    new SelectListItem() { Text = "10 Years", Value = now.AddYears(-10).ToString("MM/dd/yyyy") }
                };
            }
        }
    }
}