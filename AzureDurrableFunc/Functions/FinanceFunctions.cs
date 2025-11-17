using durrableShop.models;
using DurrableShop.models.RequestDto;
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
            { "some John number", 250 },
            { "some Alice number", 180 }
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
     [ActivityTrigger] ValidateBalanceRequestDto input,
     FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ValidateBalance");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var order = await dbContext.Orders.FindAsync(input.OrderEntry.Id);
            var customer = await dbContext.Customers.FindAsync(input.OrderEntry.CustomerId);

            if (order == null)
                return new(false, $"Order {input.OrderEntry.Id} not found");

            if (customer == null)
                return new(false, $"Customer {input.OrderEntry.CustomerId} not found");

            if (!_bankAccountNumberAmountDictionary.TryGetValue(customer.BankAccount, out decimal balance))
                return new(false, $"Bank account for {customer.Id} not found");

            decimal totalCost = order.TotalAmount + input.ShippingCost;

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
            var shippingConst = shippingTask.Result;

            if (!payment.IsSuccess)
                return new(false, payment.Message);

            if (!shippingConst.IsSuccess)
                return new(false, shippingConst.Message);

            var totalCost = await context.CallActivityAsync<OperationResult<decimal>>(
                nameof(ValidateBalanceAndCalculateTotal),
                new ValidateBalanceRequestDto() {  OrderEntry=order, ShippingCost = shippingConst.Value }
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