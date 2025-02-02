using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using PortfolioTracker.Data;
using Microsoft.EntityFrameworkCore;




namespace PortfolioTracker.Controllers
{
    public class PortfolioController : Controller
    {
        // רשימת הפוזיציות הפעילות
        private static List<PortfolioItem> portfolio = new List<PortfolioItem>();
        private static List<MarketReturn> marketReturns = new List<MarketReturn>();

        // רשימת היסטוריה
        private static List<PortfolioItem> historyPortfolio = new List<PortfolioItem>();
        private static List<PortfolioItem> portfolioHistory = new List<PortfolioItem>(); // רשימת היסטוריה
        private readonly ApplicationDbContext _dbContext;
        public PortfolioController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        // GET: Portfolio
        public ActionResult Index(int? selectedYear)
        {
            // Step 1: Filter active portfolio items
            var activePortfolio = portfolio.Where(p => p.IsActive).ToList();

            // Step 2: Calculate total investment and portfolio returns
            decimal totalInvestment = activePortfolio.Sum(p => p.Investment);
            decimal portfolioReturns = activePortfolio.Sum(p => p.ProfitLoss);



            // Top 10 Earning Stocks (sorted correctly)
            var topEarningStocks = activePortfolio
                .Where(p => p.ProfitLoss > 0) // Only profitable stocks
                .OrderByDescending(p => p.ProfitLoss)
                .Take(10)
                .ToList();

            // Bottom 10 Losing Stocks (excluding profitable stocks)
            var bottomLosingStocks = activePortfolio
                .Where(p => p.ProfitLoss < 0) // Only losing stocks
                .OrderBy(p => p.ProfitLoss)
                .Take(10)
                .ToList();
            ViewBag.TopEarningStockNames = topEarningStocks.Select(s => s.Stock).ToList();
            ViewBag.TopEarningStockProfits = topEarningStocks.Select(s => s.ProfitLoss).ToList();

            ViewBag.BottomLosingStockNames = bottomLosingStocks.Select(s => s.Stock).ToList();
            ViewBag.BottomLosingStockLosses = bottomLosingStocks.Select(s => Math.Abs(s.ProfitLoss)).ToList();

            

            // Other existing logic remains unchanged
            ViewBag.Years = portfolioHistory.Select(p => p.DateAdded.Year).Distinct().OrderByDescending(y => y).ToList();
            ViewBag.SelectedYear = selectedYear;

            // Step 5: Group data by months and years (for line chart)
            var groupedData = portfolioHistory
                .Where(p => !selectedYear.HasValue || p.DateAdded.Year == selectedYear.Value) // Filter by year if selected
                .OrderBy(p => p.DateAdded) // Sort by date
                .GroupBy(p => new { p.DateAdded.Year, p.DateAdded.Month }) // Group by year and month
                .Select(g => new
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1), // Create a date object
                    MonthlyReturn = g.Sum(p => p.ProfitLoss) / (g.Sum(p => p.Investment) == 0 ? 1 : g.Sum(p => p.Investment)) * 100
                })
                .OrderBy(g => g.Date) // Sort by date again
                .ToList();



            var years = portfolioHistory
                .Select(p => p.DateAdded.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // יצירת SelectList עבור השנים
            ViewBag.YearsList = new SelectList(
                years.Select(y => new { YearValue = y, YearText = y.ToString() }),
                "YearValue", // שם המפתח עבור הערך (value)
                "YearText", // שם המפתח עבור הטקסט שמוצג (text)
                selectedYear);

            // Step 6: Prepare data for dropdown and charts
            ViewBag.Years = portfolioHistory.Select(p => p.DateAdded.Year).Distinct().OrderByDescending(y => y).ToList();
            ViewBag.SelectedYear = selectedYear;

            ViewBag.Months = groupedData.Select(g => g.Date.ToString("MM/yyyy")).ToList(); // Format: MM/yyyy
            ViewBag.MonthlyReturns = groupedData.Select(g => g.MonthlyReturn).ToList();

            // Step 7: Calculate absolute success rate
            int totalTrades = portfolioHistory.Count; // Total trades
            int successfulTrades = portfolioHistory.Count(p => p.ProfitLoss > 0); // Profitable trades
            decimal absoluteSuccessRate = totalTrades > 0 ? (decimal)successfulTrades / totalTrades * 100 : 0;

            ViewBag.AbsoluteSuccessRate = absoluteSuccessRate;

            // Step 8: Pass market returns data
            ViewBag.MarketReturns = marketReturns
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .Select(r => r.ReturnPercentage)
                .ToList();

            // Step 9: Pass Top 10 Earning Stocks and Bottom 10 Losing Stocks to the view
            ViewBag.TopEarningStocks = topEarningStocks;
            ViewBag.BottomLosingStocks = bottomLosingStocks;



            // Return the active portfolio to the view
            return View(activePortfolio);
        }

        public decimal CalculateTotalMarketPerformance(List<decimal> marketReturns)
        {
            if (marketReturns == null || !marketReturns.Any())
            {
                return 0; // אם אין נתונים, מחזירים 0
            }

            decimal totalPerformance = 1; // מתחילים מ-1 כדי לייצג את הערך ההתחלתי (100%)

            foreach (var monthlyReturn in marketReturns)
            {
                totalPerformance *= (1 + (monthlyReturn / 100)); // מוסיפים את התשואה של כל חודש ומכפילים
            }

            totalPerformance = (totalPerformance - 1) * 100; // מורידים 1 ומחזירים אחוזים
            return totalPerformance;
        }


        [HttpPost]
        public IActionResult AddMarketReturn(int month, int year, decimal returnPercentage, [FromServices] ApplicationDbContext dbContext)
        {

            // חיפוש ערך קיים לפי חודש ושנה
            var existingReturn = dbContext.MarketReturn
                .FirstOrDefault(r => r.Month == month && r.Year == year);

            if (existingReturn != null)
            {
                // אם הערך קיים, עדכון התשואה
                existingReturn.ReturnPercentage = returnPercentage;
            }
            else
            {
                // אם הערך לא קיים, יצירת ערך חדש
                var newMarketReturn = new MarketReturn
                {
                    Month = month,
                    Year = year,
                    ReturnPercentage = Math.Round(returnPercentage, 2)
                };

                dbContext.MarketReturn.Add(newMarketReturn);
            }

            // שמירת השינויים בבסיס הנתונים
            dbContext.SaveChanges();

            // חזרה לדף הראשי
            return RedirectToAction("Index");
        }



        public ActionResult Graphs()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetYearlyData(int year)
        {
            var dataForYear = portfolioHistory
                .Where(p => p.DateAdded.Year == year)
                .GroupBy(p => p.DateAdded.ToString("MMMM"))
                .Select(g => new {
                    Month = g.Key,
                    Return = g.Sum(p => p.ProfitLoss) / (g.Sum(p => p.Investment) == 0 ? 1 : g.Sum(p => p.Investment)) * 100
                })
                .OrderBy(g => DateTime.ParseExact(g.Month, "MMMM", System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            var months = dataForYear.Select(d => d.Month).ToList();
            var returns = dataForYear.Select(d => d.Return).ToList();

            return Json(new { months, returns });

        }






        public ActionResult Alerts()
        {
            return View();
        }


        [HttpGet]
        public JsonResult Sp500Returns(string months)
        {
            var monthList = months.Split(',');
            var returns = monthList.Select(month => new
            {
                Month = month,
                Return = new Random().Next(-10, 10) // נתונים מדומים בין -10% ל-10%
            });

            return Json(new { months, returns });

        }



        // פעולה להצגת ההיסטוריה
        [HttpGet]
        public async Task<ActionResult> Full()
        {
            try
            {
                // שליפת כל הנתונים מהטבלה PortfolioItems
                var portfolioItems = await _dbContext.PortfolioItem.ToListAsync();

                // העברת הנתונים ל-View
                return View(portfolioItems);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred: " + ex.Message;
                return View(new List<PortfolioItem>());
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPortfolioData()
        {
            try
            {
                var portfolioItems = await _dbContext.PortfolioItem.ToListAsync();
                return Json(portfolioItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching portfolio data", error = ex.Message });
            }
        }



        [HttpPost]
        public async Task<ActionResult> AddToFull(string stock, decimal purchasePrice, decimal investment, DateTime dateAdded)
        {
            try
            {
                // קריאה ל-API לקבלת מידע עדכני על המניה
                var stockData = await GetStockInfo(stock);
                if (stockData == null)
                {
                    ViewBag.Error = "Failed to fetch stock data. Please check the stock symbol.";
                    return RedirectToAction("Full");
                }

                // חישוב כמות המניות ורווח/הפסד
                double quantity = (double)(investment / purchasePrice);
                decimal profitLoss = ((decimal)quantity * stockData.Price) - investment;
                decimal profitLossPercentage = investment == 0 ? 0 : (profitLoss / investment) * 100;

                // שמירת המידע בבסיס הנתונים
                var portfolioItem = new PortfolioItem
                {
                    Stock = stockData.Stock,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price, // מחיר המניה הנוכחי
                    ChangePercentage = stockData.ChangePercentage, // שינוי יומי באחוזים
                    ProfitLoss = profitLoss, // רווח/הפסד
                    ProfitLossPercentage = profitLossPercentage, // רווח/הפסד באחוזים
                    DateAdded = dateAdded, // התאריך שסופק
                    IsActive = false, // הופך ללא פעיל מיד
                    IsDeleted = true // הגדרת השדה כלא פעיל
                };

                // הוספת הנתון למסד הנתונים
                _dbContext.PortfolioItem.Add(portfolioItem);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Full");
            }
            catch (Exception ex)
            {
                // טיפול בחריגות
                ViewBag.Error = "An error occurred: " + ex.Message;
                return RedirectToAction("Full");
            }
        }




        // פעולה להוספת מניה עם תאריך
        [HttpPost]
        public async Task<ActionResult> AddStock(string stock, decimal purchasePrice, decimal investment, DateTime dateAdded)
        {
            try
            {
                // קריאה ל-API לקבלת מידע עדכני על המניה
                var stockData = await GetStockInfo(stock);
                if (stockData == null)
                {
                    ViewBag.Error = "Failed to fetch stock data. Please check the stock symbol.";
                    return View("Index", portfolio);
                }

                // חישוב כמות המניות ורווח/הפסד
                double quantity = (double)(investment / purchasePrice);
                decimal profitLoss = ((decimal)quantity * stockData.Price) - investment;

                // יצירת פריט מניה חדש
                var newStock = new PortfolioItem
                {
                    ID = portfolioHistory.Any() ? portfolioHistory.Max(p => p.ID) + 1 : 1, // ID ייחודי

                    Stock = stockData.Stock,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    Investment = investment,
                    Price = stockData.Price,
                    ChangePercentage = stockData.ChangePercentage,
                    ProfitLoss = profitLoss,
                    DateAdded = dateAdded, // הגדרת התאריך שהמשתמש סיפק
                    IsActive = true
                };

                // הוספת המניה לרשימה הראשית
                portfolio.Add(newStock);

                // הוספת המניה גם להיסטוריה
                portfolioHistory.Add(newStock);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred: " + ex.Message;
                return View("Index", portfolio);
            }
        }
        [HttpPost]

        public ActionResult ToggleActiveStatus(int id)
        {
            var stock = portfolioHistory.FirstOrDefault(p => p.ID == id);
            if (stock == null)
            {
                TempData["Error"] = "Stock not found.";
                return RedirectToAction("Full");
            }

            // בדיקה אם המנייה נמחקה לצמיתות
            if (stock.IsDeleted)
            {
                TempData["Error"] = "This stock has been permanently disabled and cannot be re-enabled.";
                return RedirectToAction("Full");
            }

            // שינוי הסטטוס בין Active ל-Inactive
            stock.IsActive = !stock.IsActive;

            // אם הסטטוס הפך ל-Inactive, סמן את המנייה כ-Deleted
            if (!stock.IsActive)
            {
                stock.IsDeleted = true;
            }

            return RedirectToAction("Full");
        }


        [HttpPost]
        public IActionResult SellStock([FromBody] SellStockRequest request)
        {
            try
            {
                var stock = _dbContext.PortfolioItem.FirstOrDefault(s => s.ID == request.StockId);

                if (stock == null)
                {
                    return NotFound(new { message = "Stock not found." });
                }

                if ((double)request.SellQuantity > stock.Quantity)
                {
                    return BadRequest(new { message = "Sell quantity exceeds available quantity." });
                }

                // חישוב כמות שנותרה ועדכון המניה
                stock.Quantity -= (double)request.SellQuantity;
                if (stock.Quantity == 0)
                {
                    stock.IsActive = false; // סימון המניה כלא פעילה אם הכמות היא 0
                }

                // יצירת רשומת מכירה
                var sale = new Sale
                {
                    StockId = stock.ID,
                    QuantitySold = (double)request.SellQuantity,
                    SellPrice = request.SellPrice,
                    ProfitLoss = (request.SellPrice * request.SellQuantity) - (stock.PurchasePrice * request.SellQuantity),
                    SaleDate = DateTime.Now
                };

                _dbContext.Sales.Add(sale);
                _dbContext.SaveChanges();

                return Ok(new { message = "Stock sold successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateRiskManagement([FromBody] RiskManagementRequest request)
        {
            var stock = _dbContext.PortfolioItem.FirstOrDefault(s => s.ID == request.StockId);

            if (stock == null)
            {
                return NotFound(new { message = "Stock not found." });
            }

            stock.StopLoss = request.StopLoss;
            stock.RiskValue = request.RiskValue;
            stock.RiskPercentage = request.RiskPercentage;

            _dbContext.SaveChanges();

            return Ok(new { message = "Risk management updated successfully." });
        }


        [HttpDelete]

        public ActionResult DeleteStockFromHistory(int id)
        {
            try
            {
                var stockToDelete = _dbContext.PortfolioItem.FirstOrDefault(p => p.ID == id);
                if (stockToDelete != null)
                {
                    _dbContext.PortfolioItem.Remove(stockToDelete); // מחיקת השורה מה-DbSet
                    _dbContext.SaveChanges(); // שמירת השינויים ב-SQL
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]


        public async Task<JsonResult> GetSP500MonthlyReturns()
        {
            try
            {
                string apiKey = "cuav1s1r01qof06jg10gcuav1s1r01qof06jg110"; // שים כאן את ה-API KEY שלך
                string symbol = "^GSPC"; // סמל המדד S&P 500
                long fromDate = GetUnixTime(DateTime.Now.AddYears(-1)); // 12 חודשים אחורה
                long toDate = GetUnixTime(DateTime.Now); // עד היום
                string apiUrl = $"https://finnhub.io/api/v1/stock/candle?symbol={symbol}&resolution=M&from={fromDate}&to={toDate}&token={apiKey}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var data = JsonConvert.DeserializeObject<dynamic>(response);

                    if (data == null || data["c"] == null)
                    {
                        return Json(new { error = "No data found for S&P 500" });
                    }


                    var closingPrices = ((IEnumerable<dynamic>)data["c"]).Select(price => (decimal)price).ToList();

                    // חישוב תשואות חודשיות
                    var returns = new List<decimal>();
                    for (int i = 1; i < closingPrices.Count; i++)
                    {
                        decimal monthlyReturn = ((closingPrices[i] - closingPrices[i - 1]) / closingPrices[i - 1]) * 100;
                        returns.Add(monthlyReturn);
                    }

                    // יצירת רשימת חודשים
                    var months = Enumerable.Range(0, returns.Count)
                        .Select(i => DateTime.Now.AddMonths(-1 * (returns.Count - 1 - i)).ToString("MMMM yyyy"))
                        .ToList();

                    return Json(new { months, returns });

                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });

            }
        }

        private long GetUnixTime(DateTime date)
        {
            return ((DateTimeOffset)date).ToUnixTimeSeconds();
        }
        [HttpGet]
        [Route("PortfolioController/GetMarketReturns")]
        public JsonResult GetMarketReturns()
        {
            var data = _dbContext.MarketReturn
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .Select(m => new
                {
                    m.Month,
                    m.Year,
                    m.ReturnPercentage
                })
                .ToList();

            return Json(data);
        }





        // פעולה למחיקת מניה מהטבלה הפעילה בלבד
        [HttpPost]
        public ActionResult DeleteStock(string stock)
        {
            // מציאת המניה ברשימה הפעילה ומחיקתה
            var stockItem = portfolio.FirstOrDefault(s => s.Stock == stock);
            if (stockItem != null)
            {
                portfolio.Remove(stockItem);
            }

            return RedirectToAction("Index");
        }

        // פעולה לשליפת מידע על מניה מ-API
        private async Task<StockData> GetStockInfo(string symbol)
        {
            try
            {
                string apiKey = "cuav1s1r01qof06jg10gcuav1s1r01qof06jg110"; // שים כאן את ה-API KEY שלך
                string apiUrl = $"https://finnhub.io/api/v1/quote?symbol={symbol}&token={apiKey}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(apiUrl);
                    var data = JsonConvert.DeserializeObject<dynamic>(response);

                    if (data == null || data["c"] == null)
                    {
                        return null; // אם אין נתונים על המניה
                    }

                    return new StockData
                    {
                        Stock = symbol,
                        Price = (decimal)data["c"], // מחיר המניה הנוכחי
                        ChangePercentage = (decimal)((data["d"] != null && data["pc"] != null)
                            ? ((decimal)data["d"] / (decimal)data["pc"] * 100)
                            : 0) // חישוב אחוז שינוי
                    };
                }
            }
            catch
            {
                return null;
            }
        }




        // מחלקה לשליפת נתוני מניה מה-API
        public class StockData
        {
            public string Stock { get; set; }
            public decimal Price { get; set; }
            public decimal ChangePercentage { get; set; }
        }
    }
}

