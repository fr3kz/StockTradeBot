using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

       
        builder.Services.AddControllersWithViews();

        
        //Odpalanie ostatniej dll StockTradeBot
        builder.Services.AddHostedService<BackgroundTaskService>();
        builder.WebHost.ConfigureKestrel(options =>
        {
             options.ListenLocalhost(7049, listenOptions =>
    {
        listenOptions.UseHttps(); // Włącz HTTPS na porcie 7049
    });
        });
        var app = builder.Build();

       
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}



public class BackgroundTaskService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        string stockTradeBotDllPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "StockTradeBot/bin/Debug/net8.0/StockTradeBot.dll");

        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet", 
            Arguments = $"\"{stockTradeBotDllPath}\"", 
            UseShellExecute = false,
            CreateNoWindow = false
        };
        
        Process process = new Process
        {
            StartInfo = startInfo
        };

        
        process.Start();
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
