namespace PortfolioTracker.Models
{
   public class PortfolioItem
    {
        public int ID { get; set; } // מזהה ייחודי
        public string Stock { get; set; }
        public double Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ChangePercentage { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal Investment { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; } // אחוזי רווח/הפסד
        public DateTime DateAdded { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public decimal? StopLoss { get; set; } // ערך הסטופ לוס
        public decimal? RiskValue { get; set; } // הערך בסיכון
        public decimal? RiskPercentage { get; set; } // אחוז הסיכון
    }
    public class MonthlyTransactionsViewModel
    {
        public string Month { get; set; } // שם החודש (למשל ינואר)
        public List<PortfolioItem> Items { get; set; } // רשימת העסקאות שבוצעו באותו חודש
    }
    public class YearlyProfitViewModel
    {
        public int Year { get; set; } // השנה (למשל 2024)
        public decimal TotalProfit { get; set; } // סך הרווחים לשנה
        public List<MonthlyTransactionsViewModel> Transactions { get; set; } // רשימת עסקאות לפי חודשים
    }

    public class MarketReturn
    {
        public int Id { get; set; } // Primary Key
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal ReturnPercentage { get; set; }
    }

    public class Sale
    {
        public int ID { get; set; }
        public int StockId { get; set; }
        public double QuantitySold { get; set; }
        public decimal SellPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public DateTime SaleDate { get; set; }
    }

    public class SellStockRequest
    {
        public int StockId { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal SellPrice { get; set; }
    }


    public class RiskManagementRequest
    {
        public int StockId { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? RiskValue { get; set; }
        public decimal? RiskPercentage { get; set; }
    }



}
