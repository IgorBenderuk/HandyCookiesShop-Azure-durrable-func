using handyCookiesShop;
using handyCookiesShop.models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Net;

namespace handyCookiesShop.Functions;

public class HandyCookieOrderProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public HandyCookieOrderProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    [Function(nameof(HandyCookieOrderProcessor))]
    public async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(HandyCookieOrderProcessor));

        var order = context.GetInput<Order>();

        if (order == null)
        {
            return "Error: No order provided";
        }

        var results = new List<string>();
        logger.LogInformation("Step 1: Validating order #{OrderId}", order.Id);

        bool isValid = await context.CallActivityAsync<bool>(nameof(OrderValidation.ValidateOrder), order);

        if (!isValid)
        {
            results.Add("Step 1: Order validation failed");
            return string.Join("\n", results);
        }
        results.Add("Step 1: Order validated successfully");
        logger.LogInformation("Step 2: Reserving stock");

        bool stockReserved = await context.CallActivityAsync<bool>(
            nameof(StockReservation.ReserveStock),
            order);

        if (!stockReserved)
        {
            results.Add("Step 2: Failed to reserve stock");
            return string.Join("\n", results);
        }
        results.Add("Step 2: Stock reserved");
        return "some";
    }

    [Function("StartCookieOrder")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("Function1_HttpStart");

        Order? order;
        try
        {
            order = await req.ReadFromJsonAsync<Order>();
            if (order == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid order data");
                return badRequest;
            }
            logger.LogInformation($"Received order: {order.Items.Count} items");
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(HandyCookieOrderProcessor),
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

    [Function(nameof(CheckAvailability))]
    public  string CheckAvailability([ActivityTrigger] string name, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SayHello");
        logger.LogInformation("Saying hello to {name}.", name);
        return $"Hello {name}!";
    }

    [Function(nameof(ConfirmOrder))]
    public  string ConfirmOrder([ActivityTrigger] string name, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SayHello");
        logger.LogInformation("Saying hello to {name}.", name);
        return $"Hello {name}!";
    }

    [Function(nameof(SendConfirmation))]
    public  string SendConfirmation([ActivityTrigger] string name, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("SayHello");
        logger.LogInformation("Saying hello to {name}.", name);
        return $"Hello {name}!";
    }
    
}