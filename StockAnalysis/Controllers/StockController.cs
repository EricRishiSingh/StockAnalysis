using StockAnalysis.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace StockAnalysis.Controllers
{
    public class StockController : Controller
    {
        #region Fields/Properties

        public const string YahooURL = "http://download.finance.yahoo.com/d/quotes.csv?s=";

        #endregion

        #region Methods

        #region ControllerActions

        public ActionResult Stocks()
        {
            // Initialize stocks and stock info
            List<StockModel> stockModels = new List<StockModel>();
            List<string> stockSymbols = new List<string>() { "^GSPC", "^GSPTSE" };

            PerformDatabaseAction((userContext, user) =>
            {
                // Add user stock to the table
                if (user?.StockSymbols?.Any() ?? false)
                    stockSymbols.AddRange(Deserialize<List<string>>(user.StockSymbols));
            });

            #region Symbols

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

            #endregion

            // Retrieve stock information and add to list for view to use
            foreach (var line in ParseWebResponse(YahooURL + string.Join("+", stockSymbols) + "&f=sarj1jkdt8n"))
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

            var userStockModels = new List<UserStockModel>();
            PerformDatabaseAction((userContext, user) =>
            {
                userStockModels = string.IsNullOrEmpty(user.UserStockInformation) ? new List<UserStockModel>() : Deserialize<List<UserStockModel>>(user.UserStockInformation);
                foreach (var stock in userStockModels)
                    UpdateUserStocks(stock);

                user.UserStockInformation = Serialize<List<UserStockModel>>(userStockModels);
            });

            return View(new UserView() { StockModels = stockModels, UserStockModels = userStockModels });
        }

        public ActionResult StockSearch(string stockSymbol)
        {
            if (!ValidateStockSymbol(stockSymbol))
                return RedirectToAction("Stocks");

            PerformDatabaseAction((userContext, user) =>
            {
                string retStockSymbol = stockSymbol.ToUpperInvariant();
                var list = new List<string>();
                if (user.StockSymbols != null)
                    list = Deserialize<List<string>>(user.StockSymbols);

                if (!list.Contains(retStockSymbol))
                {
                    list.Add(retStockSymbol);
                    user.StockSymbols = Serialize<List<string>>(list);
                }
            });

            return RedirectToAction("Stocks");
        }

        public ActionResult AddUserStock(string stockSymbol, string numberOfShares, string stockPrice)
        {
            float numOfShares = 0;
            float price = 0;
            string formattedStockSymbol = stockSymbol.ToUpperInvariant();

            if (price < 0 || !float.TryParse(numberOfShares, out numOfShares)
                || !float.TryParse(stockPrice, out price) || !ValidateStockSymbol(formattedStockSymbol))
                return RedirectToAction("Stocks");

            // Save result in database
            PerformDatabaseAction((userContext, user) =>
            {
                var userStocks = string.IsNullOrEmpty(user.UserStockInformation) ? new List<UserStockModel>() : Deserialize<List<UserStockModel>>(user.UserStockInformation);
                var stock = userStocks.FirstOrDefault(i => i.StockSymbol == formattedStockSymbol);
                if (stock != null)
                {
                    var stockPurchases = stock.StockPurchases;
                    if (stockPurchases == null)
                        stockPurchases = new List<StockPurchase>();

                    if ((stockPurchases.Sum(i => i.NumberOfShares) + numOfShares) > 0)
                    {
                        stockPurchases.Add(new StockPurchase() { NumberOfShares = numOfShares, StockPrice = price });
                        UpdateUserStocks(stock);
                    }
                }
                else
                {
                    var newStock = new UserStockModel()
                    {
                        StockSymbol = formattedStockSymbol,
                        StockPurchases = new List<StockPurchase>()
                        {
                            new StockPurchase() { NumberOfShares = numOfShares, StockPrice = price }
                        }
                    };

                    userStocks.Add(newStock);
                    UpdateUserStocks(newStock);
                }

                user.UserStockInformation = Serialize<List<UserStockModel>>(userStocks);
            });

            return RedirectToAction("Stocks");
        }

        public ActionResult RemoveStockSymbol(string stockSymbol)
        {
            // Remove stock from database
            PerformDatabaseAction((userContext, user) =>
            {
                if (user.StockSymbols != null)
                {
                    var list = Deserialize<List<string>>(user.StockSymbols);
                    list.Remove(stockSymbol);
                    user.StockSymbols = Serialize<List<string>>(list);
                }
            });

            return RedirectToAction("Stocks");
        }

        public ActionResult RemoveStockInformation(string stockSymbol)
        {
            // Remove stock from database
            PerformDatabaseAction((userContext, user) =>
            {
                if (user.UserStockInformation != null)
                {
                    var list = Deserialize<List<UserStockModel>>(user.UserStockInformation);
                    var stockToRemove = list.FirstOrDefault(i => i.StockSymbol == stockSymbol);
                    if (stockToRemove != null)
                    {
                        list.Remove(stockToRemove);
                        user.UserStockInformation = Serialize<List<UserStockModel>>(list);
                    }
                }
            });

            return RedirectToAction("Stocks");
        }

        public ActionResult StockChart(string stockSymbol, string date)
        {
            if (string.IsNullOrEmpty(stockSymbol) || !ValidateStockSymbol(stockSymbol))
                return RedirectToAction("Stocks");

            var historicalPrices = GetHistoricalPrices(stockSymbol, date);

            var stockChart = new Chart(950, 530, theme: ChartTheme.Blue);
            stockChart.AddTitle(stockSymbol);
            stockChart.AddSeries(
                name: stockSymbol,
                chartType: "Line",
                xValue: historicalPrices.Keys,
                yValues: historicalPrices.Values)
                .Write();

            stockChart.Save("~/Content/chart");
            return base.File("~/Content/chart", "jpeg");
        }

        #endregion

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

        private HttpWebResponse GetWebResponse(string url)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            var webResp = (HttpWebResponse)webReq.GetResponse();
            return webResp;
        }

        private IEnumerable<string> ParseWebResponse(string url)
        {
            var result = GetWebResponse(url);
            using (StreamReader strm = new StreamReader(result.GetResponseStream()))
            {
                string line;
                while ((line = strm.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        private bool ValidateStockSymbol(string stockSymbol)
        {
            foreach (var line in ParseWebResponse(YahooURL + stockSymbol + "&f=sn"))
            {
                // Return if the stock is not valid
                if (line.Count() <= 1 || line.Split(',').Any(i => i.Contains("N/A")))
                    return false;
            }

            return true;
        }

        public Dictionary<DateTime, string> GetHistoricalPrices(string ticker, string date)
        {
            Dictionary<DateTime, string> result = new Dictionary<DateTime, string>();

            using (WebClient web = new WebClient())
            {
                var timeRange = DateTime.ParseExact(date, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                string data = web.DownloadString(string.Format("http://ichart.finance.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}", ticker, timeRange.Month - 1, timeRange.Day, timeRange.Year));
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

        private void PerformDatabaseAction(Action<UsersContext, UserProfile> action)
        {
            // Save result in database
            using (var userContext = new UsersContext())
            {
                if (User?.Identity?.IsAuthenticated ?? true)
                {
                    var user = userContext.GetUser(User.Identity.Name);
                    if (user != null)
                    {
                        action(userContext, user);
                        userContext.Entry(user).CurrentValues.SetValues(user);
                        userContext.SaveChanges();
                    }
                }
            }
        }

        private Grade CalculateGrade(float performance)
        {
            var values = Enum.GetValues(typeof(Grade)).Cast<Grade>().OrderByDescending(i => i);
            foreach (var value in values)
            {
                if (performance >= (int)value)
                    return (Grade)value;
            }

            return Grade.F;
        }

        private float GetStockPrice(string stockSymbol)
        {
            float stockPrice = 0;
            foreach (var line in ParseWebResponse(YahooURL + stockSymbol + "&f=a"))
            {
                var lineInfo = line.Split(',');
                float.TryParse(lineInfo[0], out stockPrice);
            }

            return stockPrice;
        }

        // TODO improve performance. This method called too many times
        private void UpdateUserStocks(UserStockModel stock)
        {
            float stockPrice = GetStockPrice(stock.StockSymbol);

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

        #endregion

        #endregion
    }

    public static class Extensions
    {

    }
}
