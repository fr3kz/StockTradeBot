namespace StockTradeBot.Models;
public class Trade
{
    
    public Guid Guid { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitPercentage { get; set; }
    public bool IsWin { get; set; }
    public decimal Size { get; set; }
    public decimal PositionValue => Size * EntryPrice;
    public decimal RiskAmount { get; set; }
    public decimal RewardAmount { get; set; }
    public decimal Leverage { get; set; }
    public decimal StopLossPercentage { get; set; }
    public decimal TakeProfitPercentage { get; set; }
}
