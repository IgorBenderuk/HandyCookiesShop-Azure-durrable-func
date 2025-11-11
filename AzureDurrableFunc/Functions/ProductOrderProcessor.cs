using durrableShop.models;
using durrableShop.models.RequestDto;
using InvoiceGenerator.Models;
using Mapster;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace durrableShop.Functions;

public class ProductOrderProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public ProductOrderProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [Function(nameof(ProductOrderProcessor))]
    public async Task<string?> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ProductOrderProcessor));
        var order = context.GetInput<Order>();
        try
        {
            if (order == null)
            {
                return "Error: No order provided";
            }

            var results = new List<string>();
            logger.LogInformation("Step 1: Validating order #{OrderId}", order.Id);

            var validationResult = await context.CallActivityAsync<OperationResult<bool>>(nameof(OrderValidation.ValidateOrder), order);

            if (!validationResult.IsSuccess)
            {
                results.Add("Step 1: Order validation failed");
                return string.Join("\n", results);
            }

            results.Add("Step 1: Order validated successfully");
            logger.LogInformation("Step 2: Reserving stock");

            bool stockReserved = await context.CallActivityAsync<bool>(nameof(StockReservation.ReserveStock),order);

            if (!stockReserved)
            {
                results.Add("Step 2: Failed to reserve stock");
                return string.Join("\n", results);
            }
            results.Add("Step 2: Stock reserved");

            logger.LogInformation("Step 3: Purchave paymend and shipping validation");

            var purchaseCost = await context.CallSubOrchestratorAsync<OperationResult<decimal>>(nameof(FinanceFunctions.ValidatePaymentInfoSubOrchestrator), order);
            if (!purchaseCost.IsSuccess)
            {
                results.Add($"Step 3: {purchaseCost.Message}");
                return string.Join("\n", results);
            }
            results.Add($"Step 3 {purchaseCost.Message}");
            logger.LogInformation("Step 4: Purchave paymend confirmation");

            logger.LogInformation("Step 5: Purchave confirmation");

            var confirmed = await context.CallSubOrchestratorAsync<bool>(nameof(OrderConfirmation.OrderConfirmationOrchestrator), order);


            return string.Join("\n", results);
        }
        catch (Exception ex)
        {
            logger.LogError("Error ocured while processing order processing",ex.Message);
            var revertResult = await context.CallActivityAsync<OperationResult<bool>>(nameof(StockReservation.RevertStockReservation), order);
            logger.LogInformation(revertResult.Message); 
            return null;
        }
    }

    [Function("StartProductOrder")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("Function1_HttpStart");

        Order? order;
        try
        {
            var request = await req.ReadFromJsonAsync<CreateCookieRequestDto>();
            var rnd = new Random();
            order = request.Adapt<Order>();
           
            if (order == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid order data");
                return badRequest;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                order.Items = order.Items
                    .GroupBy(item => item.ProductId)
                    .Select(group => new OrderItem
                    {
                        ProductId = group.Key,
                        Quantity = group.Sum(item => item.Quantity), 
                        Price = group.First().Price 
                    }).ToArray();
             
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();

                logger.LogInformation($"Order {order.Id} saved to database");
            }

            logger.LogInformation($"Received order: {order.Items.Count} items");
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ProductOrderProcessor),
                order);

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'");

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing order");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync($"Error parsing order: {ex.Message}");
            return badRequest;
        }
    }
}