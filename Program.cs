using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using FinanceTracker.Models;
using Microsoft.Extensions.Hosting;
using System.Threading;

// 1. Single Instance Logic: Prevents opening multiple copies of the same app
using Mutex mutex = new Mutex(true, "FinanceTracker_Unique_Key", out bool isNewInstance);

if (!isNewInstance)
{
    // If already running, just open the browser and kill this second process
    Process.Start(new ProcessStartInfo { FileName = "http://127.0.0.1:5000", UseShellExecute = true });
    return;
}

var builder = WebApplication.CreateBuilder(args);

// 2. FIXED PORT: Keeping it on 5000 so the "Single Instance" logic always works
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Loopback, 5000);
});

// 3. DATABASE REPAIR: Break the link to central AppData
// This line makes the DB live in the SAME folder as your FinanceTracker.exe
var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FinanceTracker.db");

builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Ensure the database file is created locally on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<FinanceDbContext>();
    context.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 5. Shutdown Route: Allows closing the app gracefully
app.MapPost("/shutdown", (IHostApplicationLifetime lifetime) =>
{
    lifetime.StopApplication();
    return Results.Ok();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 6. Browser Auto-Launch
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var address = addressesFeature?.Addresses?.FirstOrDefault();

        if (!string.IsNullOrEmpty(address) && Environment.UserInteractive)
        {
            Process.Start(new ProcessStartInfo { FileName = address, UseShellExecute = true });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to launch browser: {ex.Message}");
    }
});

await app.RunAsync();