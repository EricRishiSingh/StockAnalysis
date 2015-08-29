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

        public async Task<ActionResult> Stocks()
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
            var task = Task<WebResponse>.Run(() =>
            {
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                var webResp = (HttpWebResponse)webReq.GetResponse();
                return webResp;
            });

            var result = await task;

            using (StreamReader strm = new StreamReader(result.GetResponseStream()))
            {
                string line;
                while ((line = strm.ReadLine()) != null)
                {
                    var lineInfo = line.Split(',');
                    stockInfo.Add(new StockModel()
                       {
                           StockSymbol = lineInfo[0].Replace("\"", ""),
                           StockPrice = lineInfo[1],
                           PERatio = lineInfo[2],
                           MarketCap = lineInfo[3],
                           YearLow = lineInfo[4],
                           YearHigh = lineInfo[5],
                           Dividend = lineInfo[6],
                           TargetPrice = lineInfo[7]
                       });
                }
            }
            
            return View(stockInfo);
        }

    }
}
