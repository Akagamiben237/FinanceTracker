using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Models;

namespace FinanceTracker.Controllers
{
    public class TodosController : Controller
    {
        private readonly FinanceDbContext _context;

        public TodosController(FinanceDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string filter = "all")
        {
            var query = _context.TodoItems.AsQueryable();

            query = filter switch
            {
                "active" => query.Where(t => !t.IsCompleted),
                "completed" => query.Where(t => t.IsCompleted),
                "financial" => query.Where(t => t.IsFinancial),
                "overdue" => query.Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate < DateTime.Today),
                _ => query
            };

            ViewBag.CurrentFilter = filter;
            ViewBag.TotalCount = _context.TodoItems.Count();
            ViewBag.ActiveCount = _context.TodoItems.Count(t => !t.IsCompleted);
            ViewBag.CompletedCount = _context.TodoItems.Count(t => t.IsCompleted);
            ViewBag.FinancialCount = _context.TodoItems.Count(t => t.IsFinancial);
            ViewBag.Categories = _context.Categories.ToList();

            return View(query.OrderByDescending(t => t.CreatedDate).ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View(new TodoItem());
        }

        [HttpPost]
        public IActionResult Create(TodoItem todo)
        {
            if (ModelState.IsValid)
            {
                todo.CreatedDate = DateTime.Now;
                _context.TodoItems.Add(todo);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(todo);
        }

        [HttpPost]
        public IActionResult QuickAdd(string title, string priority = "Medium", DateTime? dueDate = null)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var todo = new TodoItem
                {
                    Title = title,
                    Priority = priority,
                    DueDate = dueDate,
                    CreatedDate = DateTime.Now,
                    IsCompleted = false,
                    IsFinancial = false
                };
                _context.TodoItems.Add(todo);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult ToggleComplete(int id)
        {
            var todo = _context.TodoItems.Find(id);
            if (todo != null)
            {
                todo.IsCompleted = !todo.IsCompleted;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var todo = _context.TodoItems.Find(id);
            if (todo != null)
            {
                _context.TodoItems.Remove(todo);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var todo = _context.TodoItems.Find(id);
            if (todo == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            return View(todo);
        }

        [HttpPost]
        public IActionResult Edit(TodoItem todo)
        {
            if (ModelState.IsValid)
            {
                _context.TodoItems.Update(todo);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(todo);
        }
    }
}
