using durrableShop.models;
using InvoiceGenerator.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace durrableShop.Functions;

public class OrderValidation
{
    private readonly IServiceProvider _serviceProvider;
    public OrderValidation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Function(nameof(ValidateOrder))]
    public async Task<OperationResult<bool>> ValidateOrder(
     [ActivityTrigger] Order order,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ValidateOrder");
        logger.LogInformation($"Validating order #{order.Id} with {order.Items.Count} items");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var products = await dbContext.Products.ToListAsync();
            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(x=>x.Id == item.ProductId);

                if (product == null)
                {
                    logger.LogWarning($"Cookie with ID {item.ProductId} not found");
                    return new (false, $"Cookie with ID {item.ProductId} not found");
                }
                if (product.StockQuantity < item.Quantity)
                {
                    logger.LogWarning($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                    return new(false, $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }

                logger.LogInformation($"{product.Name}: {item.Quantity} requested, {product.StockQuantity} available");
            }

            logger.LogInformation("Order validation successful");
            return new (true,"",true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating order");
            return new (false, "Error validating order");
        }
    }
}