using Newtonsoft.Json.Linq;

namespace StockTradeBot.Models;

public class CandleStick
{
    public DateTime OpenTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }
    public DateTime CloseTime { get; set; }
    public decimal QuoteAssetVolume { get; set; }
    public int NumberOfTrades { get; set; }
    public decimal TakerBuyBaseAssetVolume { get; set; }
    public decimal TakerBuyQuoteAssetVolume { get; set; }

    private static decimal ParseDecimal(string value)
    {
      
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m; 
        }
        value = value.Trim();

        try
        {
            return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            Console.WriteLine($"Invalid decimal format: {value}");
            return 0m; 
        }
    }
    public static CandleStick FromJToken(JToken token)
    {
        return new CandleStick
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(token[0].Value<long>()).DateTime,
            OpenPrice = ParseDecimal(token[1].ToString()),
            HighPrice = ParseDecimal(token[2].ToString()),
            LowPrice = ParseDecimal(token[3].ToString()),
            ClosePrice = ParseDecimal(token[4].ToString()),
            Volume = ParseDecimal(token[5].ToString()),
            CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(token[6].Value<long>()).DateTime,
            QuoteAssetVolume = ParseDecimal(token[7].ToString()),
            NumberOfTrades = token[8].Value<int>(),
            TakerBuyBaseAssetVolume = ParseDecimal(token[9].ToString()),
            TakerBuyQuoteAssetVolume = ParseDecimal(token[10].ToString())
        };
        
    }
}