# Database Design

## Overview

WarehouseOps uses SQL Server with Entity Framework Core.

The database is created through EF Core migrations.

Local database name:

```text
WarehouseOpsDb
```

Local connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=WarehouseOpsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

## Main tables

The current database contains these main tables:

1. Products
2. InventoryItems
3. Customers
4. Orders
5. OrderItems
6. Shipments
7. Incidents
8. AuditLogs

## Shared fields

Most domain entities inherit from `BaseEntity`.

Common fields:

```text
Id
CreatedAt
UpdatedAt
```

`UpdatedAt` is nullable and is set when an entity is changed.

## Products

Represents a product in the warehouse catalog.

Important fields:

```text
Id
Name
Sku
Category
Description
Price
CreatedAt
UpdatedAt
```

Rules:

1. SKU must be unique in application logic.
2. Price cannot be negative.
3. Product is connected to inventory through InventoryItem.
4. Product is connected to orders through OrderItem.

## InventoryItems

Represents stock information for a product.

Important fields:

```text
Id
ProductId
QuantityInStock
MinimumStockLevel
CreatedAt
UpdatedAt
```

Relationship:

```text
InventoryItem -> Product
```

Rules:

1. Product must exist.
2. One product should only have one inventory item.
3. Quantity cannot be negative.
4. Minimum stock level cannot be negative.
5. Low stock is calculated when QuantityInStock is less than or equal to MinimumStockLevel.

## Customers

Represents a customer that can place orders.

Important fields:

```text
Id
Name
Email
PhoneNumber
Address
CreatedAt
UpdatedAt
```

Relationship:

```text
Customer -> Orders
```

Rules:

1. Email must be unique in application logic.
2. Email must contain @.
3. Name is required.

## Orders

Represents a customer order.

Important fields:

```text
Id
CustomerId
Status
TotalAmount
CreatedAt
UpdatedAt
```

Relationships:

```text
Order -> Customer
Order -> OrderItems
Order -> Shipment
```

Rules:

1. Customer must exist.
2. Order must contain at least one order item.
3. TotalAmount is calculated from order items.
4. Creating an order reduces inventory quantity.
5. Status changes are controlled in the application layer.

## OrderItems

Represents one product row in an order.

Important fields:

```text
Id
OrderId
ProductId
Quantity
UnitPrice
CreatedAt
UpdatedAt
```

Relationships:

```text
OrderItem -> Order
OrderItem -> Product
```

Rules:

1. Product must exist.
2. Quantity must be greater than zero.
3. UnitPrice is copied from Product.Price when the order is created.

## Shipments

Represents a shipment connected to an order.

Important fields:

```text
Id
OrderId
Status
TrackingNumber
ShippedDate
DeliveredDate
CreatedAt
UpdatedAt
```

Relationship:

```text
Shipment -> Order
```

Rules:

1. Order must exist.
2. Tracking number is required.
3. A shipment cannot be created for a cancelled order.
4. One order can only have one shipment in application logic.
5. ShippedDate is set when shipment status becomes Shipped.
6. DeliveredDate is set when shipment status becomes Delivered.

## Incidents

Represents an operational incident.

Important fields:

```text
Id
Title
Description
Status
ResolutionNotes
ClosedAt
CreatedAt
UpdatedAt
```

Rules:

1. Title is required.
2. Description is required.
3. New incidents start as Open.
4. Resolution notes are required when resolving an incident.
5. ClosedAt is set when the incident is closed.

## AuditLogs

Represents important changes in the system.

Important fields:

```text
Id
EntityName
Action
PerformedBy
PerformedAt
Changes
CreatedAt
UpdatedAt
```

Current implementation logs:

1. Product created
2. Product updated
3. Product deleted

Future implementation should also log:

1. Order created
2. Order status changed
3. Shipment created
4. Shipment status changed
5. Incident created
6. Incident closed
7. Admin actions

## Decimal precision

The database configuration sets decimal precision for money related fields:

```text
Product.Price
Order.TotalAmount
OrderItem.UnitPrice
```

Precision:

```text
18, 2
```

This avoids unsafe default decimal mapping in SQL Server.
