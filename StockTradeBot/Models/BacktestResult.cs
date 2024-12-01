namespace StockTradeBot.Models;

public class BacktestResult
{
    public decimal ReturnPercentage { get; set; }
    public int NumberOfTrades { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal AvgDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public decimal BestTrade { get; set; }
    public decimal WorstTrade { get; set; }
    public decimal AvgTrade { get; set; }
    public decimal SLPerc { get; set; }
    public decimal TPPerc { get; set; }
    public List<Trade> Trades { get; set; } = new();
    public decimal ExpectedValue { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal FinalBalance { get; set; }
    public List<decimal> EquityCurve { get; set; } = new();
}
