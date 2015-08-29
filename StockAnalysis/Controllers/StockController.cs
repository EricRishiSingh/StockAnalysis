using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using StockAnalysis.Models;

namespace StockAnalysis.Controllers
{
    public class StockController : Controller
    {
        //
        // GET: /Stocks/

        public ActionResult Stocks()
        {
            List<StockModel> stockInfo = new List<StockModel>();
            List<string> stockSymbols = new List<string>() { "GOOG", "ABUS" };

            // Get the stock info by using the following arguments
            // StockSymbol = s
            // StockPrice = a
            // PERatio = r
            // MarketCap = j1
            // YearLow = j
            // YearHigh = k
            // Dividend = d
            // TargetPrice = t8
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + string.Join("+", stockSymbols) + "&f=sarj1jkdt8";
            Task<WebResponse>.Run(() =>
            {
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                var webResp = (HttpWebResponse)webReq.GetResponse();
                return webResp;
            })
            .ContinueWith(task =>
                {
                    var result = task.Result;
                    using (StreamReader strm = new StreamReader(result.GetResponseStream()))
                    {
                        string line;
                        while ((line = strm.ReadLine()) != null)
                        {
                            
                        }
                    }

                });



            return View();
        }

    }
}
