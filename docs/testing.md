# Testing

This document describes the testing approach for WarehouseOps.

WarehouseOps uses backend service tests to verify important business rules in the Application layer.

The goal of the tests is to show that core warehouse operations behave correctly and that invalid business actions are rejected.

## Test tools

The backend test project uses:

* xUnit
* Moq
* FluentAssertions

## Test project

The test project is located at:

backend/WarehouseOps.Tests

Current test files:

* ProductServiceTests.cs
* InventoryServiceTests.cs
* OrderServiceTests.cs
* ShipmentServiceTests.cs
* IncidentServiceTests.cs

## Running tests

Run all backend tests from the backend folder:

cd backend
dotnet test

The current backend test suite contains 50 service tests.

## Test focus

The tests focus on the Application layer.

This is intentional because the Application layer contains the main business rules.

The tests mock repository interfaces with Moq. This makes the tests fast and focused on business behavior instead of database setup.

## ProductService tests

Product tests verify product management rules.

Covered behavior:

* Creating a product with valid data works.
* Product input is trimmed.
* Missing product name is rejected.
* Duplicate SKU is rejected.
* Deleting a missing product returns false.
* Deleting a product used in inventory or order history is rejected.
* Deleting a product that is not in use works.
* Product changes are audit logged.

Important business rules:

* Product name is required.
* SKU is required.
* Category is required.
* Price cannot be negative.
* SKU must be unique.
* Products used in inventory or order history cannot be deleted.

## InventoryService tests

Inventory tests verify warehouse stock rules.

Covered behavior:

* Creating an inventory item with valid data works.
* Missing product id is rejected.
* Negative quantity is rejected.
* Missing product is rejected.
* Duplicate inventory item for the same product is rejected.
* Updating inventory with valid data works.
* Updating inventory with negative quantity is rejected.
* Updating a missing inventory item returns null.
* Low stock items are mapped correctly.
* Inventory changes are audit logged.

Important business rules:

* Inventory must be connected to a product.
* Quantity in stock cannot be negative.
* Minimum stock level cannot be negative.
* A product can only have one inventory item.
* Low stock is detected when stock is below minimum level.

## OrderService tests

Order tests verify B2B order handling rules.

Covered behavior:

* Creating an order with valid customer, products and inventory works.
* Creating an order reduces inventory quantity.
* Missing customer id is rejected.
* Missing customer is rejected.
* Adding the same product more than once is rejected.
* Ordering a product without inventory is rejected.
* Ordering more than available stock is rejected.
* Cancelling an order returns items to inventory.
* Shipped orders cannot be cancelled.
* Invalid status changes are rejected.
* Order changes are audit logged.

Important business rules:

* Orders must have a valid customer.
* Orders must contain at least one item.
* The same product cannot be added more than once to the same order.
* Products must exist in inventory before they can be ordered.
* Orders cannot reserve more stock than available.
* Cancelling an order returns items to inventory.
* Shipped and completed orders cannot be cancelled.

## ShipmentService tests

Shipment tests verify delivery handling rules.

Covered behavior:

* Creating a shipment for a valid order works.
* Missing order id is rejected.
* Missing tracking number is rejected.
* Missing order is rejected.
* Creating a shipment for a cancelled order is rejected.
* Creating a second shipment for the same order is rejected.
* Updating shipment status works.
* Delivered date is set when status becomes Delivered.
* Updating a missing shipment returns null.
* Invalid shipment status is rejected.
* Delivered shipments cannot be changed.
* Shipment changes are audit logged.

Important business rules:

* A shipment must be connected to an order.
* Cancelled orders cannot receive shipments.
* An order can only have one shipment.
* Delivered and cancelled shipments cannot be changed.

## IncidentService tests

Incident tests verify operational incident rules.

Covered behavior:

* Creating an incident with valid data works.
* Missing title is rejected.
* Missing description is rejected.
* Invalid severity is rejected.
* Invalid related entity type is rejected.
* Updating incident status works.
* Closed date is set when status becomes Closed.
* Updating a missing incident returns null.
* Invalid incident status is rejected.
* Closed incidents cannot be reopened.
* Resolving an incident with resolution notes works.
* Resolving without notes is rejected.
* Resolving a missing incident returns null.
* Resolving an already closed incident is rejected.
* Filtering incidents by status works.
* Incident changes are audit logged.

Important business rules:

* Title is required.
* Description is required.
* Severity must be valid.
* Related entity type must be valid.
* Closed incidents cannot be reopened.
* Resolution notes are required when closing an incident.

## What is not tested yet

The current test suite is focused on service level business rules.

The following areas can be improved later:

* Controller integration tests
* Authorization integration tests
* Database integration tests with SQL Server or Testcontainers
* Frontend component tests
* End to end tests
* Docker build validation in CI

## Why service tests were chosen first

Service tests were chosen first because they give the highest value for the current project stage.

They verify the rules that matter most:

* Products cannot be deleted when they are in use.
* Inventory cannot become negative.
* Orders cannot reserve unavailable stock.
* Cancelling orders returns stock.
* Shipments follow valid business rules.
* Incidents cannot be closed incorrectly.

These tests are fast, readable and easy to run in GitHub Actions.

## GitHub Actions

GitHub Actions runs tests automatically.

The CI workflow runs on push and pull request to main.

The workflow checks:

* Backend restore
* Backend build
* Backend tests
* Frontend dependency installation
* Frontend build

Workflow file:

.github/workflows/ci.yml

## Test result goal

The expected result is:

failed: 0

All tests should pass before code is committed or pushed.

## Current status

| Test area | Status |
|---|---|
| ProductService | Implemented |
| InventoryService | Implemented |
| OrderService | Implemented |
| ShipmentService | Implemented |
| IncidentService | Implemented |
| Controller tests | Planned improvement |
| Authorization integration tests | Planned improvement |
| Frontend tests | Planned improvement |
| End to end tests | Planned improvement |