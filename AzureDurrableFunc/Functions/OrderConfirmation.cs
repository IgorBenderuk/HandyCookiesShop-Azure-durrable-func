using durrableShop.models;
using DurrableShop.models.RequestDto;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace durrableShop.Functions;

public class OrderConfirmation
{
    private readonly IMailingService _mailingService;
    private readonly AppDbContext _appDbContext;

    public OrderConfirmation(IMailingService mailingService,AppDbContext appDbContext)
    {
        _mailingService = mailingService;
        _appDbContext = appDbContext;
    }

    [Function(nameof(OrderConfirmationOrchestrator))]
    public async Task<OperationResult<bool>> OrderConfirmationOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(OrderConfirmationOrchestrator));
        var orderData = context.GetInput<Order>();
        await context.CallActivityAsync(nameof(SendConfirmation),
            new ConfirmPurchaseRequestDto() { Order =orderData , InstanceId = context.InstanceId });

        using (var cts = new CancellationTokenSource())
        {
            DateTime expiration = context.CurrentUtcDateTime.AddMinutes(5);
            var timeoutTask = context.CreateTimer(expiration, cts.Token);
            var confirmationTask = context.WaitForExternalEvent<bool>("OrderConfirmation");

            var winner = await Task.WhenAny(confirmationTask, timeoutTask);

            if (winner == confirmationTask)
            {
                cts.Cancel(); 
                bool confirmed = confirmationTask.Result;

                if (confirmed)
                {
                    logger.LogInformation("Order {OrderId} confirmed by customer", orderData.Id);
                    return new (true, "Order Confirmed and Processed");
                }
                else
                {
                    logger.LogInformation("Order {OrderId} rejected by customer", orderData.Id);
                    return new (false, "Order Cancelled by Customer");
                }
            }
            else
            {
                logger.LogWarning("Order {OrderId} confirmation timeout", orderData.Id);
                return new(false, "Order Cancelled - Timeout");
            }
        }
    }

    [Function(nameof(SendConfirmation))]
    public async Task SendConfirmation([ActivityTrigger] ConfirmPurchaseRequestDto input, FunctionContext executionContext)
    {
        try
        {
            var order = input.Order;

            var logger = executionContext.GetLogger(nameof(SendConfirmation));

            logger.LogInformation("Sending confirmation email for order {OrderId}", order.Id);
            string confirmUrl = $"http://localhost:7282/api/ConfirmOrder?instanceId={input.InstanceId}&orderId={order.Id}&approved=true";
            string rejectUrl = $"http://localhost:7282/api/ConfirmOrder?instanceId={input.InstanceId}&orderId={order.Id}&approved=false";

            string emailBody = $@"
             <h2>Order Confirmation Required</h2>
             <p>Dear {order.Customer.FirstName}{order.Customer.LastName},</p>
             <p>Please confirm your order #{order.Id}</p>
             <div style='margin: 20px 0;'>
                 <a href='{confirmUrl}' 
                    style='background-color: #4CAF50; color: white; padding: 15px 32px; 
                           text-decoration: none; display: inline-block; margin-right: 10px;'>
                      Confirm Order
                 </a>
                 <a href='{rejectUrl}' 
                    style='background-color: #f44336; color: white; padding: 15px 32px; 
                           text-decoration: none; display: inline-block;'>
                      Cancel Order
                 </a>
             </div>
             <p>This link will expire in 24 hours.</p>
             ";

            await _mailingService.SendInvoiceNotificationAsync(
                order.Customer.Email,
                $"Order_{order.Id}_Confirmation",
                emailBody
            );
        }catch(Exception ex)
        {
            throw(ex);
        }
     }

    [Function("ConfirmOrder")]
    public async Task<HttpResponseData> ConfirmOrder(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("ConfirmOrder");

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        string instanceId = query["instanceId"];
        string orderId = query["orderId"];
        bool approved = bool.Parse(query["approved"] ?? "false");

        logger.LogInformation("Received confirmation for order {OrderId}, approved: {Approved}",
            orderId, approved);

        await client.RaiseEventAsync(instanceId, "OrderConfirmation", approved);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");

        string message = approved
            ? "Your order has been confirmed! You will receive a confirmation email shortly."
            : "Your order has been cancelled.";

        await response.WriteStringAsync($@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial; text-align: center; padding: 50px; }}
                    .message {{ font-size: 24px; margin: 20px; }}
                </style>
            </head>
            <body>
                <div class='message'>{message}</div>
            </body>
            </html>
        ");

        return response;
    }
}