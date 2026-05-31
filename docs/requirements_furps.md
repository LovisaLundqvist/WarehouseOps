# Requirements and FURPS+

This document describes the main requirements for WarehouseOps using the FURPS+ model.

WarehouseOps is a fullstack warehouse operations system for a fictional B2B technology import company. The company imports laptops, monitors, networking hardware, storage devices and accessories, then sells them to retailers such as Elgiganten, Power, Inet and Dustin.

The purpose of the system is to support daily warehouse operations, order handling, shipment tracking, incident follow up and auditability.

## FURPS+ overview

FURPS+ is used to structure both functional and quality requirements.

FURPS+ stands for:

* Functionality
* Usability
* Reliability
* Performance
* Supportability
* Plus constraints

## Functionality requirements

### Authentication

The system shall allow users to log in with email and password.

The system shall return a JWT token after successful login.

The system shall support logout in the frontend by clearing the current user session.

The system shall support three roles:

* Admin
* WarehouseStaff
* Manager

### Authorization

The system shall protect backend endpoints with authentication.

The system shall enforce role based authorization in the backend.

The frontend shall hide actions that the current user role is not allowed to perform.

Backend authorization is the security boundary. Frontend role based rendering is used for usability.

### Dashboard

The system shall show an operational dashboard.

The dashboard shall show:

* Active orders
* Low stock items
* Open incidents
* Active shipments
* Order status distribution
* Recent audit log activity
* Recent shipments

### Products

The system shall allow Admin users to:

* Create products
* View products
* Update products
* Delete products
* Search products
* Filter products by category

WarehouseStaff and Manager users shall be able to view products.

WarehouseStaff and Manager users shall not be able to create, update or delete products directly.

Products used in inventory or order history shall not be deleted.

### Inventory

The system shall allow Admin and WarehouseStaff users to:

* Add products to inventory
* View inventory
* Update inventory quantity
* Update minimum stock level
* View low stock items

Manager users shall be able to view inventory but not update it.

A product must exist before it can be added to inventory.

A product can only have one inventory item.

Quantity in stock cannot be negative.

Minimum stock level cannot be negative.

### Customers

The system shall allow Admin and WarehouseStaff users to:

* Create customers
* View customers
* Update customers

Manager users shall be able to view customers but not create or update them.

### Orders

The system shall allow Admin and WarehouseStaff users to:

* Create orders
* View orders
* View order items
* Update order status
* Cancel orders

Manager users shall be able to view orders and order details only.

Orders shall require a valid customer.

Orders shall contain at least one order item.

Products must exist in inventory before they can be ordered.

Orders cannot reserve more stock than available.

Cancelling an order shall return items to inventory.

Shipped and completed orders cannot be cancelled.

### Shipments

The system shall allow Admin and WarehouseStaff users to:

* Create shipments from orders
* View shipments
* Update shipment status

Manager users shall be able to view shipments only.

A shipment must be connected to an order.

Cancelled orders cannot receive shipments.

An order can only have one shipment.

Delivered and cancelled shipments cannot be updated.

### Incidents

The system shall allow Admin and WarehouseStaff users to:

* Report incidents
* View incidents
* Filter incidents by status
* Update incident status
* Close incidents with resolution notes

Manager users shall be able to view incidents only.

Incident title and description are required.

Incident severity must be valid.

Related entity type must be valid.

Resolution notes are required when closing an incident.

Closed incidents cannot be reopened.

### Audit log

The system shall create audit log entries for important changes.

Audit logs shall include:

* Entity name
* Action
* Performed by
* Performed at
* Change description

Audit logs shall use the currently authenticated user when available.

Admin and Manager users shall be able to view audit logs.

WarehouseStaff users shall create audit log entries through their actions, but shall not be able to view audit logs.

## Usability requirements

The frontend shall be understandable for non technical users.

The user interface shall use clear business language.

The navigation shall be consistent across pages.

Users shall only see actions that their role can perform.

Forms shall show validation messages before invalid data is submitted.

Tables shall present data in a readable format.

The dashboard shall provide a quick overview of warehouse operations.

The system shall include demo data so the Docker version is useful immediately after startup.

## Reliability requirements

The backend shall validate important business rules.

The backend shall return clear HTTP responses for common errors.

Unexpected backend errors shall be handled by global exception middleware.

The system shall avoid returning stack traces or SQL errors to users.

Docker Compose shall start SQL Server, backend and frontend together.

The Docker database shall be migrated automatically when configured.

Demo seed data shall only be inserted when the database is empty.

Backend tests shall verify important business rules.

GitHub Actions shall run build and test checks on push and pull request to main.

## Performance requirements

The system is designed for a portfolio and local development scenario.

Expected performance requirements:

* Dashboard should load quickly with demo data.
* Product and inventory lists should load quickly with normal demo data size.
* API endpoints should avoid unnecessary business logic in controllers.
* Frontend server state should be cached through React Query.
* Docker startup should be reasonable for local development.

Large scale performance optimization is outside the current scope.

## Supportability requirements

The system shall follow a clear layered architecture.

The backend shall be separated into:

* Api
* Application
* Domain
* Infrastructure
* Tests

Controllers shall not contain business logic.

Application services shall contain business logic.

Domain shall contain entities and enums.

Infrastructure shall contain EF Core, repositories and technical implementations.

The frontend shall be separated into:

* API clients
* Auth context
* Components
* Layouts
* Pages
* Routes
* Types
* Utilities

The project shall include documentation in the docs folder.

The project shall include a README with setup instructions.

The project shall include Docker support for local setup.

The project shall include GitHub Actions CI.

## Plus constraints

### Technical constraints

The backend shall use:

* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQL Server
* JWT authentication
* Swagger

The frontend shall use:

* React
* TypeScript
* Vite
* React Router
* Axios
* TanStack React Query
* React Hook Form
* Zod
* Tailwind CSS

Testing shall use:

* xUnit
* Moq
* FluentAssertions

DevOps shall use:

* Docker
* Docker Compose
* GitHub Actions

### Security constraints

The system shall not store passwords in plain text.

The system shall not store secrets directly in appsettings.json.

The system shall not commit .env to Git.

The system shall use backend authorization for protected actions.

The system shall avoid raw SQL with user input.

The system shall avoid exposing internal exception details to users.

The system shall keep audit logs for important changes.

Docker Compose shall not expose SQL Server to the host machine.

### Design constraints

The project shall not include diagrams.

The code should be simple and professional.

The architecture should be easy to explain in a portfolio or interview.

The project should be realistic enough to represent a warehouse operations module, but not unnecessarily complex.

## Requirement status

| Area | Status |
|---|---|
| Authentication | Implemented |
| Role based authorization | Implemented |
| Dashboard | Implemented |
| Products | Implemented |
| Inventory | Implemented |
| Customers | Implemented |
| Orders | Implemented |
| Shipments | Implemented |
| Incidents | Implemented |
| Audit logs | Implemented |
| Global exception handling | Implemented |
| Backend tests | Implemented |
| GitHub Actions CI | Implemented |
| Docker Compose | Implemented |
| Demo seed data | Implemented |
| Full docs | Implemented |
| Screenshots | Implemented |