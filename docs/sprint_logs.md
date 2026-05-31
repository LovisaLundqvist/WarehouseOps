# Sprint Logs

This document summarizes the development work completed for WarehouseOps.

The project was built step by step to keep the scope controlled and to make sure each part worked before moving to the next one.

## Project goal

WarehouseOps is a fullstack warehouse operations system for a fictional B2B technology import company.

The project goal is to demonstrate skills in:

* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQL Server
* React
* TypeScript
* Authentication
* Role based authorization
* Testing
* Docker
* GitHub Actions
* Security aware development

## Sprint 1: Project structure and backend foundation

### Goal

Create the basic repository structure and backend solution.

### Completed work

* Created project root structure.
* Created backend solution.
* Created projects for Api, Application, Domain, Infrastructure and Tests.
* Added project references between layers.
* Verified that the solution builds.

### Result

The backend foundation was ready for domain modeling and API development.

## Sprint 2: Domain model

### Goal

Create the core domain entities and enums.

### Completed work

* Created domain entities for products, inventory, customers, orders, shipments, incidents and audit logs.
* Created enums for order status, shipment status, incident status, incident severity, related entity type and user roles.
* Added shared base properties such as Id, CreatedAt and UpdatedAt.

### Result

The domain layer contained the main business model for the warehouse system.

## Sprint 3: Infrastructure and database

### Goal

Set up Entity Framework Core and SQL Server persistence.

### Completed work

* Created ApplicationDbContext.
* Added DbSets for all main entities.
* Configured relationships between entities.
* Added repository interfaces and repository implementations.
* Added EF Core migrations.
* Verified database creation locally.

### Result

The backend could persist and retrieve data through EF Core and SQL Server.

## Sprint 4: Product API

### Goal

Implement product management.

### Completed work

* Created product DTOs and request models.
* Created ProductService.
* Created ProductRepository.
* Created ProductsController.
* Added create, read, update, delete, search and category filtering.
* Added business rules for required fields, unique SKU and delete protection when product is in use.
* Added audit logging for product changes.

### Result

Products could be managed through the API and later through the frontend.

## Sprint 5: Inventory API

### Goal

Implement inventory management.

### Completed work

* Created inventory DTOs and request models.
* Created InventoryService.
* Created InventoryRepository.
* Created InventoryController.
* Added ability to add products to inventory.
* Added inventory updates.
* Added low stock detection.
* Added business rules for product existence, one inventory item per product and non negative stock values.
* Added audit logging for inventory changes.

### Result

Warehouse stock levels could be tracked and updated.

## Sprint 6: Customer API

### Goal

Implement customer management.

### Completed work

* Created customer DTOs and request models.
* Created CustomerService.
* Created CustomerRepository.
* Created CustomersController.
* Added create, read, update and search functionality.
* Added validation for required customer fields.
* Added audit logging for customer changes.

### Result

B2B customers could be managed in the system.

## Sprint 7: Order API

### Goal

Implement order handling.

### Completed work

* Created order DTOs and request models.
* Created OrderService.
* Created OrderRepository.
* Created OrdersController.
* Added order creation.
* Added order status updates.
* Added order cancellation.
* Added order item handling.
* Added inventory reduction when orders are created.
* Added inventory restoration when orders are cancelled.
* Added business rules for customer existence, inventory existence, stock availability and valid status changes.
* Added audit logging for order changes.

### Result

The system could handle B2B customer orders and stock reservation.

## Sprint 8: Shipment API

### Goal

Implement shipment handling.

### Completed work

* Created shipment DTOs and request models.
* Created ShipmentService.
* Created ShipmentRepository.
* Created ShipmentsController.
* Added shipment creation from orders.
* Added shipment status updates.
* Added delivered date handling.
* Added business rules for cancelled orders, duplicate shipments and final shipment statuses.
* Added audit logging for shipment changes.

### Result

The system could track deliveries connected to customer orders.

## Sprint 9: Incident API

### Goal

Implement incident reporting and resolution.

### Completed work

* Created incident DTOs and request models.
* Created IncidentService.
* Created IncidentRepository.
* Created IncidentsController.
* Added incident creation.
* Added incident status updates.
* Added incident closing with resolution notes.
* Added filtering by incident status.
* Added business rules for severity, related entity type, closed incidents and resolution notes.
* Added audit logging for incident changes.

### Result

Operational problems could be reported, tracked and closed.

## Sprint 10: Dashboard and audit log

### Goal

Create operational overview and change history.

### Completed work

* Created DashboardService.
* Created DashboardRepository.
* Created DashboardController.
* Created AuditLogService.
* Created AuditLogRepository.
* Created AuditLogsController.
* Added dashboard summary for active orders, low stock, open incidents and active shipments.
* Added recent activity and recent shipment data.
* Added audit log view support.

### Result

The system had operational overview and traceability.

## Sprint 11: React frontend foundation

### Goal

Create the frontend application and main layout.

### Completed work

* Created React TypeScript frontend with Vite.
* Added Tailwind CSS.
* Added React Router.
* Added Axios API clients.
* Added TanStack React Query.
* Created layout with sidebar navigation.
* Created dashboard page.
* Connected dashboard to backend API.

### Result

The frontend foundation was ready for business pages.

## Sprint 12: Frontend business pages

### Goal

Create pages for the main business areas.

### Completed work

* Created Products page.
* Created Inventory page.
* Created Customers page.
* Created Orders page.
* Created Shipments page.
* Created Incidents page.
* Created Change History page.
* Added forms and actions for supported operations.
* Connected pages to backend APIs.
* Added frontend validation where needed.
* Added dashboard charts and summary cards.

### Result

The main WarehouseOps workflow could be used through the frontend.

## Sprint 13: Authentication and roles

### Goal

Add login, logout and role based access.

### Completed work

* Added AuthController.
* Added AuthService.
* Added JWT token generation.
* Added password hashing service.
* Added demo users for Admin, WarehouseStaff and Manager.
* Added JWT authentication configuration.
* Added backend role based authorization.
* Added frontend auth context.
* Added protected routes.
* Added login page.
* Added logout support.
* Added role based UI actions.

### Result

Users could log in and see actions based on their role.

## Sprint 14: Security improvements

### Goal

Improve security and traceability.

### Completed work

* Added current user service.
* Updated audit log to use the authenticated user instead of only System.
* Added global exception handling middleware.
* Prevented internal technical errors from being returned to users.
* Verified backend authorization with Swagger.
* Verified Manager gets 403 Forbidden for restricted actions.
* Verified WarehouseStaff creates audit logs but cannot view audit history.

### Result

The system had stronger practical OWASP alignment.

## Sprint 15: Backend tests

### Goal

Add automated tests for core business logic.

### Completed work

* Configured xUnit test project.
* Added Moq.
* Added FluentAssertions.
* Created ProductServiceTests.
* Created InventoryServiceTests.
* Created OrderServiceTests.
* Created ShipmentServiceTests.
* Created IncidentServiceTests.
* Verified 50 backend service tests pass.

### Result

Important business rules are covered by automated tests.

## Sprint 16: GitHub Actions CI

### Goal

Add continuous integration.

### Completed work

* Created GitHub Actions workflow.
* Added backend restore.
* Added backend build.
* Added backend test execution.
* Added frontend dependency installation.
* Added frontend build.
* Updated checkout action to a newer version.
* Verified green CI on GitHub.

### Result

Every push and pull request to main is checked automatically.

## Sprint 17: Docker and Docker Compose

### Goal

Containerize the system.

### Completed work

* Added backend Dockerfile.
* Added frontend Dockerfile.
* Added frontend Nginx configuration.
* Added Docker ignore files.
* Added docker-compose.yml.
* Added SQL Server container.
* Added backend container.
* Added frontend container.
* Bound backend and frontend to 127.0.0.1.
* Kept SQL Server internal to Docker network.
* Added .env.example.
* Added .env to .gitignore.
* Tested docker compose build.
* Tested docker compose up.
* Verified frontend, backend and SQL Server run together.

### Result

The full system can run locally with Docker Compose.

## Sprint 18: Demo seed data

### Goal

Make the Docker version useful immediately after startup.

### Completed work

* Added DatabaseSeeder.
* Added technology import demo data.
* Seeded products, inventory, customers, orders, shipments, incidents and audit logs.
* Added automatic migration option for Docker.
* Added automatic seed option for Docker.
* Tested Docker database reset with docker compose down -v.
* Verified seeded data appears in frontend.

### Result

A new Docker database starts with realistic B2B technology warehouse data.

## Sprint 19: README and documentation

### Goal

Make the project understandable on GitHub.

### Completed work

* Wrote README.
* Documented project purpose.
* Documented tech stack.
* Documented user roles.
* Documented setup with Docker.
* Documented local setup.
* Documented tests and CI.
* Added architecture documentation.
* Added OWASP security documentation.
* Added FURPS+ requirements documentation.
* Added testing documentation.
* Added API documentation.
* Added database design documentation.
* Added SOLID and GRASP documentation.
* Added sprint logs.

### Result

The project is easier to review, run and discuss in a portfolio or interview.

## Current status

Implemented:

* Backend API
* React frontend
* SQL Server persistence
* Login and JWT authentication
* Role based authorization
* Role based frontend actions
* Dashboard
* Product management
* Inventory management
* Customer management
* Order management
* Shipment management
* Incident management
* Audit logs
* Global exception handling
* 50 backend service tests
* GitHub Actions CI
* Docker Compose
* Demo seed data
* README
* Documentation

## Remaining improvements

Possible future improvements:

* Add screenshots to README.
* Add controller integration tests.
* Add authorization integration tests.
* Add frontend tests.
* Add Docker build validation to GitHub Actions.
* Add stricter database uniqueness constraints.
* Replace demo users with ASP.NET Identity.
* Add refresh token support.
* Add rate limiting for login.
* Add supplier purchasing.
* Add return handling.
* Add barcode scanning.
* Add multi warehouse support.

## Summary

WarehouseOps was built iteratively from backend foundation to frontend, security, tests, CI, Docker and documentation.

The final result is a fullstack warehouse operations portfolio project with practical examples of architecture, business rules, testing, DevOps and security aware development.