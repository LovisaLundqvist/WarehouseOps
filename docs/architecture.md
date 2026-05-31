# Architecture

WarehouseOps is a fullstack warehouse operations system built with React, ASP.NET Core Web API and SQL Server.

The system is designed for a fictional B2B technology import company that imports products such as laptops, monitors, networking equipment, storage devices and accessories, then sells them to retailers such as Elgiganten, Power, Inet and Dustin.

## Project structure

WarehouseOps/
  backend/
    WarehouseOps.Api/
    WarehouseOps.Application/
    WarehouseOps.Domain/
    WarehouseOps.Infrastructure/
    WarehouseOps.Tests/
  frontend/
    warehouseops-client/
  docs/
  docker-compose.yml
  README.md

## Backend architecture

The backend follows a layered architecture.

Layers:

Api
Application
Domain
Infrastructure
Tests

The main rule is that controllers should not contain business logic. Controllers receive HTTP requests, call services and return HTTP responses.

Business logic belongs in the Application layer.

## WarehouseOps.Api

The API layer contains the HTTP entry points of the system.

Responsibilities:

* Controllers
* Authentication setup
* Authorization setup
* Swagger setup
* CORS setup
* Global exception handling middleware
* Current user service for audit logging
* Dependency injection setup

Examples:

* AuthController
* ProductsController
* InventoryController
* CustomersController
* OrdersController
* ShipmentsController
* IncidentsController
* DashboardController
* AuditLogsController

Controllers call application services. They do not directly contain business rules.

## WarehouseOps.Application

The Application layer contains use case logic and business rules.

Responsibilities:

* Service interfaces
* Service implementations
* DTOs
* Request models
* Business validation
* Audit log calls

Examples:

* ProductService
* InventoryService
* CustomerService
* OrderService
* ShipmentService
* IncidentService
* DashboardService
* AuditLogService
* AuthService

This layer decides what is allowed in the business process.

Examples of business rules:

* A product SKU must be unique.
* A product used in inventory or order history cannot be deleted.
* A product must exist in inventory before it can be ordered.
* An order cannot reserve more stock than available.
* Cancelling an order returns items to inventory.
* A cancelled order cannot get a shipment.
* An order can only have one shipment.
* Closed incidents cannot be reopened.

## WarehouseOps.Domain

The Domain layer contains the core business model.

Responsibilities:

* Entities
* Enums
* Shared base entity

Examples of entities:

* Product
* InventoryItem
* Customer
* Order
* OrderItem
* Shipment
* Incident
* AuditLog

Examples of enums:

* OrderStatus
* ShipmentStatus
* IncidentStatus
* IncidentSeverity
* IncidentRelatedEntityType
* UserRole

The Domain layer does not depend on Api, Application or Infrastructure.

## WarehouseOps.Infrastructure

The Infrastructure layer contains technical implementation details.

Responsibilities:

* Entity Framework Core DbContext
* SQL Server persistence
* Repository implementations
* EF Core migrations
* Demo seed data
* JWT token implementation

Examples:

* ApplicationDbContext
* ProductRepository
* InventoryRepository
* CustomerRepository
* OrderRepository
* ShipmentRepository
* IncidentRepository
* AuditLogRepository
* DashboardRepository
* DatabaseSeeder

Repositories hide EF Core details from the application services.

## WarehouseOps.Tests

The test project contains backend service tests.

Current test areas:

* ProductServiceTests
* InventoryServiceTests
* OrderServiceTests
* ShipmentServiceTests
* IncidentServiceTests

The tests use:

* xUnit
* Moq
* FluentAssertions

The test focus is business rules in the Application layer.

## Frontend architecture

The frontend is a React TypeScript application built with Vite.

Responsibilities:

* Routing
* Layout
* Authentication state
* Protected routes
* Role based UI actions
* API communication
* Form validation
* Dashboard presentation

Main frontend folders:

* api
* auth
* components
* layouts
* pages
* routes
* types
* utils

## API communication

The frontend communicates with the backend through Axios based API clients.

Examples:

* productsApi.ts
* inventoryApi.ts
* customersApi.ts
* ordersApi.ts
* shipmentsApi.ts
* incidentsApi.ts
* dashboardApi.ts
* auditLogsApi.ts
* authApi.ts

TanStack React Query is used for server state, loading states and cache invalidation.

## Authentication flow

The user logs in through the frontend login page.

The frontend sends email and password to:

POST /api/Auth/login

The backend validates the demo user credentials and returns a JWT token.

The frontend sends the token with later API requests:

Authorization: Bearer token

The backend validates the token and checks role based authorization.

## Role based access

WarehouseOps has three roles:

* Admin
* WarehouseStaff
* Manager

Authorization is enforced in the backend.

The frontend also hides actions that the current user role is not allowed to perform.

Backend authorization is the security boundary. Frontend role based rendering is used for usability.

## Role behavior summary

### Admin

Admin has full administrative access.

Admin can manage products, inventory, customers, orders, shipments, incidents, dashboard and audit logs.

### WarehouseStaff

WarehouseStaff handles daily warehouse operations.

WarehouseStaff can view products, add products to inventory, update inventory, manage customers, manage orders, manage shipments and manage incidents.

WarehouseStaff cannot create, update or delete products directly.

WarehouseStaff cannot view audit logs.

### Manager

Manager has read only access to operational data and audit history.

Manager cannot create, update, cancel or delete operational records.

## Database

WarehouseOps uses SQL Server through Entity Framework Core.

The database stores:

* Products
* Inventory items
* Customers
* Orders
* Order items
* Shipments
* Incidents
* Audit logs

EF Core migrations are used to create and update the database schema.

## Docker architecture

Docker Compose runs three services:

| Service | Purpose |
|---|---|
| sqlserver | SQL Server database |
| backend | ASP.NET Core Web API |
| frontend | React app served by Nginx |

The backend connects to SQL Server through the internal Docker network.

Frontend is available at:

http://localhost:3000

Backend is available at:

http://localhost:5059

SQL Server is not exposed to the host machine. It is only reachable inside the Docker network.

## CI architecture

GitHub Actions is used for continuous integration.

The workflow runs on push and pull request to main.

It checks:

* Backend restore
* Backend build
* Backend tests
* Frontend dependency installation
* Frontend build

Workflow file:

.github/workflows/ci.yml

## Design goal

The architecture is intentionally simple and clear.

The goal is to show clean separation of concerns, testable business logic, role based security, Docker based local deployment and CI validation.