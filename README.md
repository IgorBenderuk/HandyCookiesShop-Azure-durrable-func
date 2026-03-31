# 🛒 Order Processing System (Azure Durable Functions)

## 📦 Overview

This project is a distributed order processing system built with Azure Durable Functions using the .NET isolated worker model.

It demonstrates how to implement a reliable, multi-step workflow for handling e-commerce orders, including:

* validation
* stock reservation
* payment processing
* shipping calculation
* user confirmation

The system follows orchestration and saga-like patterns to ensure consistency across multiple steps.

---

## 🧠 Architecture

The application is based on a Durable Functions orchestration pattern.

### High-Level Flow

```
HTTP Request → Orchestrator → Activities → Sub-Orchestrators → External Event
```

---

## ⚙️ Workflow

### Entry Point

**Function:** `StartProductOrder` (HTTP Trigger)

* Accepts incoming order request
* Normalizes order items (groups duplicates)
* Saves order to database
* Starts orchestration

---

### Main Orchestrator

**Function:** `ProductOrderProcessor`

This is the core workflow controller.

#### Step 1 – Order Validation

* Ensures products exist
* Checks stock availability

#### Step 2 – Stock Reservation

* Deducts product quantities from database
* Prevents overselling

#### Step 3 – Payment & Shipping Validation (Sub-Orchestrator)

* Runs in parallel:

  * Payment method validation
  * Shipping cost calculation
* Then validates:

  * Customer balance
  * Total cost (order + shipping)

#### Step 4 – Order Confirmation (Sub-Orchestrator)

* Sends confirmation email
* Waits for user action (approve/reject)
* Uses external event pattern
* Timeout: 5 minutes

---

## 🔁 Sub-Orchestrators

### 1. Payment & Shipping

**Function:** `ValidatePaymentInfoSubOrchestrator`

* Executes parallel activities
* Aggregates results
* Performs final balance validation

---

### 2. Order Confirmation

**Function:** `OrderConfirmationOrchestrator`

* Sends email with confirmation links
* Waits for external event:

  ```
  OrderConfirmation
  ```
* Handles:

  * approval
  * rejection
  * timeout

---

## 🔧 Activities

* `ValidateOrder` → validates product existence and stock
* `ReserveStock` → reduces inventory
* `RevertStockReservation` → rollback on failure
* `CalculateShippingCost` → calculates delivery price
* `ValidateCustomerPaymentMethod` → checks allowed payment methods
* `ValidateBalanceAndCalculateTotal` → verifies available balance
* `SendConfirmation` → sends email with confirmation links

---

## 🗄️ Data Layer

Uses Entity Framework Core with SQL Server.

### Entities:

* Customer
* Product
* Order
* OrderItem

### Features:

* Seeded data for testing
* Enum conversion for payment methods
* Relationship configuration with cascading rules

---

## 🧪 How to Run

### 1. Prerequisites

* .NET SDK
* Azure Functions Core Tools
* Azurite (local storage emulator)
* SQL Server

---

### 2. Start Azurite

```bash
azurite
```

---

### 3. Configure settings

Create `local.settings.json`:

```json
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
```

---

### 4. Run the project

```bash
func start
```

---

## 📩 How to Test

### Endpoint

```
POST http://localhost:7071/api/StartProductOrder
```

---

### Example Request

```json
{
  "customerId": 1,
  "shippingAdress": "Bern",
  "orderItems":[ 
    { "cookieId": 1, "quantity": 3 },
    { "cookieId": 1, "quantity": 1 },
    { "cookieId": 3, "quantity": 3 }
  ]
}
```

---

### What happens after request

1. Order is saved to database
2. Orchestration starts
3. System executes validation and reservation steps
4. Email is sent to customer
5. System waits for confirmation

---

## ✅ Order Confirmation

User receives email with two links:

* Approve order
* Reject order

These links call:

```
/api/ConfirmOrder?instanceId=...&approved=true|false
```

This triggers the orchestration to continue.

---

## ⚠️ Failure Handling

* If stock reservation fails → process stops
* If payment validation fails → process stops
* If exception occurs → stock rollback is triggered
* If user does not confirm → order is cancelled (timeout)

---

## 💡 Important Notes

* Workflow is stateful (Durable Functions)
* Uses parallel execution (`Task.WhenAll`)
* Implements compensation logic (rollback)
* Uses external events for human interaction

---

## ⚠️ Limitations

* Hardcoded delivery distances and balances
* No retry policies for activities
* Orchestrator returns string instead of structured result
* Email sending depends on SMTP configuration

---

## 🚀 Possible Improvements

* Move configuration to database or external config
* Add retry and error policies
* Introduce DTOs for orchestration responses
* Replace hardcoded data with persistent storage
* Add logging/monitoring dashboard

---

## 🎯 Key Concepts Demonstrated

* Durable Functions orchestration
* Sub-orchestrators
* Parallel execution
* Saga / compensation pattern
* Human-in-the-loop workflows
* Integration with database and external services

---

## 🔒 Security Note

Do NOT commit sensitive data such as:

* SMTP credentials
* database connection strings

Use environment variables or a template configuration file instead.

---

## 🧩 Summary

This project demonstrates how to build a reliable, multi-step order processing system using Durable Functions.

It models real-world business workflows with:

* validation
* state management
* failure recovery
* asynchronous processing

and provides a solid foundation for distributed system design in cloud environments.
