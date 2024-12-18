using System.Diagnostics;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using StockTradeBot.Models;
namespace StockTradeBot;

public class Strategy : IStrategy
{
    private readonly HttpClient _client;

    public Strategy(HttpClient client)
    {
        _client = client;
        client.BaseAddress = new Uri("http://localhost:5105/");
    }

    public Strategy() : this(new HttpClient())
    {
    }

    public List<CandleStick> LoadData()
    {
        var timeframe = "1h";
        int limit = 700000;
        string url = $"https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval={timeframe}&limit={limit}";
        var response = _client.GetStringAsync(url).GetAwaiter().GetResult();
        var data = JArray.Parse(response);
        var candles = data.Select(CandleStick.FromJToken).ToList();
        return candles;
    }

    public List<CandleStick> CalculatePosition(bool isLong, List<CandleStick> data)
    {
        var entries = new List<CandleStick>();
        entries = CalculatePositionBasedOnConditions(data, isLong);
        return entries;
    }

    private List<CandleStick> CalculatePositionBasedOnConditions(List<CandleStick> data, bool isLong)
    {
        var entries = new List<CandleStick>();
        
        var volumeMA = CalculateVolumeMA(data, 20);  
        var priceMA = CalculatePriceMA(data, 50);   

        for (int i = 3; i < data.Count; i++) 
        {
            CandleStick current = data[i];
            CandleStick position_1 = data[i - 1];
            CandleStick position_2 = data[i - 2];

            
            bool isUptrend = current.ClosePrice > priceMA[i];
            bool higherVolume = current.Volume > volumeMA[i];

            if (isLong)
            {
               
                bool c1 = current.HighPrice > current.ClosePrice;
                bool c2 = current.ClosePrice > position_2.HighPrice;
                bool c3 = position_2.HighPrice > position_1.HighPrice;
                bool c4 = position_1.HighPrice > current.LowPrice;
                bool c5 = current.LowPrice > position_2.LowPrice;
                bool c6 = position_2.LowPrice > position_1.LowPrice;

              
                bool strongCandle = (current.ClosePrice - current.LowPrice) > (current.HighPrice - current.ClosePrice);
                bool minBodySize = (current.ClosePrice - current.OpenPrice).Abs() > current.OpenPrice * 0.003m;

                if (c1 && c2 && c3 && c4 && c5 && c6 && isUptrend /*&& higherVolume && strongCandle && minBodySize */)
                {
                    entries.Add(current);
                    
                    Trade trade = new Trade();
                    //trade.Guid = Guid.NewGuid();
                    trade.EntryPrice = current.HighPrice;
                    trade.StopLoss = current.HighPrice *0.98m;
                    trade.TakeProfit = current.HighPrice * 1.02m;
                    trade.EntryTime = current.OpenTime;
                    trade.IsWin = false;
                    
                    
                    //Todo: upload do api
                   var response =  _client.PostAsJsonAsync("/trades/add", trade).GetAwaiter().GetResult();
                   if (response.IsSuccessStatusCode)
                   {
                       Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                       Console.WriteLine("Dodano trade do api");
                   }
                }
            }
            else
            {
                
                bool c1 = current.LowPrice < current.OpenPrice;
                bool c2 = current.OpenPrice < position_2.LowPrice;
                bool c3 = position_2.LowPrice < position_1.LowPrice;
                bool c4 = position_1.LowPrice < current.HighPrice;
                bool c5 = current.HighPrice < position_2.HighPrice;
                bool c6 = position_2.HighPrice < position_1.HighPrice;

                
                bool strongCandle = (current.HighPrice - current.ClosePrice) > (current.ClosePrice - current.LowPrice);
                bool minBodySize = (current.OpenPrice - current.ClosePrice).Abs() > current.OpenPrice * 0.003m;

                if (c1 && c2 && c3 && c4 && c5 && c6 && !isUptrend && higherVolume && strongCandle && minBodySize)
                {
                    entries.Add(current);
                }
            }
        }

        return entries;
    }

    private List<decimal> CalculateVolumeMA(List<CandleStick> data, int period)
    {
        var result = new List<decimal>();
        for (int i = 0; i < data.Count; i++)
        {
            if (i < period)
            {
                result.Add(0);
                continue;
            }

            decimal sum = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                sum += data[j].Volume;
            }
            result.Add(sum / period);
        }
        return result;
    }

    private List<decimal> CalculatePriceMA(List<CandleStick> data, int period)
    {
        var result = new List<decimal>();
        for (int i = 0; i < data.Count; i++)
        {
            if (i < period)
            {
                result.Add(0);
                continue;
            }

            decimal sum = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                sum += data[j].ClosePrice;
            }
            result.Add(sum / period);
        }
        return result;
    }
}

public static class DecimalExtesions
{
    public static decimal Abs(this decimal value)
    {
        return Math.Abs(value);
    }
}