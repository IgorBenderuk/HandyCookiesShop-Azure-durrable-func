using handyCookiesShop.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace handyCookiesShop.Functions;

public class StockReservation
{
    private readonly IServiceProvider _serviceProvider;
    public StockReservation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Function(nameof(ReserveStock))]
    public async Task<bool> ReserveStock(
       [ActivityTrigger] Order order,
       FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(ReserveStock));
        logger.LogInformation($"Reserving stock for order #{order.Id}");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var item in order.Items)
            {
                var cookie = await dbContext.Product.FindAsync(item.ProductId);

                if (cookie == null || cookie.StockQuantity < item.Quantity)
                {
                    logger.LogWarning($"Cannot reserve stock for cookie {item.ProductId}");
                    return false;
                }

                cookie.StockQuantity -= item.Quantity;
                logger.LogInformation($"Reserved {item.Quantity} of {cookie.Name}. New stock: {cookie.StockQuantity}");
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Stock reserved successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reserving stock");
            return false;
        }
    }

    [Function(nameof(RevertStockReservation))]
    public async Task<bool> RevertStockReservation(
     [ActivityTrigger] Order order,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(RevertStockReservation));
        logger.LogInformation($"Reserving stock for order #{order.Id}");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var item in order.Items)
            {
                var product = await dbContext.Product.FindAsync(item.ProductId);

                if (product == null)
                {
                    logger.LogWarning($"Cannot reserve stock for cookie {item.ProductId}");
                    return false;
                }

                product.StockQuantity += item.Quantity;
                logger.LogInformation($"Reserved {item.Quantity} of {product.Name}. New stock: {product.StockQuantity}");
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Stock reserved successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reserving stock");
            return false;
        }
    }
}