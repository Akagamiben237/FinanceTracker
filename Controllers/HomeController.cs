using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Models;
using System.Linq;
using System.Globalization; // Added for CSV Culture
using System.IO;            // Added for StreamReader
using CsvHelper;            // Ensure you have CsvHelper NuGet package installed
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            // ... (Your existing Index logic kept exactly the same)
            var allData = _context.Transactions?.ToList() ?? [];

            decimal totalGain = (decimal)allData.Where(t => t.Type == "Gain").Sum(t => t.Amount);
            decimal totalLoss = (decimal)allData.Where(t => t.Type == "Loss").Sum(t => t.Amount);

            decimal cashGain = (decimal)allData.Where(t => t.Source == "Cash" && t.Type == "Gain").Sum(t => t.Amount);
            decimal cashLoss = (decimal)allData.Where(t => t.Source == "Cash" && t.Type == "Loss").Sum(t => t.Amount);

            decimal bankGain = (decimal)allData.Where(t => t.Source == "Bank" && t.Type == "Gain").Sum(t => t.Amount);
            decimal bankLoss = (decimal)allData.Where(t => t.Source == "Bank" && t.Type == "Loss").Sum(t => t.Amount);

            DateTime cutoffDate = timeframe switch
            {
                "daily" => DateTime.Today,
                "weekly" => DateTime.Today.AddDays(-7),
                _ => DateTime.Today.AddDays(-30)
            };

            var budgetSetting = _context.ApplicationSettings?.FirstOrDefault(s => s.Key == "MonthlyBudget");
            decimal monthlyGoal = budgetSetting != null && decimal.TryParse(budgetSetting.Value, out decimal val)
                                  ? val
                                  : 10000;
            decimal thisMonthSpend = (decimal)allData
                .Where(t => t.Type == "Loss" && t.TransactionDate.Month == DateTime.Now.Month && t.TransactionDate.Year == DateTime.Now.Year)
                .Sum(t => t.Amount);

            ViewBag.BudgetPercent = Math.Min((thisMonthSpend / monthlyGoal) * 100, 100);
            ViewBag.MonthlyGoal = monthlyGoal;
            ViewBag.ThisMonthSpend = thisMonthSpend;

            var spendingByCategory = allData
                .Where(t => t.Type == "Loss" && t.TransactionDate >= cutoffDate)
                .GroupBy(t => t.Category ?? "Other")
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(t => t.Amount));

            var gainsByCategory = allData
                .Where(t => t.Type == "Gain" && t.TransactionDate >= cutoffDate)
                .GroupBy(t => t.Category ?? "Other")
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(t => t.Amount));

            var spendingHistory = allData
                .Where(t => t.Type == "Loss" && t.TransactionDate >= cutoffDate)
                .GroupBy(t => t.TransactionDate.ToString("MMM dd"))
                .ToDictionary(g => g.Key, g => (decimal)g.Sum(t => t.Amount));

            var lastDateRecord = allData.OrderByDescending(t => t.TransactionDate).FirstOrDefault();
            DateTime lastDate = lastDateRecord?.TransactionDate ?? DateTime.Today;

            var viewModel = new DashboardViewModel
            {
                TotalBalance = totalGain - totalLoss,
                CashBalance = cashGain - cashLoss,
                BankBalance = bankGain - bankLoss,
                RecentTransactions = allData.Where(t => t.TransactionDate.Date == lastDate.Date)
                                            .OrderByDescending(t => t.Id).ToList(),
                SpendingData = spendingByCategory,
                GainsData = gainsByCategory,
                SpendingHistory = spendingHistory,
                TotalChange = timeframe,
                CashChange = "Live",
                BankChange = "Live"
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create(string date)
        {
            if (!_context.Categories.Any())
            {
                var defaults = new List<Category>
                {
                    new Category { Name = "Food", Type = "Loss", Icon = "🍕" },
                    new Category { Name = "Fuel", Type = "Loss", Icon = "⛽" },
                    new Category { Name = "Grocery", Type = "Loss", Icon = "🛒" },
                    new Category { Name = "Rent", Type = "Loss", Icon = "🏠" },
                    new Category { Name = "Medical", Type = "Loss", Icon = "🏥" },
                    new Category { Name = "Salary", Type = "Gain", Icon = "💰" },
                    new Category { Name = "Business", Type = "Gain", Icon = "🏢" },
                    new Category { Name = "Freelance", Type = "Gain", Icon = "🧑‍💻" },
                    new Category { Name = "Stocks", Type = "Gain", Icon = "📈" },
                    new Category { Name = "Other", Type = "Loss", Icon = "📦" }
                };
                _context.Categories.AddRange(defaults);
                _context.SaveChanges();
            }

            DateTime workingDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
            var dayEntries = _context.Transactions
                .Where(t => t.TransactionDate.Date == workingDate.Date)
                .OrderByDescending(t => t.Id)
                .ToList();

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

            ViewBag.RecentTransactions = dayEntries;
            ViewBag.SelectedDate = workingDate.ToString("yyyy-MM-dd");
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();

            return View(new Transaction
            {
                TransactionDate = workingDate,
                Title = string.Empty,
                Category = string.Empty,
                Type = string.Empty,
                Source = string.Empty
            });
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

            var dayEntries = _context.Transactions
                .Where(t => t.TransactionDate.Date == transaction.TransactionDate.Date)
                .OrderByDescending(t => t.Id)
                .ToList();

            ViewBag.RecentTransactions = dayEntries;
            ViewBag.SelectedDate = transaction.TransactionDate.ToString("yyyy-MM-dd");
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(transaction);
        }

        public IActionResult Edit(int id)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction == null) return NotFound();
            return View(transaction);
        }

        [HttpPost]
        public IActionResult Edit(Transaction transaction, string returnUrl)
        {
            transaction.Amount = Math.Abs(transaction.Amount);

            if (ModelState.IsValid)
            {
                _context.Transactions.Update(transaction);
                _context.SaveChanges();

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Records");
            }
            return View(transaction);
        }

        public IActionResult Delete(int id, string? returnUrl = null)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction != null)
            {
                var date = transaction.TransactionDate.ToString("yyyy-MM-dd");
                _context.Transactions.Remove(transaction);
                _context.SaveChanges();

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

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(t =>
                    (t.Title != null && t.Title.ToLower().Contains(s)) ||
                    (t.Category != null && t.Category.ToLower().Contains(s)) ||
                    (t.Source != null && t.Source.ToLower().Contains(s))
                );
            }

            if (!string.IsNullOrEmpty(category))
            {
                string c = category.Trim().ToLower();
                query = query.Where(t => t.Category != null && t.Category.ToLower().Contains(c));
            }

            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.Type.ToLower() == type.ToLower());

            if (!string.IsNullOrEmpty(source))
                query = query.Where(t => t.Source.ToLower() == source.ToLower());

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime filterDate))
                query = query.Where(t => t.TransactionDate.Date == filterDate.Date);

            if (month.HasValue && month > 0)
                query = query.Where(t => t.TransactionDate.Month == month.Value);

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
                // Ensures date is yyyy-MM-dd for clean re-import
                builder.AppendLine($"{item.TransactionDate:yyyy-MM-dd},{item.Title},{item.Amount},{item.Type},{item.Source},{item.Category}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "FinanceReport.csv");
        }

        [HttpPost]
        public async Task<IActionResult> ImportCSV(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please select a file.");

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>().ToList();

                    // Fetch existing data once to compare and speed up the process
                    var existingTransactions = _context.Transactions.ToList();

                    foreach (var row in records)
                    {
                        var dict = (IDictionary<string, object>)row;

                        DateTime date = DateTime.Parse(dict["Date"].ToString());
                        string title = dict["Title"].ToString();
                        float amount = float.Parse(dict["Amount"].ToString(), CultureInfo.InvariantCulture);

                        // DUPLICATE CHECK: See if this exact record already exists
                        bool isDuplicate = existingTransactions.Any(t =>
                            t.TransactionDate.Date == date.Date &&
                            t.Title == title &&
                            Math.Abs(t.Amount - amount) < 0.01 // Use a small margin for float comparison
                        );

                        if (!isDuplicate)
                        {
                            var transaction = new Transaction
                            {
                                TransactionDate = date,
                                Title = title,
                                Amount = amount,
                                Type = dict["Type"].ToString(),
                                Source = dict["Source"].ToString(),
                                Category = dict["Category"].ToString()
                            };

                            _context.Transactions.Add(transaction);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Records");
            }
            catch (Exception)
            {
                TempData["Error"] = "Import failed. Check CSV format.";
                return RedirectToAction("Records");
            }
        }

        public IActionResult Settings()
        {
            var budgetSetting = _context.ApplicationSettings?.FirstOrDefault(s => s.Key == "MonthlyBudget");
            ViewBag.MonthlyBudget = budgetSetting?.Value ?? "10000";
            return View();
        }

        [HttpPost]
        public IActionResult UpdateSettings(string monthlyBudget)
        {
            if (decimal.TryParse(monthlyBudget, out decimal parsedBudget))
            {
                var setting = _context.ApplicationSettings?.FirstOrDefault(s => s.Key == "MonthlyBudget");
                if (setting == null)
                {
                    setting = new ApplicationSetting { Key = "MonthlyBudget", Value = parsedBudget.ToString() };
                    _context.ApplicationSettings?.Add(setting);
                }
                else
                {
                    setting.Value = parsedBudget.ToString();
                    _context.ApplicationSettings?.Update(setting);
                }
                _context.SaveChanges();
            }
            return RedirectToAction("Settings");
        }
    }
}