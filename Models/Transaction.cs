using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; } // Description of spend

        [Required]
        public float Amount { get; set; } // Using float as requested

        [Required]
        public DateTime TransactionDate { get; set; } // Calendar date

        [Required]
        public required string Category { get; set; } // Food, Grocery, etc.

        [Required]
        public required string Type { get; set; } // "Gain" or "Loss"

        [Required]
        public required string Source { get; set; } // "Cash" or "Bank"
                                           // This tells C#: "If Type is Gain, IsIncome is true. Otherwise, it's false."
        public bool IsIncome => Type == "Gain";
    

        // Keep your other properties (Date, Category, etc.) below
    }
}