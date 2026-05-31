# API Documentation

This document describes the main backend API endpoints in WarehouseOps.

The API is built with ASP.NET Core Web API and is available locally at:

http://localhost:5059

Swagger is available at:

http://localhost:5059/swagger

## Authentication

Most endpoints require a JWT token.

The token is sent with requests using the Authorization header:

Authorization: Bearer token

## Roles

WarehouseOps uses three roles:

* Admin
* WarehouseStaff
* Manager

Role groups used by the API:

| Role group | Roles |
|---|---|
| AllRoles | Admin, WarehouseStaff, Manager |
| AdminOrWarehouseStaff | Admin, WarehouseStaff |
| AdminOrManager | Admin, Manager |
| Admin | Admin only |

## Auth API

Base route:

api/Auth

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | /api/Auth/login | Public | Logs in a user and returns a JWT token. |

### Login request

Fields:

* email
* password

### Login response

Fields:

* token
* expiresAt
* email
* displayName
* role

## Dashboard API

Base route:

api/Dashboard

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Dashboard/summary | Admin, WarehouseStaff, Manager | Returns dashboard summary data. |

### Dashboard summary includes

* activeOrdersCount
* lowStockItemsCount
* openIncidentsCount
* activeShipmentsCount
* orderStatusCounts
* recentActivities
* recentShipments

## Products API

Base route:

api/Products

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Products | Admin, WarehouseStaff, Manager | Gets all products. Supports search and category query parameters. |
| GET | /api/Products/{id} | Admin, WarehouseStaff, Manager | Gets one product by id. |
| POST | /api/Products | Admin | Creates a product. |
| PUT | /api/Products/{id} | Admin | Updates a product. |
| DELETE | /api/Products/{id} | Admin | Deletes a product if it is not used by inventory or order history. |

### Product query parameters

| Parameter | Description |
|---|---|
| search | Optional text search. |
| category | Optional category filter. |

### Product request fields

Create product:

* name
* sku
* category
* description
* price

Update product:

* name
* sku
* category
* description
* price

### Product business rules

* Name is required.
* SKU is required.
* Category is required.
* Price cannot be negative.
* SKU must be unique.
* Products used in inventory or order history cannot be deleted.

## Inventory API

Base route:

api/Inventory

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Inventory | Admin, WarehouseStaff, Manager | Gets all inventory items. |
| GET | /api/Inventory/low-stock | Admin, WarehouseStaff, Manager | Gets inventory items at or below minimum stock level. |
| GET | /api/Inventory/{id} | Admin, WarehouseStaff, Manager | Gets one inventory item by id. |
| POST | /api/Inventory | Admin, WarehouseStaff | Adds a product to inventory. |
| PUT | /api/Inventory/{id} | Admin, WarehouseStaff | Updates inventory quantity and minimum stock level. |

### Inventory request fields

Create inventory item:

* productId
* quantityInStock
* minimumStockLevel

Update inventory item:

* quantityInStock
* minimumStockLevel

### Inventory business rules

* Product must exist.
* A product can only have one inventory item.
* Quantity in stock cannot be negative.
* Minimum stock level cannot be negative.
* Low stock is detected when quantity is at or below minimum stock level.

## Customers API

Base route:

api/Customers

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Customers | Admin, WarehouseStaff, Manager | Gets all customers. Supports optional search query. |
| GET | /api/Customers/{id} | Admin, WarehouseStaff, Manager | Gets one customer by id. |
| POST | /api/Customers | Admin, WarehouseStaff | Creates a customer. |
| PUT | /api/Customers/{id} | Admin, WarehouseStaff | Updates a customer. |

### Customer query parameters

| Parameter | Description |
|---|---|
| search | Optional search by customer information. |

### Customer request fields

Create customer:

* name
* email
* phoneNumber
* address

Update customer:

* name
* email
* phoneNumber
* address

### Customer business rules

* Customer name is required.
* Customer email is required.
* Customer records can be used when creating orders.

## Orders API

Base route:

api/Orders

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Orders | Admin, WarehouseStaff, Manager | Gets all orders. |
| GET | /api/Orders/{id} | Admin, WarehouseStaff, Manager | Gets one order by id. |
| POST | /api/Orders | Admin, WarehouseStaff | Creates an order. |
| PUT | /api/Orders/{id}/status | Admin, WarehouseStaff | Updates order status. |
| PUT | /api/Orders/{id}/cancel | Admin, WarehouseStaff | Cancels an order if cancellation is allowed. |

### Create order request fields

* customerId
* items

Each order item includes:

* productId
* quantity

### Update order status request fields

* status

### Order business rules

* Customer must exist.
* Order must contain at least one item.
* The same product cannot be added more than once to the same order.
* Product must exist in inventory before it can be ordered.
* Order cannot reserve more stock than available.
* Creating an order reduces inventory quantity.
* Cancelling an order returns items to inventory.
* Shipped and completed orders cannot be cancelled.

## Shipments API

Base route:

api/Shipments

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Shipments | Admin, WarehouseStaff, Manager | Gets all shipments. |
| GET | /api/Shipments/{id} | Admin, WarehouseStaff, Manager | Gets one shipment by id. |
| POST | /api/Shipments | Admin, WarehouseStaff | Creates a shipment from an order. |
| PUT | /api/Shipments/{id}/status | Admin, WarehouseStaff | Updates shipment status. |

### Create shipment request fields

* orderId
* trackingNumber

### Update shipment status request fields

* status

### Shipment business rules

* Order must exist.
* Tracking number is required.
* Cancelled orders cannot receive shipments.
* An order can only have one shipment.
* Delivered and cancelled shipments cannot be changed.
* Delivered date is set when shipment status becomes Delivered.

## Incidents API

Base route:

api/Incidents

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/Incidents | Admin, WarehouseStaff, Manager | Gets all incidents. Supports optional status query. |
| GET | /api/Incidents/{id} | Admin, WarehouseStaff, Manager | Gets one incident by id. |
| POST | /api/Incidents | Admin, WarehouseStaff | Creates an incident. |
| PUT | /api/Incidents/{id}/status | Admin, WarehouseStaff | Updates incident status. |
| PUT | /api/Incidents/{id}/resolve | Admin, WarehouseStaff | Closes an incident with resolution notes. |

### Incident query parameters

| Parameter | Description |
|---|---|
| status | Optional incident status filter. |

### Create incident request fields

* title
* description
* severity
* relatedEntityType
* relatedEntityId

### Update incident status request fields

* status

### Resolve incident request fields

* resolutionNotes

### Incident business rules

* Title is required.
* Description is required.
* Severity must be valid.
* Related entity type must be valid.
* Closed incidents cannot be reopened.
* Resolution notes are required when closing an incident.

## Audit Logs API

Base route:

api/AuditLogs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | /api/AuditLogs | Admin, Manager | Gets all audit logs. |
| GET | /api/AuditLogs/{id} | Admin, Manager | Gets one audit log by id. |

### Audit log fields

* id
* entityName
* entityId
* action
* performedBy
* performedAt
* changes
* createdAt

### Audit log behavior

Audit logs are created for important business changes.

Examples:

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
* Incident resolved

Audit logs use the currently authenticated user when available.

## Common HTTP status codes

| Status code | Meaning |
|---|---|
| 200 OK | Request succeeded. |
| 201 Created | Resource was created. |
| 204 No Content | Resource was deleted successfully. |
| 400 Bad Request | Invalid input or invalid business request. |
| 401 Unauthorized | Missing or invalid JWT token. |
| 403 Forbidden | User is authenticated but does not have the required role. |
| 404 Not Found | Resource was not found. |
| 409 Conflict | Request conflicts with current business state. |
| 500 Internal Server Error | Unexpected server error handled by global exception middleware. |

## Swagger

Swagger is the best place to inspect exact request and response schemas while running the API.

Local Swagger URL:

http://localhost:5059/swagger