using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using StockTradeBot.Models;
using StockTradeBot.Api;

namespace StockTradeBot;

public class Program
{
    
    public static void Main()
    {
        var appTask = Task.Run(() => StartApi());
        appTask.Wait();
        Task.Run(() => ShowStrategy());
        Task.Run(() => ShowBackTesting());
        Task.Run((() => StartApi()));
        //Task.Run((() => StartWeb()));
        var (app,appTaskResult) = appTask.Result;
        

        app.Run();
    }

    private static void StartWeb()
    {
        string stockTradeBotProjectFolder = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "../StockTradeBot");
        
        // Ścieżka do pliku .csproj (w projekcie StockTradeBot)
        string projectFilePath = Path.Combine(stockTradeBotProjectFolder, "StockTradeBot.csproj");

        // Uruchomienie procesu dotnet, wskazując projekt StockTradeBot
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet", 
            Arguments = $"run --project \"{projectFilePath}\"", 
            WorkingDirectory = stockTradeBotProjectFolder // Ustawienie katalogu roboczego na folder projektu
        };

        // Uruchomienie procesu
        Process.Start(startInfo);
    }
    private static void ShowStrategy()
    {
        
        Console.WriteLine("Starting Strategy...");

   
        Strategy strategy = new Strategy();


        var data = strategy.LoadData();

      
        var longs = strategy.CalculatePosition(true, data);
        var shorts = strategy.CalculatePosition(false, data);

   
        Console.WriteLine("Long Positions Found:");
        foreach (var longPosition in longs)
        {
            Console.WriteLine($"OpenTime: {longPosition.OpenTime}, ClosePrice: {longPosition.ClosePrice}");
        }

      
    }


    private static void ShowBackTesting()
    {
        
        Strategy strategy = new Strategy();
        var data = strategy.LoadData();
        var longs = strategy.CalculatePosition(true, data); 
        Console.WriteLine("\nStarting Backtesting...");
        
        var backtest = new BackTest(data,longs);
        
       
        var initialResult = backtest.RunBacktest(0.06m, 0.10m);
        Console.WriteLine("\nInitial Backtest Results (SL: 6%, TP: 9%):");
        PrintBacktestResults(initialResult);

     
        Console.WriteLine("\nFinding best parameters...");
        var (bestResult, allResults) = backtest.Optimize();
        
        Console.WriteLine("\nOptimization Results:");
        Console.WriteLine($"Best Parameters Found:");
        Console.WriteLine($"Stop Loss: {bestResult.SLPerc:P2}");
        Console.WriteLine($"Take Profit: {bestResult.TPPerc:P2}");
        PrintBacktestResults(bestResult);

        
        /*
        Console.WriteLine("\nTop 5 Parameter Combinations:");
        var top5 = allResults.Take(5);
        foreach (var result in top5)
        {
            Console.WriteLine($"SL: {result.SL:P2}, TP: {result.TP:P2}, Return: {result.Return:F2}%");
        }
        */
        Console.WriteLine("\nStrategy and Backtesting Complete.");
    }

   
    private static (WebApplication,ApiStartup) StartApi()
    {
        var builder = WebApplication.CreateBuilder();
        ApiStartup apisetStartup = new ApiStartup();
        var webappbuild = apisetStartup.ConfigureApi(builder);
        var app = builder.Build();




        apisetStartup.MapRoutes(app);
        //app.Run();
        return (app,apisetStartup);

    }
    private static void PrintBacktestResults(BacktestResult result)
    {
        Console.WriteLine($"Total Return: {result.ReturnPercentage:F2}%");
        Console.WriteLine($"Number of Trades: {result.NumberOfTrades}");
        Console.WriteLine($"Win Rate: {result.WinRate:F2}%");
        Console.WriteLine($"Maximum Drawdown: {result.MaxDrawdown:F2}%");
        Console.WriteLine($"Average Drawdown: {result.AvgDrawdown:F2}%");
        Console.WriteLine($"Best Trade: {result.BestTrade:F2}%");
        Console.WriteLine($"Worst Trade: {result.WorstTrade:F2}%");
        Console.WriteLine($"Average Trade: {result.AvgTrade:F2}%");
        
        if (result.Trades.Any())
        {
            var winningTrades = result.Trades.Count(t => t.IsWin);
            var losingTrades = result.Trades.Count - winningTrades;
            
            Console.WriteLine($"\nDetailed Trade Statistics:");
            Console.WriteLine($"Winning Trades: {winningTrades}");
            Console.WriteLine($"Losing Trades: {losingTrades}");

            
            decimal avgWinSize = result.Trades.Any(t => t.IsWin) 
                ? result.Trades.Where(t => t.IsWin).Average(t => t.ProfitPercentage) 
                : 0;
                
            decimal avgLossSize = result.Trades.Any(t => !t.IsWin) 
                ? result.Trades.Where(t => !t.IsWin).Average(t => t.ProfitPercentage) 
                : 0;

            Console.WriteLine($"Average Win Size: {avgWinSize:F2}%");
            Console.WriteLine($"Average Loss Size: {avgLossSize:F2}%");

            if (result.Trades.Count > 0)
            {
                Console.WriteLine("\nLast 5 trades:");
                foreach (var trade in result.Trades.TakeLast(Math.Min(5, result.Trades.Count)))
                {
                    Console.WriteLine($"Entry: {trade.EntryTime}, Exit: {trade.ExitTime}, " +
                                    $"Profit: {trade.ProfitPercentage:F2}%, " +
                                    $"Result: {(trade.IsWin ? "Win" : "Loss")}");
                }
            }

            if (result.ProfitFactor > 0)
            {
                Console.WriteLine($"\nProfit Factor: {result.ProfitFactor:F2}");
            }

            Console.WriteLine($"Expected Value per Trade: {result.ExpectedValue:F2}%");
        }
        else
        {
            Console.WriteLine("\nNo trades were executed during the backtest period.");
        }
    }
}