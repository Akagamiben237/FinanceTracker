using Microsoft.EntityFrameworkCore;
using FinanceTracker.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Link your DB Context to SQLite
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite("Data Source=FinanceData.db"));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 2. The "Automatic DB" Logic
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<FinanceDbContext>();

    // This creates the database file and tables on the fly if they are missing
    context.Database.EnsureCreated();
}

app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();