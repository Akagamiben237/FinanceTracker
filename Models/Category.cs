using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; } // e.g., "Food"

        [Required]
        public required string Type { get; set; } // "Gain" or "Loss"

        public string Icon { get; set; } = "📦"; // Default icon
    }
}
