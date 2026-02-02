using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Models;
using System.Linq;

namespace FinanceTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly FinanceDbContext _context;

        public HomeController(FinanceDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string timeframe = "monthly")
        {
            // 1. Get all data from the database
            var allData = _context.Transactions?.ToList() ?? [];

            // 2. Define balance variables with explicit (decimal) casts
            decimal totalGain = (decimal)allData.Where(t => t.Type == "Gain").Sum(t => t.Amount);
            decimal totalLoss = (decimal)allData.Where(t => t.Type == "Loss").Sum(t => t.Amount);

            decimal cashGain = (decimal)allData.Where(t => t.Source == "Cash" && t.Type == "Gain").Sum(t => t.Amount);
            decimal cashLoss = (decimal)allData.Where(t => t.Source == "Cash" && t.Type == "Loss").Sum(t => t.Amount);

            decimal bankGain = (decimal)allData.Where(t => t.Source == "Bank" && t.Type == "Gain").Sum(t => t.Amount);
            decimal bankLoss = (decimal)allData.Where(t => t.Source == "Bank" && t.Type == "Loss").Sum(t => t.Amount);

            // 3. Timeframe Logic
            DateTime cutoffDate = timeframe switch
            {
                "daily" => DateTime.Today,
                "weekly" => DateTime.Today.AddDays(-7),
                _ => DateTime.Today.AddDays(-30)
            };

            // 3b. NEW: Monthly Budgeting Logic
            decimal monthlyGoal = 10000; // Set your limit here
            decimal thisMonthSpend = (decimal)allData
                .Where(t => t.Type == "Loss" && t.TransactionDate.Month == DateTime.Now.Month && t.TransactionDate.Year == DateTime.Now.Year)
                .Sum(t => t.Amount);

            // Calculate percentage (capped at 100 for the bar)
            ViewBag.BudgetPercent = Math.Min((thisMonthSpend / monthlyGoal) * 100, 100);
            ViewBag.MonthlyGoal = monthlyGoal;
            ViewBag.ThisMonthSpend = thisMonthSpend;

            // 4. Spending Analysis Data (Losses)
            var spendingByCategory = allData
                .Where(t => t.Type == "Loss" && t.TransactionDate >= cutoffDate)
                .GroupBy(t => t.Category ?? "Other")
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(t => t.Amount));

            // 5. Gains Analysis Data (Incomes)
            var gainsByCategory = allData
                .Where(t => t.Type == "Gain" && t.TransactionDate >= cutoffDate)
                .GroupBy(t => t.Category ?? "Other")
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(t => t.Amount));

            // 6. Get the last recorded date for the Stack view
            var lastDateRecord = allData.OrderByDescending(t => t.TransactionDate).FirstOrDefault();
            DateTime lastDate = lastDateRecord?.TransactionDate ?? DateTime.Today;

            // 7. Build the ViewModel
            var viewModel = new DashboardViewModel
            {
                TotalBalance = totalGain - totalLoss,
                CashBalance = cashGain - cashLoss,
                BankBalance = bankGain - bankLoss,
                RecentTransactions = allData.Where(t => t.TransactionDate.Date == lastDate.Date)
                                            .OrderByDescending(t => t.Id).ToList(),
                SpendingData = spendingByCategory,
                GainsData = gainsByCategory,
                TotalChange = timeframe,
                CashChange = "Live",
                BankChange = "Live"
            };

            return View(viewModel);
        }
        // GET: Day Entry Mode
        // GET: Shows the page
        [HttpGet]
        public IActionResult Create(string date)
        {
            // 1. Determine the working date
            DateTime workingDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);

            // 2. Fetch records for the selected date
            var dayEntries = _context.Transactions
                .Where(t => t.TransactionDate.Date == workingDate.Date)
                .OrderByDescending(t => t.Id)
                .ToList();

            // 3. IF EMPTY: Find the last date that actually has data
            if (!dayEntries.Any())
            {
                var lastEntry = _context.Transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefault();

                if (lastEntry != null)
                {
                    dayEntries = _context.Transactions
                        .Where(t => t.TransactionDate.Date == lastEntry.TransactionDate.Date)
                        .OrderByDescending(t => t.Id)
                        .ToList();

                    ViewBag.ShowingLastUsedDate = lastEntry.TransactionDate.ToString("MMM dd, yyyy");
                }
            }

            // 4. Set ViewBags for the View to use
            ViewBag.RecentTransactions = dayEntries;
            ViewBag.SelectedDate = workingDate.ToString("yyyy-MM-dd");

            // Return a SINGLE transaction model for the form
            return View(new Transaction { TransactionDate = workingDate });
        }

        [HttpPost]
        public IActionResult Create(Transaction transaction)
        {
            transaction.Amount = Math.Abs(transaction.Amount);

            if (ModelState.IsValid)
            {
                _context.Transactions.Add(transaction);
                _context.SaveChanges();
                return RedirectToAction("Create", new { date = transaction.TransactionDate.ToString("yyyy-MM-dd") });
            }

            // If validation fails, we MUST refill the list so the page doesn't crash
            var dayEntries = _context.Transactions
                .Where(t => t.TransactionDate.Date == transaction.TransactionDate.Date)
                .OrderByDescending(t => t.Id)
                .ToList();

            ViewBag.RecentTransactions = dayEntries;
            ViewBag.SelectedDate = transaction.TransactionDate.ToString("yyyy-MM-dd");
            return View(transaction);
        }

        // GET: Edit Page
        public IActionResult Edit(int id)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction == null) return NotFound();
            return View(transaction);
        }

        // POST: Update and return to the entry date
        [HttpPost]
        public IActionResult Edit(Transaction transaction, string returnUrl)
        {
            // 1. Keep the number positive
            transaction.Amount = Math.Abs(transaction.Amount);

            if (ModelState.IsValid)
            {
                _context.Transactions.Update(transaction);
                _context.SaveChanges();

                // 2. REDIRECT: Go back to exactly where you came from
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Records");
            }
            return View(transaction);
        }

        // GET: Delete and return to the entry date
        public IActionResult Delete(int id, string returnUrl = null)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction != null)
            {
                var date = transaction.TransactionDate.ToString("yyyy-MM-dd");
                _context.Transactions.Remove(transaction);
                _context.SaveChanges();

                // If we specified a returnUrl (like /Home/Records), go there. 
                // Otherwise, go back to the Daily Entry (Create) page.
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Create", new { date = date });
            }
            return RedirectToAction("Index");
        }

        public IActionResult Records(string search, string type, string category, string source, string date, int? month)
        {
            var query = _context.Transactions.AsQueryable();

            // 1. Bulletproof Search (Title, Category, Source)
            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower(); // Remove extra spaces and lowercase
                query = query.Where(t =>
                    (t.Title != null && t.Title.ToLower().Contains(s)) ||
                    (t.Category != null && t.Category.ToLower().Contains(s)) ||
                    (t.Source != null && t.Source.ToLower().Contains(s))
                );
            }

            // 2. Bulletproof Category Filter
            if (!string.IsNullOrEmpty(category))
            {
                string c = category.Trim().ToLower();
                // Use Contains instead of == to catch partial matches or records with extra spaces
                query = query.Where(t => t.Category != null && t.Category.ToLower().Contains(c));
            }

            // 3. Exact Filters (Type and Source are usually clean, but let's be safe)
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.Type.ToLower() == type.ToLower());

            if (!string.IsNullOrEmpty(source))
                query = query.Where(t => t.Source.ToLower() == source.ToLower());

            // 4. Date & Month logic remains the same...
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime filterDate))
                query = query.Where(t => t.TransactionDate.Date == filterDate.Date);

            if (month.HasValue && month > 0)
                query = query.Where(t => t.TransactionDate.Month == month.Value);

            // Persist state
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentType = type;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSource = source;
            ViewBag.CurrentDate = date;
            ViewBag.CurrentMonth = month;

            return View(query.OrderByDescending(t => t.TransactionDate).ToList());
        }
        public IActionResult ExportToCSV()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Date,Title,Amount,Type,Source,Category");

            var data = _context.Transactions.ToList();
            foreach (var item in data)
            {
                builder.AppendLine($"{item.TransactionDate:yyyy-MM-dd},{item.Title},{item.Amount},{item.Type},{item.Source},{item.Category}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "FinanceReport.csv");
        }
    }
}