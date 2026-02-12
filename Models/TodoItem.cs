namespace FinanceTracker.Models
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public decimal? Amount { get; set; }
        public string? Category { get; set; }
        public bool IsFinancial { get; set; }
        public string Priority { get; set; } = "Medium";
    }
}
