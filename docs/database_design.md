# Database Design

This document describes the database design for WarehouseOps.

WarehouseOps uses SQL Server with Entity Framework Core. The database supports a B2B technology warehouse scenario with products, inventory, customers, orders, shipments, incidents and audit logs.

## Database technology

The project uses:

* SQL Server
* Entity Framework Core
* EF Core migrations
* Repository pattern

The database context is located in:

backend/WarehouseOps.Infrastructure/ApplicationDbContext.cs

## Main tables

The main database tables are:

* Products
* InventoryItems
* Customers
* Orders
* OrderItems
* Shipments
* Incidents
* AuditLogs

## Products

The Products table stores product catalog information.

Example products in the demo seed data:

* ASUS ROG Zephyrus G14
* Apple MacBook Pro 14 M4
* Lenovo ThinkPad X1 Carbon
* Samsung Odyssey monitor
* Sony headphones
* Logitech accessories
* Ubiquiti networking hardware
* Synology storage devices

Main fields:

* Id
* Name
* Sku
* Category
* Description
* Price
* CreatedAt
* UpdatedAt

Business rules:

* Product name is required.
* SKU is required.
* Category is required.
* Price cannot be negative.
* SKU should be unique.
* Products used in inventory or order history cannot be deleted.

Relationships:

* One product can have one inventory item.
* One product can appear in many order items.

## InventoryItems

The InventoryItems table stores warehouse stock levels.

Main fields:

* Id
* ProductId
* QuantityInStock
* MinimumStockLevel
* CreatedAt
* UpdatedAt

Business rules:

* ProductId is required.
* QuantityInStock cannot be negative.
* MinimumStockLevel cannot be negative.
* A product can only have one inventory item.
* Low stock is detected when quantity is at or below the minimum stock level.

Relationships:

* Each inventory item belongs to one product.

## Customers

The Customers table stores B2B customer information.

Example customers in the demo seed data:

* Elgiganten Sverige AB
* Power Sverige AB
* Inet AB
* Dustin Sverige AB

Main fields:

* Id
* Name
* Email
* PhoneNumber
* Address
* CreatedAt
* UpdatedAt

Business rules:

* Customer name is required.
* Customer email is required.
* Customers can be connected to orders.

Relationships:

* One customer can have many orders.

## Orders

The Orders table stores customer orders.

Main fields:

* Id
* CustomerId
* Status
* TotalAmount
* CreatedAt
* UpdatedAt

Order status values:

* Pending
* Processing
* Packed
* Shipped
* Cancelled
* Completed

Business rules:

* CustomerId is required.
* An order must contain at least one order item.
* Order status changes must follow valid business rules.
* Shipped and completed orders cannot be cancelled.
* Cancelling an order returns items to inventory.

Relationships:

* Each order belongs to one customer.
* Each order has many order items.
* Each order can have one shipment.

## OrderItems

The OrderItems table stores products included in an order.

Main fields:

* Id
* OrderId
* ProductId
* Quantity
* UnitPrice
* CreatedAt
* UpdatedAt

Business rules:

* OrderId is required.
* ProductId is required.
* Quantity must be greater than zero.
* Product must exist in inventory before it can be ordered.
* The same product cannot be added more than once to the same order.
* Order quantity cannot exceed available stock.

Relationships:

* Each order item belongs to one order.
* Each order item belongs to one product.

## Shipments

The Shipments table stores delivery information for orders.

Main fields:

* Id
* OrderId
* Status
* TrackingNumber
* ShippedDate
* DeliveredDate
* CreatedAt
* UpdatedAt

Shipment status values:

* Pending
* Packed
* Shipped
* Delivered
* Delayed
* Cancelled

Business rules:

* OrderId is required.
* TrackingNumber is required.
* Cancelled orders cannot receive shipments.
* An order can only have one shipment.
* Delivered and cancelled shipments cannot be changed.
* DeliveredDate is set when shipment status becomes Delivered.

Relationships:

* Each shipment belongs to one order.

## Incidents

The Incidents table stores operational problems and follow up work.

Main fields:

* Id
* Title
* Description
* Severity
* RelatedEntityType
* RelatedEntityId
* Status
* ResolutionNotes
* ClosedAt
* CreatedAt
* UpdatedAt

Incident severity values:

* Low
* Medium
* High
* Critical

Incident status values:

* Open
* InProgress
* Resolved
* Closed

Related entity type values:

* General
* Product
* Inventory
* Customer
* Order
* Shipment

Business rules:

* Title is required.
* Description is required.
* Severity must be valid.
* Related entity type must be valid.
* Closed incidents cannot be reopened.
* Resolution notes are required when closing an incident.

Relationships:

* Incidents can reference different business areas through RelatedEntityType and RelatedEntityId.
* This is intentionally flexible because incidents can relate to products, inventory, customers, orders or shipments.

## AuditLogs

The AuditLogs table stores change history for important business actions.

Main fields:

* Id
* EntityName
* Action
* PerformedBy
* PerformedAt
* Changes
* CreatedAt
* UpdatedAt

Audit logs are created for actions such as:

* Product created
* Product updated
* Product deleted
* Inventory created
* Inventory updated
* Customer created
* Customer updated
* Order created
* Order status updated
* Order cancelled
* Shipment created
* Shipment status updated
* Incident created
* Incident status updated
* Incident closed

Business rules:

* Important changes should create audit log entries.
* Audit logs should show who performed the action.
* Audit log access is limited to Admin and Manager.

## Relationship summary

Product relationships:

* Product to InventoryItem: one to one
* Product to OrderItem: one to many

Customer relationships:

* Customer to Order: one to many

Order relationships:

* Order to OrderItem: one to many
* Order to Shipment: one to one

Shipment relationships:

* Shipment to Order: many to one from shipment perspective

Incident relationships:

* Incident uses flexible reference fields instead of direct foreign keys.

AuditLog relationships:

* AuditLog stores historical change information and is not dependent on a direct foreign key to one specific entity type.

## Demo seed data

The Docker setup includes demo seed data.

Seed data is located in:

backend/WarehouseOps.Infrastructure/DatabaseSeeder.cs

The seed data creates:

* Products
* Inventory items
* Customers
* Orders
* Order items
* Shipments
* Incidents
* Audit logs

Seed data is enabled in Docker Compose with:

Database__SeedDemoData=true

Seed data only runs when the database has no products.

## Docker database

Docker Compose runs SQL Server in a container.

The SQL Server service is named:

sqlserver

The backend connects to SQL Server through the internal Docker network.

SQL Server is not exposed to the host machine.

This means the database is reachable by the backend container, but not opened directly on localhost from Windows.

## Local database

When running locally without Docker, the project can use SQL Server LocalDB.

The connection string is stored in appsettings.json for local development.

The JWT secret is not stored in appsettings.json. It is stored through user secrets locally or environment variables in Docker.

## EF Core migrations

Entity Framework Core migrations are used to create and update the database schema.

The migrations are located in the Infrastructure project.

To update the local database manually:

cd backend
dotnet ef database update --project WarehouseOps.Infrastructure --startup-project WarehouseOps.Api

In Docker, migrations can be applied automatically when this setting is enabled:

Database__AutoMigrate=true

## Current database improvement ideas

The current database design works for the portfolio version.

Possible future improvements:

* Add unique database index for Product.Sku.
* Add unique database index for Customer.Email.
* Add unique database index for InventoryItem.ProductId.
* Add unique database index for Shipment.OrderId.
* Add stronger length constraints for important text fields.
* Add integration tests for database behavior.
* Add more realistic audit log metadata.
* Add supplier and purchasing tables.
* Add returns handling.
* Add multi warehouse support.

## Summary

The database design supports the main WarehouseOps workflow:

* Products are imported and stored in the product catalog.
* Inventory tracks stock levels for products.
* Customers place B2B orders.
* Orders reserve stock and contain order items.
* Shipments track deliveries.
* Incidents track operational problems.
* Audit logs track important business changes.

The design is intentionally clear and focused on warehouse operations rather than a full ERP system.