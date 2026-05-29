# Security and OWASP

## Overview

WarehouseOps follows OWASP principles on a practical level.

The current backend version focuses on:

1. Input validation
2. Safe database access through Entity Framework Core
3. Controlled business rules in services
4. Safe error responses for expected validation errors
5. Audit logging for important product changes
6. Avoiding hardcoded secrets where possible

Authentication and role based authorization are planned for a later step.

## Current security status

Implemented:

1. Backend input validation
2. Entity Framework Core instead of raw SQL
3. Service layer business rules
4. Audit log endpoint
5. Audit logging for product create, update and delete
6. Local database connection through appsettings
7. No passwords stored in the current version

Not implemented yet:

1. Login
2. Logout
3. JWT or ASP.NET Identity
4. Role based authorization
5. Protected endpoints
6. User based audit identity
7. Global exception middleware
8. Frontend validation

These missing parts are planned for later steps.

## OWASP: Broken Access Control

Status:

```text
Planned
```

The current version does not yet have authentication or role based authorization.

Planned access rules:

1. Admin can manage products and view audit logs.
2. WarehouseStaff can manage inventory, orders, shipments and incidents.
3. Manager can view dashboard data and audit logs.
4. Authorization must be checked in the backend, not only in the frontend.

Future implementation should use attributes such as:

```csharp
[Authorize(Roles = "Admin")]
```

or policy based authorization.

Important principle:

```text
Hiding buttons in React is not enough. Backend endpoints must be protected.
```

## OWASP: Injection

Status:

```text
Partly implemented
```

WarehouseOps uses Entity Framework Core for database access.

Current implementation does not build raw SQL strings from user input.

Examples:

1. Product search uses LINQ.
2. Customer search uses LINQ.
3. Inventory queries use LINQ.
4. Orders, shipments and incidents use repository methods with EF Core.

This reduces SQL injection risk because EF Core parameterizes database queries.

Rule for future code:

```text
Do not concatenate user input into SQL strings.
```

## OWASP: Identification and Authentication Failures

Status:

```text
Planned
```

Authentication is not implemented yet.

Future implementation should include:

1. Login endpoint
2. Logout handling
3. Secure password hashing
4. JWT or ASP.NET Identity
5. Token expiration
6. No plaintext passwords
7. No hardcoded tokens or secrets in GitHub

Important rule:

```text
Passwords must never be stored in plaintext.
```

## OWASP: Software and Data Integrity Failures

Status:

```text
Partly implemented
```

The project uses Git and GitHub for version control.

Current protection:

1. Source code is tracked in Git.
2. Build output folders are ignored with .gitignore.
3. bin and obj are not supposed to be committed.
4. Changes are committed in small steps.

Future protection:

1. GitHub Actions should build the backend.
2. GitHub Actions should build the frontend.
3. GitHub Actions should run tests.
4. Dependencies should be reviewed before adding packages.

## OWASP: Security Misconfiguration

Status:

```text
Partly implemented
```

Current local configuration:

1. Swagger is enabled only in development.
2. Connection string is stored in appsettings for local development.
3. HTTPS redirection is currently kept in Program.cs.
4. SQL Server LocalDB is used locally.

Known local warning:

```text
Failed to determine the https port for redirect.
```

This warning is accepted for now because the API works locally through:

```text
http://localhost:5059/swagger
```

Future improvements:

1. Move secrets to environment variables for Docker.
2. Add safe production settings.
3. Add global exception middleware.
4. Avoid exposing stack traces outside development.
5. Disable Swagger in production unless explicitly needed.

## OWASP: Vulnerable and Outdated Components

Status:

```text
Planned
```

The project uses NuGet packages and later will use npm packages for frontend.

Future actions:

1. Keep packages updated.
2. Review package warnings.
3. Use GitHub Dependabot if useful.
4. Avoid unnecessary dependencies.
5. Remove unused packages.

## OWASP: Identification of Unsafe Design

Status:

```text
Partly implemented
```

The backend is split into four layers:

1. Api
2. Application
3. Domain
4. Infrastructure

This reduces unsafe design because controllers do not contain business logic.

Current design rules:

1. Controllers receive HTTP requests.
2. Services contain business logic.
3. Domain contains entities and enums.
4. Infrastructure contains EF Core and repositories.

Examples of service layer rules:

1. Product price cannot be negative.
2. Product SKU must be unique.
3. Inventory quantity cannot be negative.
4. Orders must contain at least one item.
5. Orders cannot use products without inventory.
6. Orders reduce inventory quantity.
7. Invalid order status changes are rejected.
8. Shipments cannot be duplicated for the same order.
9. Delivered shipment status requires the correct status flow.
10. Incidents require title and description.
11. Incidents require resolution notes when closed.

## OWASP: Security Logging and Monitoring Failures

Status:

```text
Partly implemented
```

Audit logging has been added.

Current audit log records:

1. Product created
2. Product updated
3. Product deleted

Audit log fields:

```text
EntityName
Action
PerformedBy
PerformedAt
Changes
CreatedAt
UpdatedAt
```

Current limitation:

```text
PerformedBy is currently set to System because authentication is not implemented yet.
```

Future audit log improvements:

1. Log order creation.
2. Log order status changes.
3. Log shipment creation.
4. Log shipment status changes.
5. Log incident creation.
6. Log incident closing.
7. Store the authenticated user id when login is implemented.
8. Restrict audit log access to Admin and Manager roles.

## OWASP: Error Handling

Status:

```text
Partly implemented
```

Current controllers catch expected validation and business rule errors.

Examples:

1. Invalid input returns 400 Bad Request.
2. Business rule conflicts return 409 Conflict.
3. Missing records return 404 Not Found.

Examples of safe responses:

```text
Product was not found.
Customer email must be valid.
This order already has a shipment.
Shipment status is invalid.
```

Current limitation:

```text
Unexpected errors can still show developer exception details in development.
```

Future improvement:

```text
Add global exception middleware before production style use.
```

The global exception middleware should hide stack traces from users.

## OWASP: Secrets Management

Status:

```text
Partly implemented
```

Current local development uses appsettings.json for the SQL Server LocalDB connection string.

No production secrets should be committed to GitHub.

Future Docker configuration should use environment variables for:

1. Database connection string
2. JWT secret
3. Admin seed password
4. Other sensitive settings

Rule:

```text
Do not hardcode real secrets in source code.
```

## Summary

The current backend already demonstrates several security aware choices:

1. Layered architecture
2. EF Core instead of raw SQL
3. Backend validation
4. Business rules in services
5. Audit logging for important product changes
6. Clean Git tracking
7. No committed bin or obj folders

The next security critical step is authentication and role based authorization.
