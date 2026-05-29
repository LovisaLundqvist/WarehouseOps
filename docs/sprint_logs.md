# Sprint Logs

## Sprint 1: Project setup and backend foundation

### Sprint goal

Create the basic project structure and backend foundation for WarehouseOps.

### Planned work

1. Create repository structure.
2. Create backend solution.
3. Create Api, Application, Domain, Infrastructure and Tests projects.
4. Create documentation folder.
5. Add initial markdown documentation files.
6. Configure Git and GitHub.

### Completed work

1. Created the WarehouseOps folder structure.
2. Created the backend solution.
3. Added the following backend projects:
   1. WarehouseOps.Api
   2. WarehouseOps.Application
   3. WarehouseOps.Domain
   4. WarehouseOps.Infrastructure
   5. WarehouseOps.Tests
4. Added docs folder.
5. Added initial documentation files.
6. Added .gitignore.
7. Pushed project to GitHub.

### Problems

1. Git was not installed at first.
2. bin and obj folders were accidentally tracked by Git.
3. Empty frontend folder was not tracked because Git does not track empty folders.

### Solutions

1. Installed Git.
2. Removed bin and obj from Git tracking.
3. Added bin, obj and .vs to .gitignore.

### Result

Sprint 1 was completed.

## Sprint 2: Domain model and database setup

### Sprint goal

Create the domain model and configure Entity Framework Core with SQL Server LocalDB.

### Planned work

1. Create domain entities.
2. Create enums.
3. Configure ApplicationDbContext.
4. Add EF Core packages.
5. Add initial migration.
6. Create database.

### Completed work

1. Created BaseEntity.
2. Created Product.
3. Created InventoryItem.
4. Created Customer.
5. Created Order.
6. Created OrderItem.
7. Created Shipment.
8. Created Incident.
9. Created AuditLog.
10. Created enums:
    1. OrderStatus
    2. ShipmentStatus
    3. IncidentStatus
    4. UserRole
11. Configured ApplicationDbContext.
12. Added DbSets for all main entities.
13. Added decimal precision for:
    1. Product.Price
    2. Order.TotalAmount
    3. OrderItem.UnitPrice
14. Added EF Core migrations.
15. Created local SQL Server database.

### Problems

1. EF Core warned about decimal precision.
2. UpdatedAt was added after the first migration.
3. Product.Category was added after the first migration.

### Solutions

1. Added HasPrecision(18, 2) for decimal fields.
2. Created a second migration for Product.Category and UpdatedAt.

### Result

Sprint 2 was completed.

## Sprint 3: Product API

### Sprint goal

Create a complete Product API with clean layered architecture.

### Planned work

1. Create product DTOs.
2. Create product interfaces.
3. Create ProductService.
4. Create ProductRepository.
5. Create ProductsController.
6. Register dependencies in Program.cs.
7. Test API in Swagger.

### Completed work

1. Created ProductDto.
2. Created CreateProductRequest.
3. Created UpdateProductRequest.
4. Created IProductService.
5. Created IProductRepository.
6. Created ProductService.
7. Created ProductRepository.
8. Created ProductsController.
9. Registered ProductService and ProductRepository in Program.cs.
10. Tested endpoints in Swagger:
    1. GET /api/Products
    2. POST /api/Products
    3. GET /api/Products/{id}
    4. PUT /api/Products/{id}
    5. DELETE /api/Products/{id}

### Important rules implemented

1. Product name is required.
2. SKU is required.
3. Category is required.
4. Price cannot be negative.
5. SKU must be unique.
6. Search and category filtering are supported.

### Result

Product API was completed and pushed to GitHub.

## Sprint 4: Inventory API and Customer API

### Sprint goal

Create APIs for inventory and customers so that orders can be tested later.

### Planned work

1. Create Inventory API.
2. Create Customer API.
3. Test both APIs in Swagger.
4. Push changes to GitHub.

### Completed work

Inventory API:

1. Created InventoryItemDto.
2. Created CreateInventoryItemRequest.
3. Created UpdateInventoryItemRequest.
4. Created IInventoryService.
5. Created IInventoryRepository.
6. Created InventoryService.
7. Created InventoryRepository.
8. Created InventoryController.
9. Registered dependencies in Program.cs.
10. Tested inventory endpoints in Swagger.

Customer API:

1. Created CustomerDto.
2. Created CreateCustomerRequest.
3. Created UpdateCustomerRequest.
4. Created ICustomerService.
5. Created ICustomerRepository.
6. Created CustomerService.
7. Created CustomerRepository.
8. Created CustomersController.
9. Registered dependencies in Program.cs.
10. Tested customer endpoints in Swagger.

### Important inventory rules implemented

1. Product must exist before inventory can be created.
2. One product can only have one inventory item.
3. Quantity cannot be negative.
4. Minimum stock level cannot be negative.
5. Low stock endpoint returns products at or below minimum stock level.

### Important customer rules implemented

1. Name is required.
2. Email is required.
3. Email must contain @.
4. Email must be unique.
5. Customer search is supported.

### Problems

1. Customer API initially returned an error because ICustomerService was not registered in Program.cs.
2. This was fixed by registering CustomerService and CustomerRepository.

### Result

Inventory API and Customer API were completed and pushed to GitHub.

## Sprint 5: Order API

### Sprint goal

Create an Order API that connects customers, products and inventory.

### Planned work

1. Create order DTOs.
2. Create order interfaces.
3. Create OrderService.
4. Create OrderRepository.
5. Create OrdersController.
6. Register dependencies in Program.cs.
7. Test order creation and status changes in Swagger.

### Completed work

1. Created OrderDto.
2. Created OrderItemDto.
3. Created CreateOrderRequest.
4. Created CreateOrderItemRequest.
5. Created UpdateOrderStatusRequest.
6. Created IOrderService.
7. Created IOrderRepository.
8. Created OrderService.
9. Created OrderRepository.
10. Created OrdersController.
11. Registered dependencies in Program.cs.
12. Tested order creation in Swagger.
13. Tested order status flow:
    1. Pending
    2. Processing
    3. Packed
    4. Shipped
    5. Completed
14. Verified that inventory quantity was reduced when an order was created.

### Important rules implemented

1. Customer must exist.
2. Order must contain at least one order item.
3. Product must exist.
4. Inventory item must exist for the product.
5. Quantity must be greater than zero.
6. Stock must be available.
7. The same product cannot appear twice in the same order.
8. Creating an order reduces inventory quantity.
9. Invalid status transitions are rejected.
10. Completed or shipped orders cannot be cancelled.

### Result

Order API was completed and pushed to GitHub.

## Sprint 6: Shipment API and Incident API

### Sprint goal

Create APIs for shipments and incidents.

### Planned work

1. Create Shipment API.
2. Create Incident API.
3. Test both APIs in Swagger.
4. Push changes to GitHub.

### Completed work

Shipment API:

1. Created ShipmentDto.
2. Created CreateShipmentRequest.
3. Created UpdateShipmentStatusRequest.
4. Created IShipmentService.
5. Created IShipmentRepository.
6. Created ShipmentService.
7. Created ShipmentRepository.
8. Created ShipmentsController.
9. Registered dependencies in Program.cs.
10. Tested shipment creation in Swagger.
11. Tested status flow:
    1. Pending
    2. Packed
    3. Shipped
    4. Delivered
12. Verified that ShippedDate is set when status becomes Shipped.
13. Verified that DeliveredDate is set when status becomes Delivered.

Incident API:

1. Created IncidentDto.
2. Created CreateIncidentRequest.
3. Created UpdateIncidentStatusRequest.
4. Created ResolveIncidentRequest.
5. Created IIncidentService.
6. Created IIncidentRepository.
7. Created IncidentService.
8. Created IncidentRepository.
9. Created IncidentsController.
10. Registered dependencies in Program.cs.
11. Tested incident creation in Swagger.
12. Tested status update to InProgress.
13. Tested resolving incident with resolution notes.
14. Verified that ClosedAt is set when incident is closed.

### Important shipment rules implemented

1. Order must exist.
2. Tracking number is required.
3. Shipment cannot be created for a cancelled order.
4. One order can only have one shipment.
5. Delivered can only be reached through the correct status flow.

### Important incident rules implemented

1. Title is required.
2. Description is required.
3. New incidents start as Open.
4. Resolution notes are required when closing an incident.
5. ClosedAt is set when incident is closed.
6. Closed incidents cannot be resolved again.

### Problems

1. Shipment status test was first attempted on the Product endpoint by mistake.
2. Incident API initially returned an error because IIncidentService was not registered in Program.cs.

### Solutions

1. Re-tested Shipment API on the correct endpoint.
2. Registered IncidentService and IncidentRepository in Program.cs.

### Result

Shipment API and Incident API were completed and pushed to GitHub.

## Sprint 7: Audit Log and backend documentation

### Sprint goal

Add audit logging and update backend documentation before starting frontend development.

### Planned work

1. Create Audit Log API.
2. Add audit logging to ProductService.
3. Test audit log in Swagger.
4. Update backend documentation.
5. Push changes to GitHub.

### Completed work

Audit Log:

1. Created AuditLogDto.
2. Created IAuditLogService.
3. Created IAuditLogRepository.
4. Created AuditLogService.
5. Created AuditLogRepository.
6. Created AuditLogsController.
7. Registered dependencies in Program.cs.
8. Added audit logging to ProductService.
9. Tested audit logging by creating a product.
10. Verified audit log with GET /api/AuditLogs.

Documentation:

1. Updated api_documentation.md.
2. Updated database_design.md.
3. Updated security_owasp.md.
4. Updated sprint_logs.md.

### Important rules implemented

1. Product creation is logged.
2. Product updates are logged.
3. Product deletion is logged.
4. Audit logs include entity name, action, performed by, timestamp and change description.

### Current limitation

Audit log currently uses:

```text
System
```

as PerformedBy because authentication is not implemented yet.

When login and roles are added, audit log should use the authenticated user.

### Result

Audit Log was completed and backend documentation was updated.

## Current backend status

Completed:

1. Product API
2. Inventory API
3. Customer API
4. Order API
5. Shipment API
6. Incident API
7. Audit Log
8. Backend documentation update

Next planned step:

```text
React frontend with layout, sidebar and dashboard
```
