using StockTradeBot;
using StockTradeBot.Models;

public class BackTest
{
    private decimal InitialBalance { get; set; }
    private decimal Margin { get; set; }
    private decimal Commission { get; set; }
    private decimal MySize { get; set; }
    private List<CandleStick> Data { get; set; }
    private List<CandleStick> Entries { get; set; }
    private List<decimal> BalanceHistory { get; set; }

    public BackTest(List<CandleStick> data,List<CandleStick>entries, decimal initialBalance = 5000m, decimal margin = 0.5m, 
                   decimal commission = 0.0002m, decimal mySize = 0.1m)
    {
        Data = data;
        Entries = entries;
        InitialBalance = initialBalance;
        Margin = margin;
        Commission = commission;
        MySize = mySize;
        BalanceHistory = new List<decimal> { initialBalance };
    }

     public BacktestResult RunBacktest(decimal slPerc, decimal tpPerc)
    {
        var result = new BacktestResult 
        { 
            SLPerc = slPerc,
            TPPerc = tpPerc
        };

        decimal currentBalance = InitialBalance;
        decimal peakBalance = InitialBalance;
        BalanceHistory.Clear();
        BalanceHistory.Add(currentBalance);

        for (int i = 2; i < Data.Count; i++)
        {
            if (GetSignal(i) == 1)
            {
                var candle = Data[i];
               
                decimal riskAmount = currentBalance * MySize;
                decimal leveragedPosition = riskAmount * Margin;
                decimal units = leveragedPosition / candle.ClosePrice;

                var trade = new Trade
                {
                    EntryPrice = candle.ClosePrice,
                    EntryTime = candle.OpenTime,
                    Size = units,
                    StopLoss = candle.ClosePrice * (1 - slPerc),
                    TakeProfit = candle.ClosePrice * (1 + tpPerc),
                    
                };

              
                decimal entryCommission = (units * candle.ClosePrice) * Commission;
                currentBalance -= entryCommission;

                bool foundExit = false;
                for (int j = i + 1; j < Math.Min(i + 100, Data.Count) && !foundExit; j++)
                {
                    var exitCandle = Data[j];
                    
                    if (exitCandle.LowPrice <= trade.StopLoss)
                    {
                      
                        trade.ExitPrice = trade.StopLoss;
                        trade.ExitTime = exitCandle.OpenTime;
                        
                        decimal exitValue = units * trade.StopLoss;
                        decimal exitCommission = exitValue * Commission;
                        
                        
                        decimal actualLoss = (trade.StopLoss - trade.EntryPrice) * units;
                        currentBalance += actualLoss - exitCommission;
                        
                        trade.Profit = actualLoss - entryCommission - exitCommission;
                        trade.ProfitPercentage = -slPerc * 100; 
                        trade.IsWin = false;
                        
                        foundExit = true;
                    }
                    else if (exitCandle.HighPrice >= trade.TakeProfit)
                    {
                        
                        trade.ExitPrice = trade.TakeProfit;
                        trade.ExitTime = exitCandle.OpenTime;
                        
                        decimal exitValue = units * trade.TakeProfit;
                        decimal exitCommission = exitValue * Commission;
                        
                        
                        decimal actualProfit = (trade.TakeProfit - trade.EntryPrice) * units;
                        currentBalance += actualProfit - exitCommission;
                        
                        trade.Profit = actualProfit - entryCommission - exitCommission;
                        trade.ProfitPercentage = tpPerc * 100;
                        trade.IsWin = true;
                        
                        foundExit = true;
                    }

                    if (foundExit)
                    {
                        
                        peakBalance = Math.Max(peakBalance, currentBalance);
                        result.Trades.Add(trade);
                        BalanceHistory.Add(currentBalance);
                        i = j; 
                        break;
                    }
                }
            }
        }

       
        if (result.Trades.Any())
        {
            result.NumberOfTrades = result.Trades.Count;
            result.ReturnPercentage = ((currentBalance - InitialBalance) / InitialBalance) * 100;
            result.WinRate = ((decimal)result.Trades.Count(t => t.IsWin) / result.NumberOfTrades) * 100;
            
            var winningTrades = result.Trades.Where(t => t.IsWin).ToList();
            var losingTrades = result.Trades.Where(t => !t.IsWin).ToList();
            
            result.BestTrade = winningTrades.Any() ? winningTrades.Max(t => t.ProfitPercentage) : 0;
            result.WorstTrade = losingTrades.Any() ? losingTrades.Min(t => t.ProfitPercentage) : 0;
            result.AvgTrade = result.Trades.Average(t => t.ProfitPercentage);
            
           
            decimal maxDrawdown = 0;
            decimal currentDrawdown = 0;
            decimal runningPeak = InitialBalance;

            foreach (decimal balance in BalanceHistory)
            {
                if (balance > runningPeak)
                {
                    runningPeak = balance;
                    currentDrawdown = 0;
                }
                else
                {
                    currentDrawdown = ((runningPeak - balance) / runningPeak) * 100;
                    maxDrawdown = Math.Max(maxDrawdown, currentDrawdown);
                }
            }

            result.MaxDrawdown = maxDrawdown;
            
           
            if (BalanceHistory.Count > 1)
            {
                decimal sumDrawdown = 0;
                int drawdownCount = 0;
                runningPeak = BalanceHistory[0];

                for (int i = 1; i < BalanceHistory.Count; i++)
                {
                    if (BalanceHistory[i] < runningPeak)
                    {
                        decimal drawdown = ((runningPeak - BalanceHistory[i]) / runningPeak) * 100;
                        sumDrawdown += drawdown;
                        drawdownCount++;
                    }
                    else
                    {
                        runningPeak = BalanceHistory[i];
                    }
                }

                result.AvgDrawdown = drawdownCount > 0 ? sumDrawdown / drawdownCount : 0;
            }

           
            decimal grossProfit = winningTrades.Sum(t => t.Profit);
            decimal grossLoss = Math.Abs(losingTrades.Sum(t => t.Profit));
            result.ProfitFactor = grossLoss != 0 ? grossProfit / grossLoss : 0;
        }

        return result;
    }

     //Parametry do utworzenia sygnalu
    private int GetSignal(int currentIndex)
    {
        if (currentIndex < 3) return 0;

        var current = Data[currentIndex];
        var position_1 = Data[currentIndex - 1];
        var position_2 = Data[currentIndex - 2];

        bool c1 = current.HighPrice > current.ClosePrice;
        bool c2 = current.ClosePrice > position_2.HighPrice;
        bool c3 = position_2.HighPrice > position_1.HighPrice;
        bool c4 = position_1.HighPrice > current.LowPrice;
        bool c5 = current.LowPrice > position_2.LowPrice;
        bool c6 = position_2.LowPrice > position_1.LowPrice;
        
        if (c1 && c2 && c3 && c4 && c5 && c6 )
            return 1;

        return 0;
    }

    public (BacktestResult BestResult, List<(decimal SL, decimal TP, decimal Return)> AllResults) Optimize()
    {
        var allResults = new List<(decimal SL, decimal TP, decimal Return)>();
        BacktestResult bestResult = null;
        decimal bestReturn = decimal.MinValue;

        for (decimal sl = 0.01m; sl <= 0.1m; sl += 0.01m)
        {
            for (decimal tp = 0.01m; tp <= 0.1m; tp += 0.01m)
            {
                var result = RunBacktest(sl, tp);
                allResults.Add((sl, tp, result.ReturnPercentage));

                if (result.ReturnPercentage > bestReturn && result.NumberOfTrades >= 10)
                {
                    bestReturn = result.ReturnPercentage;
                    bestResult = result;
                }
            }
        }

        allResults = allResults.OrderByDescending(r => r.Return).ToList();

        return (bestResult ?? new BacktestResult(), allResults);
    }
}