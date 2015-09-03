﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
        //
        // GET: /Stocks/

        public async Task<ActionResult> Stocks()
        {
            List<StockModel> stockInfo = new List<StockModel>();
            stockInfo.Add(StockModel.Header);
            List<string> stockSymbols = new List<string>() { "^GSPC", "^INDU", "OSPTX" };

            var user = User.GetUserProfile();
            if (user != null)
            {
                if (user?.StockSymbols?.Any() ?? false)
                {
                    stockSymbols.AddRange(user.StockSymbols.XmlDeserializeFromString());
                }
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
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + string.Join("+", stockSymbols) + "&f=sarj1jkdt8n";
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
                        TargetPrice = lineInfo[7],
                        Name = lineInfo[8]
                    });
                }
            }

            return View(stockInfo);
        }

        public async Task<ActionResult> StockSearch(string stockSymbol)
        {
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + stockSymbol + "&f=s";

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
                    string retStockSymbol = lineInfo[0].Replace("\"", "").ToUpper();

                    using (var userContext = new UsersContext())
                    {
                        string userName = User.Identity.Name;
                        if (User?.Identity?.IsAuthenticated ?? true)
                        {
                            var user = userContext.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == userName.ToLower());
                            if (user != null)
                            {
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
    }

    public static class Extensions
    {
        public static UserProfile GetUserProfile(this System.Security.Principal.IPrincipal User)
        {
            UserProfile user = null;
            using (var userContext = new UsersContext())
            {
                string userName = User.Identity.Name;
                if (User?.Identity?.IsAuthenticated ?? true)
                    user = userContext.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == userName.ToLower());
            }

            return user;
        }

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
