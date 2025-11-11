using durrableShop.models;
using InvoiceGenerator.Models;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace durrableShop.Functions;

public class FinanceFunctions
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, decimal> _bankAccountNumberAmountDictionary;
    public FinanceFunctions(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _bankAccountNumberAmountDictionary = new Dictionary<string, decimal>()
        {
            { "some John number", 150 },
            { "some Alice number", 50 }
        };
    }
  


    [Function(nameof(ValidateCustomerPaymentMethod))]
    public async Task<OperationResult<bool>> ValidateCustomerPaymentMethod(
      [ActivityTrigger] Order orderEntry,
      FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ValidateCustomer");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var customer = await dbContext.Customers.FindAsync(orderEntry.CustomerId);
            if (customer == null)
                return new(false, $"Customer {orderEntry.CustomerId} not found");

            var order = await dbContext.Orders.FindAsync(orderEntry.Id);
            if (order == null)
                return new(false, $"Order {orderEntry.Id} not found");

            var requiredMethods = order.Items.Select(x => x.Product.PaymentMethod).Distinct().ToList();
            var missing = requiredMethods.Except(customer.PaymentMethods).ToList();

            if (missing.Any())
                return new(false, $"Missing payment methods: {string.Join(", ", missing)}");

            logger.LogInformation("Payment methods validated successfully");
            return new(true, "Payment methods validated", true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating payment method");
            return new(false, ex.Message);
        }
    }


    [Function(nameof(ValidateBalanceAndCalculateTotal))]
    public async Task<OperationResult<decimal>> ValidateBalanceAndCalculateTotal(
     [ActivityTrigger] (Order orderEntry, decimal shippingCost) input,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ValidateBalance");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var order = await dbContext.Orders.FindAsync(input.orderEntry.Id);
            var customer = await dbContext.Customers.FindAsync(input.orderEntry.CustomerId);

            if (order == null)
                return new(false, $"Order {input.orderEntry.Id} not found");

            if (customer == null)
                return new(false, $"Customer {input.orderEntry.CustomerId} not found");

            if (!_bankAccountNumberAmountDictionary.TryGetValue(customer.BankAccount, out decimal balance))
                return new(false, $"Bank account for {customer.Id} not found");

            decimal totalCost = order.TotalAmount + input.shippingCost;

            if (totalCost > balance)
                return new(false, $"Insufficient funds. Need {totalCost}, available {balance}");

            return new(true, "Balance validation successfully. Total cost :", totalCost);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating balance");
            return new(false, ex.Message);
        }
    }


    [Function(nameof(ValidatePaymentInfoSubOrchestrator))]
    public async Task<OperationResult<decimal>> ValidatePaymentInfoSubOrchestrator(
      [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger("ValidatePaymentInfoSubOrchestrator");
        var order = context.GetInput<Order>();

        try
        {
            var paymentTask = context.CallActivityAsync<OperationResult<bool>>(nameof(ValidateCustomerPaymentMethod), order);
            var shippingTask = context.CallActivityAsync<OperationResult<decimal>>(nameof(DeliveryFunctions.CalculateShippingCost), order);

            await Task.WhenAll(paymentTask, shippingTask);

            var payment = paymentTask.Result;
            var shipping = shippingTask.Result;

            if (!payment.IsSuccess)
                return new(false, payment.Message);

            if (!shipping.IsSuccess)
                return new(false, shipping.Message);

            var totalCost = await context.CallActivityAsync<OperationResult<decimal>>(
                nameof(ValidateBalanceAndCalculateTotal),
                (order, shipping.Value)
            );

            return totalCost;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating payment info");
            return new(false, ex.Message);
        }
    }
}