using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using StockTradeBot.Api.Interfaces;
using StockTradeBot.Models;

namespace StockTradeBot.Api
{
    public  class ApiStartup: IApi
    {
        //private string _connectionString = "Data Source=app.db;";
        private static string databasePath = Path.Combine(
            Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.FullName ?? string.Empty,
            "app.db");
        private string _connectionString = $"Data Source={databasePath};";
        

        public WebApplicationBuilder ConfigureApi(WebApplicationBuilder builder)
        {
           
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.WriteIndented = true;
                options.SerializerOptions.IncludeFields = true;
                
            });

            builder.Services.AddControllersWithViews().AddRazorOptions(options =>
            {
                options.ViewLocationFormats.Add("/Api/Views/{1}/{0}.cshtml"); // Jeśli masz niestandardową lokalizację
            });  
                
            builder.Services.AddRazorPages();
            
            
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Trades (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EntryPrice DECIMAL(18, 2),
                ExitPrice DECIMAL(18, 2),
                StopLoss DECIMAL(18, 2),
                TakeProfit DECIMAL(18, 2),
                EntryTime DATETIME,
                ExitTime DATETIME,
                Profit DECIMAL(18, 2),
                ProfitPercentage DECIMAL(18, 2),
                IsWin BOOLEAN,
                Size DECIMAL(18, 2),
                PositionValue DECIMAL(18, 2),
                RiskAmount DECIMAL(18, 2),
                RewardAmount DECIMAL(18, 2)
            );
        ";

                using (var command = new SqliteCommand(createTableQuery, connection))  
                {
                    command.ExecuteNonQuery();
                }
            }
            return builder;
        }

        public  void MapRoutes(WebApplication app)
        {
            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage(); 
                app.UseDeveloperExceptionPage();

            }
            
            app.UseStaticFiles();
            app.UseRouting();
         // lista wszystkich tradow open
            app.MapGet("/trades", () =>
            {
                List<Trade> trades = new List<Trade>();
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT EntryPrice, ExitPrice, StopLoss, TakeProfit, EntryTime, ExitTime, Profit, ProfitPercentage, IsWin, Size, RiskAmount, RewardAmount FROM Trades";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var trade = new Trade
                            {
                                EntryPrice = reader.GetDecimal(0),
                                ExitPrice = reader.GetDecimal(1),
                                StopLoss = reader.GetDecimal(2),
                                TakeProfit = reader.GetDecimal(3),
                                EntryTime = reader.GetDateTime(4),
                                ExitTime = reader.GetDateTime(5),
                                Profit = reader.GetDecimal(6),
                                ProfitPercentage = reader.GetDecimal(7),
                                IsWin = reader.GetBoolean(8),
                                Size = reader.GetDecimal(9),
                                RiskAmount = reader.GetDecimal(10),
                                RewardAmount = reader.GetDecimal(11)
                            };

                            trades.Add(trade);
                        }
                    }
                }

                //TERAZ trzeba serializowac te dane
                return Results.Json(trades);
                
                
            });

            // historia wszystkich tradow
            app.MapGet("/trades/history", () => "Hello, World!");

            // dodanie trade
            app.MapPost("/trades/add", async (Trade trade) =>
            {

                if (!CheckForDuplicates(trade))
                {
                    return Results.Json("Duplicate");
                } 
                
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
        INSERT INTO Trades 
        (EntryPrice, ExitPrice, StopLoss, TakeProfit, EntryTime, ExitTime, Profit, ProfitPercentage, IsWin, Size, RiskAmount, RewardAmount) 
        VALUES 
        ($EntryPrice, $ExitPrice, $StopLoss, $TakeProfit, $EntryTime, $ExitTime, $Profit, $ProfitPercentage, $IsWin, $Size, $RiskAmount, $RewardAmount);";

               
                command.Parameters.AddWithValue("$EntryPrice", trade.EntryPrice);
                command.Parameters.AddWithValue("$ExitPrice", trade.ExitPrice);
                command.Parameters.AddWithValue("$StopLoss", trade.StopLoss);
                command.Parameters.AddWithValue("$TakeProfit", trade.TakeProfit);
                command.Parameters.AddWithValue("$EntryTime", trade.EntryTime);
                command.Parameters.AddWithValue("$ExitTime", trade.ExitTime);
                command.Parameters.AddWithValue("$Profit", trade.Profit);
                command.Parameters.AddWithValue("$ProfitPercentage", trade.ProfitPercentage);
                command.Parameters.AddWithValue("$IsWin", trade.IsWin);
                command.Parameters.AddWithValue("$Size", trade.Size);
                command.Parameters.AddWithValue("$RiskAmount", trade.RiskAmount);
                command.Parameters.AddWithValue("$RewardAmount", trade.RewardAmount);

                
                await command.ExecuteNonQueryAsync();
                return Results.Ok("Trade added.");
            });

            
            // edytowanie trade

           
        }


        public bool CheckForDuplicates(Trade trade)
        {
            List<Trade> trades = new List<Trade>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                SELECT EntryPrice, ExitPrice, EntryTime FROM Trades
                ";
            
                using (var command = new SqliteCommand(query, connection))
                {
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
            
                        if (trade.EntryPrice == reader.GetDecimal(0) && trade.EntryTime == reader.GetDateTime(2))
                        {
                            return false;
                        }
                        
                    }

                    return true;
                }
            } 
        }
    }
}
