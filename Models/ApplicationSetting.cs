using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class ApplicationSetting
    {
        [Key]
        public int Id { get; set; }

        public required string Key { get; set; } // e.g., "MonthlyBudget"

        public required string Value { get; set; } // e.g., "5000"
    }
}
