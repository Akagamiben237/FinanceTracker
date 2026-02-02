namespace FinanceTracker.Models
{
    public class DashboardViewModel
    {
        // Summary Cards (Real numbers)
        public decimal TotalBalance { get; set; }
        public decimal CashBalance { get; set; }
        public decimal BankBalance { get; set; }

        // Percentage changes (Keep these for your UI design)
        public string TotalChange { get; set; } = "0.0%";
        public string CashChange { get; set; } = "0.0%";
        public string BankChange { get; set; } = "0.0%";

        
        // The list of transactions for the table
      
        public Dictionary<string, decimal> SpendingData { get; set; } = [];
        public Dictionary<string, decimal> GainsData { get; set; } = [];
        public List<Transaction> RecentTransactions { get; set; } = [];

        // Data for the Donut Chart
        public SpendingSummary Spending { get; set; } = new SpendingSummary();
    }



    public class SpendingSummary
    {
        public int Id { get; set; } // Adding this fixes the error!
        public int BudgetUsedPercentage { get; set; }
        public decimal Housing { get; set; }
        public decimal Food { get; set; }
        public decimal Utilities { get; set; }
    }
}