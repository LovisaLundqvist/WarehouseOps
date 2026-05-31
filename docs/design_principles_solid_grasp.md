# Design Principles: SOLID and GRASP

This document describes how WarehouseOps uses SOLID and GRASP principles in the project structure.

The goal is not to overcomplicate the code. The goal is to keep responsibilities clear, make the code easier to test and make the architecture easy to explain.

## Design goal

WarehouseOps follows a simple layered design:

Api
Application
Domain
Infrastructure
Tests

The main idea is:

* Controllers handle HTTP requests and responses.
* Services contain business logic.
* Repositories handle database access.
* Domain contains entities and enums.
* Tests verify business rules.

This keeps the code organized and avoids putting too much responsibility in one place.

## SOLID overview

SOLID is a set of object oriented design principles.

SOLID stands for:

* Single Responsibility Principle
* Open Closed Principle
* Liskov Substitution Principle
* Interface Segregation Principle
* Dependency Inversion Principle

WarehouseOps applies these principles in a practical and simple way.

## Single Responsibility Principle

A class should have one main responsibility.

WarehouseOps examples:

### Controllers

Controllers are responsible for HTTP communication.

Examples:

* ProductsController receives product requests.
* OrdersController receives order requests.
* ShipmentsController receives shipment requests.

Controllers do not contain business logic.

They call application services.

### Services

Services are responsible for business rules.

Examples:

* ProductService validates products and product rules.
* InventoryService handles inventory rules.
* OrderService handles stock reservation and order status rules.
* ShipmentService handles shipment creation and shipment status rules.
* IncidentService handles incident status and closing rules.

### Repositories

Repositories are responsible for database access.

Examples:

* ProductRepository reads and writes products.
* OrderRepository reads and writes orders.
* ShipmentRepository reads and writes shipments.

This separation makes the code easier to understand and test.

## Open Closed Principle

Code should be open for extension but closed for unnecessary modification.

WarehouseOps applies this mainly through interfaces and services.

Examples:

* IProductService can be used without depending directly on ProductService.
* IProductRepository can be used without depending directly on Entity Framework Core.
* IAuditLogService can be used by other services without knowing how audit logs are saved.

This makes it easier to replace or extend implementations later.

Example:

If audit logging is moved from SQL Server to another logging system in the future, services can still depend on IAuditLogService.

## Liskov Substitution Principle

Objects should be replaceable by their interface or base type without breaking the system.

WarehouseOps uses interfaces for services and repositories.

Examples:

* ProductService depends on IProductRepository.
* InventoryService depends on IInventoryRepository.
* OrderService depends on IOrderRepository.
* Tests use mocked repositories instead of real repositories.

This works because the services rely on expected behavior from the interfaces, not on concrete database classes.

## Interface Segregation Principle

Interfaces should not force classes to depend on methods they do not need.

WarehouseOps uses focused interfaces.

Examples:

* IProductRepository contains product related methods.
* IInventoryRepository contains inventory related methods.
* IOrderRepository contains order related methods.
* IShipmentRepository contains shipment related methods.
* IIncidentRepository contains incident related methods.

The services only depend on interfaces that match their business area.

This is better than one large repository interface containing every method in the system.

## Dependency Inversion Principle

High level business logic should not depend directly on low level technical details.

WarehouseOps applies this through dependency injection.

Examples:

* ProductService depends on IProductRepository, not ProductRepository.
* OrderService depends on IOrderRepository, not ApplicationDbContext.
* AuditLogService depends on IAuditLogRepository, not direct SQL code.
* Controllers depend on service interfaces, not concrete service details.

The concrete implementations are registered in Program.cs.

This makes the Application layer easier to test because tests can use Moq instead of a real database.

## GRASP overview

GRASP is a set of responsibility assignment principles.

WarehouseOps mainly applies:

* Information Expert
* Creator
* Controller
* Low Coupling
* High Cohesion
* Indirection
* Protected Variations

## Information Expert

The class that has the information needed should be responsible for the related logic.

WarehouseOps examples:

### OrderService

OrderService handles order rules because it works with:

* Orders
* Order items
* Products
* Inventory
* Order status changes

It has the information needed to decide whether an order can be created or cancelled.

### InventoryService

InventoryService handles inventory rules because it works with:

* Quantity in stock
* Minimum stock level
* Product inventory connection

It has the information needed to decide whether stock is low or whether an update is valid.

### ShipmentService

ShipmentService handles shipment rules because it works with:

* Shipment status
* Order status
* Tracking number
* Delivered date

It has the information needed to decide whether a shipment can be created or updated.

## Creator

A class should create objects when it has the information needed to initialize them correctly.

WarehouseOps examples:

### ProductService creates Product

ProductService creates Product entities from CreateProductRequest.

It validates the request before creating the entity.

### OrderService creates Order and OrderItem

OrderService creates orders because it needs to validate:

* Customer exists
* Products exist
* Products exist in inventory
* Stock is available
* Same product is not added twice

### ShipmentService creates Shipment

ShipmentService creates shipments because it validates:

* Order exists
* Order is not cancelled
* Order does not already have a shipment
* Tracking number is valid

## Controller

In GRASP, a controller receives system events and delegates work to the right objects.

In WarehouseOps, API controllers act as HTTP controllers.

Examples:

* ProductsController delegates to IProductService.
* OrdersController delegates to IOrderService.
* IncidentsController delegates to IIncidentService.

Controllers do not solve the business problem themselves.

They coordinate the request and response.

## Low Coupling

Low coupling means that classes should avoid unnecessary dependencies on each other.

WarehouseOps uses low coupling by:

* Depending on interfaces instead of concrete implementations.
* Keeping controllers separate from EF Core.
* Keeping business logic out of controllers.
* Keeping database logic in repositories.
* Keeping domain entities independent from API and frontend code.

Example:

OrderService does not depend directly on ApplicationDbContext. It depends on IOrderRepository.

This makes the service easier to test.

## High Cohesion

High cohesion means that a class should contain related responsibilities.

WarehouseOps examples:

### ProductService

ProductService only handles product related rules.

### InventoryService

InventoryService only handles inventory related rules.

### IncidentService

IncidentService only handles incident related rules.

This makes each service easier to understand.

A class with high cohesion is usually easier to maintain than a class that does many unrelated things.

## Indirection

Indirection means using an intermediate object to reduce direct dependency.

WarehouseOps uses indirection through interfaces.

Examples:

* Controllers communicate with services through service interfaces.
* Services communicate with repositories through repository interfaces.
* Audit logging is accessed through IAuditLogService.
* Current user information is accessed through ICurrentUserService.

This avoids direct dependencies between unrelated parts of the system.

## Protected Variations

Protected Variations means protecting the system from changes by hiding unstable details behind stable interfaces.

WarehouseOps examples:

### Database access

Application services do not know the details of EF Core queries.

Repositories hide database details.

If the persistence logic changes, the services should not need major changes.

### Audit logging

Services call IAuditLogService.

If audit logging changes later, the services can still call the same interface.

### Current user handling

AuditLogService uses ICurrentUserService.

The logic for reading the current HTTP user is not spread across all services.

## Practical examples in WarehouseOps

### Product deletion

The product deletion rule is handled in ProductService.

ProductService checks whether the product is used before allowing deletion.

This keeps the rule in the business layer instead of the controller.

### Order creation

OrderService handles order creation because it must coordinate several rules:

* Customer must exist.
* Products must exist.
* Products must exist in inventory.
* Stock must be available.
* Inventory must be reduced.
* Audit log must be created.

This is business logic and belongs in the Application layer.

### Order cancellation

OrderService handles cancellation because it must:

* Check current order status.
* Return items to inventory.
* Update order status.
* Create audit log entry.

### Shipment creation

ShipmentService handles shipment creation because it must:

* Check that the order exists.
* Check that the order is not cancelled.
* Check that the order does not already have a shipment.
* Create shipment.
* Create audit log entry.

### Incident closing

IncidentService handles incident closing because it must:

* Require resolution notes.
* Prevent already closed incidents from being closed again.
* Set ClosedAt.
* Create audit log entry.

## Why business logic is not in controllers

Putting business logic in controllers would make the project harder to maintain.

Problems with business logic in controllers:

* Controllers become too large.
* Business rules are harder to test.
* Logic can be duplicated between endpoints.
* HTTP concerns mix with domain rules.

WarehouseOps avoids this by placing business rules in services.

## Why repositories are used

Repositories hide EF Core details from the Application layer.

Benefits:

* Services are easier to test.
* EF Core queries stay in Infrastructure.
* Business logic stays in Application.
* The code follows a clear layered structure.

## Why DTOs are used

DTOs separate API data from domain entities.

Benefits:

* The API does not expose entities directly.
* Request and response shapes are easier to control.
* Frontend gets only the data it needs.
* Validation becomes clearer.

## Current design limitations

Some areas can be improved later:

* Move dependency injection registration into extension methods.
* Add database uniqueness constraints.
* Add more integration tests.
* Replace demo authentication with ASP.NET Identity.
* Add stronger typed result handling instead of throwing exceptions for some business errors.

These are planned improvements, not blockers for the current portfolio version.

## Summary

WarehouseOps applies SOLID and GRASP by keeping responsibilities separated.

The most important design choices are:

* Controllers handle HTTP only.
* Services contain business logic.
* Repositories handle database access.
* Domain contains entities and enums.
* Interfaces reduce coupling.
* Tests focus on business rules.
* Audit logging and current user access are handled through dedicated services.

The result is a clean and understandable architecture that is suitable for a professional fullstack portfolio project.