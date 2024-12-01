namespace StockTradeBot;
using StockTradeBot.Models;

public interface IStrategy
{
    public List<CandleStick> LoadData();
}