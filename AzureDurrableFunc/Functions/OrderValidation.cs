using handyCookiesShop.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace handyCookiesShop.Functions;

public class OrderValidation
{
    private readonly IServiceProvider _serviceProvider;
    public OrderValidation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Function(nameof(ValidateOrder))]
    public async Task<bool> ValidateOrder(
     [ActivityTrigger] Order order,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ValidateOrder");
        logger.LogInformation($"Validating order #{order.Id} with {order.Items.Count} items");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var item in order.Items)
            {
                var product = await dbContext.Product.FindAsync(item.ProductId);

                if (product == null)
                {
                    logger.LogWarning($"Cookie with ID {item.ProductId} not found" );
                    return false;
                }
                if (product.StockQuantity < item.Quantity)
                {
                    logger.LogWarning($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                    return false;
                }

                logger.LogInformation($"{product.Name}: {item.Quantity} requested, {product.StockQuantity} available");
            }

            logger.LogInformation("Order validation successful");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating order");
            return false;
        }
    }
}