using Microsoft.AspNetCore.Builder;
using StockTradeBot.Models;

namespace StockTradeBot.Api.Interfaces;

public interface IApi
{
    public WebApplicationBuilder ConfigureApi(WebApplicationBuilder builder);
    public void MapRoutes(WebApplication app);

    public bool CheckForDuplicates(Trade trade);

}