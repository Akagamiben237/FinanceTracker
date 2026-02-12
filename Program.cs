using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using FinanceTracker.Models;

var builder = WebApplication.CreateBuilder(args);

// Put the DB in the per-user LocalApplicationData folder to avoid permission issues
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var appFolder = Path.Combine(localAppData, "FinanceTracker");
Directory.CreateDirectory(appFolder); // ensure directory exists
var dbPath = Path.Combine(appFolder, "FinanceData.db");

// 1. Link your DB Context to SQLite (absolute path)
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// If this project contains Razor Pages, register them (preferred for Razor Pages projects)
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Do not hardcode the listening URL here; prefer configuration (ASPNETCORE_URLS, launchSettings, or --urls)

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

// Correct middleware order for endpoint routing
app.UseRouting();
app.UseAuthorization();

// Map endpoints for controllers and Razor Pages
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Register a safe browser launch once the host has started and only if running interactively
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        // Get actual bound addresses (populated after the server starts)
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses?.ToArray() ?? Array.Empty<string>();

        if (addresses.Length == 0)
        {
            // Fallback to ASPNETCORE_URLS or a sensible default for developer convenience
            var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (!string.IsNullOrWhiteSpace(envUrls))
            {
                addresses = envUrls.Split(';', StringSplitOptions.RemoveEmptyEntries);
              }
        }

        if (addresses.Length == 0)
        {
            Console.WriteLine("Server started but no bound addresses were discovered.");
            return;
        }

        // Log all addresses so you can inspect them if console is hidden
        Console.WriteLine("Server listening on:");
        foreach (var a in addresses) Console.WriteLine($"  {a}");

        // Only attempt to open the browser if an interactive desktop is available
        if (Environment.UserInteractive)
        {
            // Prefer the first address
            var address = addresses[0];
            var psi = new ProcessStartInfo { FileName = address, UseShellExecute = true };
            Process.Start(psi);
        }
        else
        {
            Console.WriteLine("Interactive session unavailable, skipping automatic browser launch.");
        }
    }
    catch (Exception ex)
    {
        // Keep the logging so hidden consoles can record why the browser failed to open
        Console.WriteLine($"Failed to launch browser: {ex.Message}");
    }
});

// Start without blocking, then wait for shutdown
try
{
    await app.StartAsync();
}
catch (Exception ex) when (
    ex is System.IO.IOException ||
    (ex.InnerException != null && ex.InnerException.GetType().Name.Contains("AddressInUse", StringComparison.OrdinalIgnoreCase)) ||
    (ex.InnerException != null && ex.InnerException is System.Net.Sockets.SocketException)
)
{
    Console.WriteLine($"Failed to start web host: {ex.Message}");
    Console.WriteLine("A process is already using the configured port. Stop that process or set ASPNETCORE_URLS to a different port.");
    return;
}

await app.WaitForShutdownAsync();