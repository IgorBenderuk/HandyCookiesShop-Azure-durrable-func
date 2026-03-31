📦 Project Overview

This project is an order processing system built with Azure Durable Functions, implementing a distributed workflow for handling e-commerce orders.

The system demonstrates:

Orchestration patterns
Sub-orchestrators
Activity-based execution
External event handling (human interaction)
Integration with EF Core and SMTP services
🧠 Architecture Overview

The system follows a Durable Functions orchestration pattern:

Main Orchestrator

ProductOrderProcessor

Pipeline:

Order Validation
Checks product existence
Validates stock availability
Stock Reservation
Deducts product quantities from DB
Payment & Shipping Validation (Sub-Orchestrator)
Parallel execution:
Payment method validation
Shipping cost calculation
Final step:
Balance validation
Order Confirmation (Human Interaction)
Sends email with confirmation link
Waits for external event (OrderConfirmation)
Timeout: 5 minutes
⚙️ Key Components
Activities
ValidateOrder → validates stock and product existence
ReserveStock → reserves inventory
RevertStockReservation → rollback on failure
CalculateShippingCost → calculates delivery cost
ValidateCustomerPaymentMethod → checks allowed payment methods
ValidateBalanceAndCalculateTotal → ensures sufficient funds
Sub-Orchestrators
ValidatePaymentInfoSubOrchestrator
Runs parallel tasks
Combines shipping + payment validation
OrderConfirmationOrchestrator
Sends email
Waits for user confirmation via HTTP endpoint
Implements timeout + external event pattern
🧪 How to Run & Test
1. Prerequisites
.NET SDK
Azure Functions Core Tools
Azurite
SQL Server
2. Start dependencies
azurite

3. Configure settings

Create local.settings.json:

{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "HandyCookiesConnection": "<your-db-connection>",
    "Email:SmtpHost": "smtp.gmail.com",
    "Email:SmtpPort": "587",
    "Email:Username": "<email>",
    "Email:Password": "<password>"
  }
}

4. Run the project
func start

5. Send request

POST

http://localhost:7071/api/StartProductOrder


Body:

{
  "customerId": 1,
  "shippingAdress": "Bern",
  "orderItems":[ 
    { "cookieId": 1, "quantity": 3 },
    { "cookieId": 1, "quantity": 1 },
    { "cookieId": 3, "quantity": 3 }
  ]
}

6. What happens next
Order is saved to DB
Orchestration starts
Email is sent with confirmation links
7. Confirm order

Open link from email:

/api/ConfirmOrder?instanceId=...&approved=true


OR manually:

http://localhost:7071/api/ConfirmOrder?instanceId=XXX&approved=true
