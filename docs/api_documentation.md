# API Documentation

## Overview

WarehouseOps exposes a REST API built with ASP.NET Core Web API.

The API currently supports:

1. Products
2. Inventory
3. Customers
4. Orders
5. Shipments
6. Incidents
7. Audit logs

Swagger is used for local API testing.

Local Swagger URL:

```text
http://localhost:5059/swagger
```

## Products

Base route:

```text
/api/Products
```

### GET /api/Products

Returns all products.

Optional query parameters:

```text
search
category
```

Example:

```text
GET /api/Products?search=Dell&category=Electronics
```

### GET /api/Products/{id}

Returns one product by id.

### POST /api/Products

Creates a new product.

Example request:

```json
{
  "name": "Barcode Scanner",
  "sku": "SCAN-001",
  "category": "Equipment",
  "description": "Wireless barcode scanner for warehouse staff.",
  "price": 1299
}
```

Important rules:

1. Name is required.
2. SKU is required.
3. Category is required.
4. Price cannot be negative.
5. SKU must be unique.

Product creation is written to the audit log.

### PUT /api/Products/{id}

Updates an existing product.

Important rules:

1. Product must exist.
2. Name is required.
3. SKU is required.
4. Category is required.
5. Price cannot be negative.
6. SKU must be unique.

Product update is written to the audit log.

### DELETE /api/Products/{id}

Deletes an existing product.

Product deletion is written to the audit log.

## Inventory

Base route:

```text
/api/Inventory
```

### GET /api/Inventory

Returns all inventory items with product information.

### GET /api/Inventory/low-stock

Returns inventory items where the current quantity is less than or equal to the minimum stock level.

### GET /api/Inventory/{id}

Returns one inventory item by id.

### POST /api/Inventory

Creates an inventory item for a product.

Example request:

```json
{
  "productId": "c4e4ec76-55a2-41f6-a338-8aabd1822d80",
  "quantityInStock": 20,
  "minimumStockLevel": 5
}
```

Important rules:

1. Product id is required.
2. Product must exist.
3. Quantity cannot be negative.
4. Minimum stock level cannot be negative.
5. A product can only have one inventory item.

### PUT /api/Inventory/{id}

Updates inventory quantity and minimum stock level.

Example request:

```json
{
  "quantityInStock": 25,
  "minimumStockLevel": 10
}
```

Important rules:

1. Inventory item must exist.
2. Quantity cannot be negative.
3. Minimum stock level cannot be negative.

## Customers

Base route:

```text
/api/Customers
```

### GET /api/Customers

Returns all customers.

Optional query parameter:

```text
search
```

### GET /api/Customers/{id}

Returns one customer by id.

### POST /api/Customers

Creates a customer.

Example request:

```json
{
  "name": "Nordic Warehouse AB",
  "email": "contact@nordicwarehouse.se",
  "phoneNumber": "031123456",
  "address": "Göteborg"
}
```

Important rules:

1. Name is required.
2. Email is required.
3. Email must contain @.
4. Email must be unique.

### PUT /api/Customers/{id}

Updates an existing customer.

Important rules:

1. Customer must exist.
2. Name is required.
3. Email is required.
4. Email must contain @.
5. Email must be unique.

## Orders

Base route:

```text
/api/Orders
```

### GET /api/Orders

Returns all orders with customer and order item information.

### GET /api/Orders/{id}

Returns one order by id.

### POST /api/Orders

Creates an order.

Example request:

```json
{
  "customerId": "b69f0291-5fb3-417b-9270-38ee15cf70d6",
  "items": [
    {
      "productId": "c4e4ec76-55a2-41f6-a338-8aabd1822d80",
      "quantity": 2
    }
  ]
}
```

Important rules:

1. Customer id is required.
2. Customer must exist.
3. Order must contain at least one order item.
4. Product must exist.
5. Inventory item must exist for the product.
6. Quantity must be greater than zero.
7. Stock must be available.
8. The same product cannot appear more than once in the same order.
9. Creating an order reduces inventory quantity.

### PUT /api/Orders/{id}/status

Updates order status.

Valid statuses:

```text
Pending
Processing
Packed
Shipped
Cancelled
Completed
```

Allowed normal status flow:

```text
Pending -> Processing -> Packed -> Shipped -> Completed
```

Cancellation is allowed from:

```text
Pending
Processing
Packed
```

Completed or shipped orders cannot be cancelled.

### PUT /api/Orders/{id}/cancel

Cancels an order and returns ordered quantity to inventory.

## Shipments

Base route:

```text
/api/Shipments
```

### GET /api/Shipments

Returns all shipments.

### GET /api/Shipments/{id}

Returns one shipment by id.

### POST /api/Shipments

Creates a shipment for an order.

Example request:

```json
{
  "orderId": "53049767-72ce-4167-9d40-6536f8f7abe8",
  "trackingNumber": "SE-TRK-1001"
}
```

Important rules:

1. Order id is required.
2. Order must exist.
3. Tracking number is required.
4. Shipment cannot be created for a cancelled order.
5. One order can only have one shipment.

### PUT /api/Shipments/{id}/status

Updates shipment status.

Valid statuses:

```text
Pending
Packed
Shipped
Delivered
Delayed
Cancelled
```

Allowed normal status flow:

```text
Pending -> Packed -> Shipped -> Delivered
```

When status becomes Shipped, `ShippedDate` is set.

When status becomes Delivered, `DeliveredDate` is set.

## Incidents

Base route:

```text
/api/Incidents
```

### GET /api/Incidents

Returns all incidents.

Optional query parameter:

```text
status
```

Example:

```text
GET /api/Incidents?status=Open
```

### GET /api/Incidents/{id}

Returns one incident by id.

### POST /api/Incidents

Creates an incident.

Example request:

```json
{
  "title": "Damaged package",
  "description": "A package was damaged during warehouse handling."
}
```

Important rules:

1. Title is required.
2. Description is required.
3. New incidents start with status Open.

### PUT /api/Incidents/{id}/status

Updates incident status.

Valid statuses:

```text
Open
InProgress
Resolved
Closed
```

### PUT /api/Incidents/{id}/resolve

Closes an incident with resolution notes.

Example request:

```json
{
  "resolutionNotes": "Package was inspected and replacement process was started."
}
```

Important rules:

1. Resolution notes are required.
2. ClosedAt is set when the incident is closed.
3. Closed incidents cannot be resolved again.

## Audit Logs

Base route:

```text
/api/AuditLogs
```

### GET /api/AuditLogs

Returns all audit logs.

### GET /api/AuditLogs/{id}

Returns one audit log by id.

Current audit log implementation records product create, update and delete actions.

Example audit log entry:

```json
{
  "entityName": "Product",
  "action": "Created",
  "performedBy": "System",
  "changes": "Created product Barcode Scanner with SKU SCAN-001."
}
```

## Current limitation

Authentication and role based authorization are not implemented yet.

This will be added in a later step.
