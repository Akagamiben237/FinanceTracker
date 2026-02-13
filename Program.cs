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

// Create a unique name for your app lock
using Mutex mutex = new Mutex(true, "FinanceTracker_Unique_Key", out bool isNewInstance);

if (!isNewInstance)
{
    // Another instance is already running! 
    // We can't easily get its port here, so we will stick to a FIXED port
    // for this strategy to work perfectly.
    Process.Start(new ProcessStartInfo { FileName = "http://127.0.0.1:5000", UseShellExecute = true });
    return; // Close this second .exe immediately
}


var builder = WebApplication.CreateBuilder(args);

// FIX 1: Tell Kestrel to find ANY available port if port 5000 is busy
// CHANGE THIS:
builder.WebHost.ConfigureKestrel(options =>
{
    // Use port 5000 consistently so the "Single Instance" logic knows where to go
    options.Listen(System.Net.IPAddress.Loopback, 5000); 
});
var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var appFolder = Path.Combine(localAppData, "FinanceTracker");
Directory.CreateDirectory(appFolder);
var dbPath = Path.Combine(appFolder, "FinanceData.db");

builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<FinanceDbContext>();
    context.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// FIX 2: Create a route that stops the .exe when called
app.MapPost("/shutdown", (IHostApplicationLifetime lifetime) =>
{
    lifetime.StopApplication();
    return Results.Ok();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        // Get the first bound address (the one with the random port)
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