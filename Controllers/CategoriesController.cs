using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Models;
using System.Linq;

namespace FinanceTracker.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly FinanceDbContext _context;

        public CategoriesController(FinanceDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories?.OrderBy(c => c.Type).ThenBy(c => c.Name).ToList() ?? [];
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories?.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                // 1. Find all transactions that were using this category
                var orphanTransactions = _context.Transactions
                    .Where(t => t.Category == category.Name)
                    .ToList();

                // 2. Re-assign them to "Other" before deleting the category
                foreach (var trans in orphanTransactions)
                {
                    trans.Category = "Other"; // Or "Deleted Category"
                }

                _context.Categories.Remove(category);
                _context.SaveChanges();

                TempData["Success"] = $"Category '{category.Name}' deleted. {orphanTransactions.Count} records moved to 'Other'.";
            }
            return RedirectToAction("Index", "Categories"); // Redirects back to your Category list
        }
    }
}
