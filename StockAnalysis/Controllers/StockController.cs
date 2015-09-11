using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using StockAnalysis.Models;

namespace StockAnalysis.Controllers
{
    public class StockController : Controller
    {
        #region Fields/Properties


        #endregion

        //
        // GET: /Stocks/

        public async Task<ActionResult> Stocks()
        {
            // Initialize stocks and stock info
            List<StockModel> stockModels = new List<StockModel>();
            stockModels.Add(StockModel.Header);
            List<string> stockSymbols = new List<string>() { "^GSPC", "^GSPTSE" };
            
            using (var userContext = new UsersContext())
            {
                // Add user stock to the table
                var user = userContext.GetUser(User.Identity.Name);
                if (user != null)
                    if (user?.StockSymbols?.Any() ?? false)
                        stockSymbols.AddRange(user.StockSymbols.XmlDeserializeFromString());
            }

            // Get the stock info by using the following arguments
            // StockSymbol = s
            // StockPrice = a
            // PERatio = r
            // MarketCap = j1
            // YearLow = j
            // YearHigh = k
            // Dividend = d
            // TargetPrice = t8
            // Name = n
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + string.Join("+", stockSymbols) + "&f=sarj1jkdt8n";
            var task = Task<WebResponse>.Run(() =>
            {
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                var webResp = (HttpWebResponse)webReq.GetResponse();
                return webResp;
            });

            var result = await task;

            // Retrieve stock information and add to list for view to use
            using (StreamReader strm = new StreamReader(result.GetResponseStream()))
            {
                string line;
                while ((line = strm.ReadLine()) != null)
                {
                    var lineInfo = line.Split(',');
                    stockModels.Add(new StockModel()
                    {
                        StockSymbol = lineInfo[0].Replace("\"", ""),
                        StockPrice = lineInfo[1],
                        PERatio = lineInfo[2],
                        MarketCap = lineInfo[3],
                        YearLow = lineInfo[4],
                        YearHigh = lineInfo[5],
                        Dividend = lineInfo[6],
                        TargetPrice = lineInfo[7],
                        Name = lineInfo[8].Replace("\"", "")
                    });
                }
            }
            
            return View(stockModels);
        }

        public async Task<ActionResult> StockSearch(StockModel stockModel, string stockSymbol)
        {
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + stockSymbol + "&f=sn";

            var task = Task<WebResponse>.Run(() =>
            {
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                var webResp = (HttpWebResponse)webReq.GetResponse();
                return webResp;
            });

            var result = await task;

            // Add a stock to the table for user
            using (StreamReader strm = new StreamReader(result.GetResponseStream()))
            {
                string line;
                while ((line = strm.ReadLine()) != null)
                {
                    var lineInfo = line.Split(',');

                    // Return if the stock is not valid
                    if (line.Count() <= 1 || lineInfo.Any(i => i.Contains("N/A")))
                    {
                        //ModelState.AddModelError("StockNotFound", $"The stock symbol '{stockSymbol}' is not valid");
                        return RedirectToAction("Stocks");
                    }

                    // Save result in database
                    using (var userContext = new UsersContext())
                    {
                        if (User?.Identity?.IsAuthenticated ?? true)
                        {
                            var user = userContext.GetUser(User.Identity.Name);
                            if (user != null)
                            {
                                string retStockSymbol = lineInfo[0].Replace("\"", "").ToUpper();
                                var list = user.StockSymbols.XmlDeserializeFromString();
                                list.Add(retStockSymbol);
                                user.StockSymbols = list.XmlSerializeToString();
                                userContext.Entry(user).CurrentValues.SetValues(user);
                                userContext.SaveChanges();
                            }
                        }
                    }
                }
            }

            return RedirectToAction("Stocks");
        }

        public ActionResult RemoveStock(string stockSymbol)
        {
            // Remove stock from database
            using (var userContext = new UsersContext())
            {
                string userName = User.Identity.Name;
                if (User?.Identity?.IsAuthenticated ?? true)
                {
                    var user = userContext.GetUser(userName);
                    if (user != null)
                    {
                        var list = user.StockSymbols.XmlDeserializeFromString();
                        list.Remove(stockSymbol);
                        user.StockSymbols = list.XmlSerializeToString();
                        userContext.Entry(user).CurrentValues.SetValues(user);
                        userContext.SaveChanges();
                    }
                }
            }

            return RedirectToAction("Stocks");
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// List<string> --> String
        /// </summary>
        /// <param name="objectInstance">The List(string)</param>
        /// <returns>string</returns>
        public static string XmlSerializeToString(this object objectInstance)
        {
            try
            { 
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();

            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }
            catch(Exception ex)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// String --> List<string>
        /// </summary>
        /// <param name="objectData">The string</param>
        /// <returns>List(string)</returns>
        public static List<string> XmlDeserializeFromString(this string objectData)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<string>));
                List<string>result;

                using (TextReader reader = new StringReader(objectData))
                {
                    result = (List<string>)serializer.Deserialize(reader);
                }

                return result;
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
