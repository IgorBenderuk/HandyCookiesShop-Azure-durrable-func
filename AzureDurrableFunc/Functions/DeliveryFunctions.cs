using durrableShop.models;
using InvoiceGenerator.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static durrableShop.Functions.FinanceFunctions;

namespace durrableShop.Functions;

public class DeliveryFunctions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, int> _destinationDistanceDictionary;
    public DeliveryFunctions(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _destinationDistanceDictionary = new Dictionary<string, int>()
        {
            { "Zulih", 170 },
            { "Geneva", 260},
            { "Bern", 140 },
            { "luzerne", 210 }
        };
    }

    [Function(nameof(CalculateShippingCost))]
    public async Task<OperationResult<decimal>> CalculateShippingCost(
     [ActivityTrigger] Order order,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("CalculateShippingCost");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!_destinationDistanceDictionary.TryGetValue(order.DeliveryAddress, out var distance))
                return new(false, $"No delivery to {order.DeliveryAddress}");

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            var totalWeight = (decimal)order.Items.Sum(i => products[i.ProductId].Weight * i.Quantity);
            decimal cost = totalWeight * distance * 0.5m;

            return new(true, "Shipping calculated", cost);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating shipping");
            return new(false, ex.Message);
        }
    }
}