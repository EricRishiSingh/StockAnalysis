using StockAnalysis.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Helpers;
using System.Xml.Serialization;

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
            List<string> stockSymbols = new List<string>() { "^GSPC", "^GSPTSE" };

            using (var userContext = new UsersContext())
            {
                // Add user stock to the table
                var user = userContext.GetUser(User.Identity.Name);
                if (user != null)
                    if (user?.StockSymbols?.Any() ?? false)
                        stockSymbols.AddRange(Deserialize<List<string>>(user.StockSymbols));
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
            var result = await GetWebResponse(url);

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

            var userStockModels = new List<UserStockModel>();
            using (var userContext = new UsersContext())
            {
                if (User?.Identity?.IsAuthenticated ?? true)
                {
                    var user = userContext.GetUser(User.Identity.Name);
                    if (user != null)
                    {
                        userStockModels = string.IsNullOrEmpty(user.UserStockInformation) ? new List<UserStockModel>() : Deserialize<List<UserStockModel>>(user.UserStockInformation);

                        foreach (var stock in userStockModels)
                            await UpdateUserStocks(stock);

                        user.UserStockInformation = Serialize<List<UserStockModel>>(userStockModels);
                        userContext.Entry(user).CurrentValues.SetValues(user);
                        userContext.SaveChanges();
                    }
                }
            }

            return View(new UserView() { StockModels = stockModels, UserStockModels = userStockModels });
        }

        public ActionResult StockSearch(string stockSymbol)
        {
            if (!ValidateStockSymbol(stockSymbol))
                return RedirectToAction("Stocks");

            // Save result in database
            using (var userContext = new UsersContext())
            {
                if (User?.Identity?.IsAuthenticated ?? true)
                {
                    var user = userContext.GetUser(User.Identity.Name);
                    if (user != null)
                    {
                        string retStockSymbol = stockSymbol.ToUpperInvariant();
                        var list = new List<string>();
                        if (user.StockSymbols != null)
                            list = Deserialize<List<string>>(user.StockSymbols);

                        if (!list.Contains(retStockSymbol))
                        {
                            list.Add(retStockSymbol);
                            user.StockSymbols = Serialize<List<string>>(list);
                            userContext.Entry(user).CurrentValues.SetValues(user);
                            userContext.SaveChanges();
                        }
                    }
                }
            }

            return RedirectToAction("Stocks");
        }

        public async Task<ActionResult> AddUserStock(string stockSymbol, string numberOfShares, string stockPrice)
        {
            float numOfShares = 0;
            float price = 0;
            if (!float.TryParse(numberOfShares, out numOfShares)
                || !float.TryParse(stockPrice, out price)
                || !ValidateStockSymbol(stockSymbol.ToUpperInvariant()))
                return RedirectToAction("Stocks");

            // Save result in database
            // TODO find a better way to get the user - make it common
            using (var userContext = new UsersContext())
            {
                if (User?.Identity?.IsAuthenticated ?? true)
                {
                    var user = userContext.GetUser(User.Identity.Name);
                    if (user != null)
                    {
                        var userStocks = string.IsNullOrEmpty(user.UserStockInformation) ? new List<UserStockModel>() : Deserialize<List<UserStockModel>>(user.UserStockInformation);
                        var stock = userStocks.FirstOrDefault(i => i.StockSymbol == stockSymbol.ToUpperInvariant());
                        if (stock != null)
                        {
                            var stockPurchases = stock.StockPurchases;
                            if (stockPurchases == null)
                                stockPurchases = new List<StockPurchase>();

                            if ((stockPurchases.Sum(i => i.NumberOfShares) + numOfShares) < 0)
                                return RedirectToAction("Stocks");

                            if (price > 0)
                                stockPurchases.Add(new StockPurchase() { NumberOfShares = numOfShares, StockPrice = price });

                            await UpdateUserStocks(stock);
                        }
                        else
                        {
                            if (price > 0)
                            {
                                var newStock = new UserStockModel()
                                {
                                    StockSymbol = stockSymbol.ToUpperInvariant(),
                                    StockPurchases = new List<StockPurchase>() { new StockPurchase() { NumberOfShares = numOfShares, StockPrice = price } }
                                };


                                if (price > 0)
                                    userStocks.Add(newStock);

                                await UpdateUserStocks(newStock);
                            }
                        }

                        user.UserStockInformation = Serialize<List<UserStockModel>>(userStocks);
                        userContext.Entry(user).CurrentValues.SetValues(user);
                        userContext.SaveChanges();
                    }
                }

                return RedirectToAction("Stocks");
            }
        }

        // TODO improve performance. This method called too many times
        private async Task UpdateUserStocks(UserStockModel stock)
        {
            float stockPrice = await GetStockPrice(stock.StockSymbol);

            if (stock?.StockPurchases?.Any() == true)
            {
                var purchases = stock.StockPurchases;
                float totalPurchaseCost = purchases.Sum(i => i.NumberOfShares * i.StockPrice);
                float totalNumberOfShares = purchases.Sum(i => i.NumberOfShares);

                stock.CostBasis = totalPurchaseCost / totalNumberOfShares;
                stock.Performance = (float)Math.Round((stockPrice - stock.CostBasis) / stock.CostBasis * 100, 2);
                stock.StockGrade = CalculateGrade(stock.Performance);
            }
        }

        private Grade CalculateGrade(float performance)
        {
            Grade result = Grade.F;

            if (performance >= 25)
                result = Grade.A;
            else if (performance >= 10)
                result = Grade.B;
            else if (performance >= 0)
                result = Grade.C;
            else if (performance >= -50)
                result = Grade.D;

            return result;
        }

        private async Task<float> GetStockPrice(string stockSymbol)
        {
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + stockSymbol + "&f=a";
            var result = await GetWebResponse(url);
            float stockPrice = 0;

            using (StreamReader strm = new StreamReader(result.GetResponseStream()))
            {
                string line;
                while ((line = strm.ReadLine()) != null)
                {
                    var lineInfo = line.Split(',');
                    float.TryParse(lineInfo[0], out stockPrice);
                }
            }

            return stockPrice;
        }

        public ActionResult RemoveStock(string stockSymbol, bool stockSymbols)
        {
            // Remove stock from database
            using (var userContext = new UsersContext())
            {
                if (User?.Identity?.IsAuthenticated ?? true)
                {
                    string userName = User.Identity.Name;
                    var user = userContext.GetUser(userName);
                    if (user != null)
                    {
                        if (stockSymbols)
                        {
                            var list = Deserialize<List<string>>(user.StockSymbols);
                            list.Remove(stockSymbol);
                            user.StockSymbols = Serialize<List<string>>(list);
                        }
                        else
                        {
                            var list = Deserialize<List<UserStockModel>>(user.UserStockInformation);
                            var stockToRemove = list.FirstOrDefault(i => i.StockSymbol == stockSymbol);
                            if (stockToRemove != null)
                            {
                                list.Remove(stockToRemove);
                                user.UserStockInformation = Serialize<List<UserStockModel>>(list);
                            }
                        }

                        userContext.Entry(user).CurrentValues.SetValues(user);
                        userContext.SaveChanges();
                    }
                }
            }

            return RedirectToAction("Stocks");
        }

        public ActionResult StockChart(string stockSymbol)
        {
            if (string.IsNullOrEmpty(stockSymbol))
                return RedirectToAction("Stocks");

            var historicalPrices = GetHistoricalPrices(stockSymbol, 2014);

            var stockChart = new Chart(950, 530, theme: ChartTheme.Blue);
            stockChart.AddTitle(stockSymbol);
            stockChart.AddSeries(
                name: stockSymbol,
                chartType: "Line",
                xValue: historicalPrices.Keys,
                yValues: historicalPrices.Values)
                .Write();

            stockChart.Save("~/Content/chart" + stockSymbol);
            return base.File("~/Content/chart" + stockSymbol, "jpeg");
        }

        #region HelperMethods

        /// <summary>
        /// Deserialize the object
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="xml">The xml string</param>
        /// <returns>The object</returns>
        public static T Deserialize<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(new StringReader(xml));
        }

        /// <summary>
        /// Serialize the object
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="xml">The xml string</param>
        /// <returns>The object</returns>
        public static string Serialize<T>(object value)
        {
            var serializer = new XmlSerializer(typeof(T));
            var sb = new StringBuilder();

            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, value);
            }

            return sb.ToString();
        }

        private Task<HttpWebResponse> GetWebResponse(string url)
        {
            return Task<WebResponse>.Run(() =>
            {
                var webReq = (HttpWebRequest)WebRequest.Create(url);
                var webResp = (HttpWebResponse)webReq.GetResponse();
                return webResp;
            });
        }
        private bool ValidateStockSymbol(string stockSymbol)
        {
            string url = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + stockSymbol + "&f=sn";
            var task = GetWebResponse(url);

            // Add a stock to the table for user
            using (StreamReader strm = new StreamReader(task.Result.GetResponseStream()))
            {
                string line;
                while ((line = strm.ReadLine()) != null)
                {
                    var lineInfo = line.Split(',');

                    // Return if the stock is not valid
                    if (line.Count() <= 1 || lineInfo.Any(i => i.Contains("N/A")))
                    {
                        //ModelState.AddModelError("StockNotFound", $"The stock symbol '{stockSymbol}' is not valid");
                        return false;
                    }
                }

                return true;
            }
        }

        public Dictionary<DateTime, string> GetHistoricalPrices(string ticker, int yearToStartFrom)
        {
            Dictionary<DateTime, string> result = new Dictionary<DateTime, string>();

            using (WebClient web = new WebClient())
            {
                string data = web.DownloadString(string.Format("http://ichart.finance.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}", ticker, 1, 2, yearToStartFrom));
                data = data.Replace("r", "");
                string[] rows = data.Split('\n');

                //First row is headers so Ignore it
                for (int i = 1; i < rows.Length; i++)
                {
                    if (rows[i].Replace("n", "").Trim() == "")
                        continue;

                    string[] cols = rows[i].Split(',');
                    result[Convert.ToDateTime(cols[0])] = cols[4];
                }

                return result;
            }
        }

        #endregion
    }

    public static class Extensions
    {

    }
}
