# Security and OWASP

This document describes how WarehouseOps applies security aware development practices and how the implementation relates to common OWASP risks.

WarehouseOps is a portfolio project, but the security decisions are implemented in practical code rather than only described in theory.

## Security goals

The main security goals are:

* Protect backend endpoints with authentication.
* Enforce role based authorization in the backend.
* Avoid exposing sensitive technical errors to users.
* Avoid storing secrets in source control.
* Avoid storing passwords in plain text.
* Validate input in both frontend and backend.
* Avoid raw SQL with user input.
* Keep audit logs for important business changes.
* Limit exposed ports in Docker Compose.

## Authentication

WarehouseOps uses JWT based authentication.

The login endpoint is:

POST /api/Auth/login

When login succeeds, the backend returns:

* JWT token
* Expiration time
* Email
* Display name
* Role

The frontend sends the token in later requests using:

Authorization: Bearer token

The backend validates:

* Token issuer
* Token audience
* Signing key
* Expiration time
* Role claims

## Demo users

The current version uses demo users for portfolio and local development.

Demo users exist for:

* Admin
* WarehouseStaff
* Manager

Passwords are not stored as plain text. They are stored as PBKDF2 hashes.

For a production system, demo users should be replaced with ASP.NET Identity or another production ready identity provider.

## Password handling

Passwords are verified using PBKDF2 based hashing.

The stored password format contains:

* Algorithm marker
* Iteration count
* Salt
* Hash

The password verification uses fixed time comparison to reduce timing based comparison risk.

Plain text passwords are not stored.

## Authorization

Authorization is enforced in backend controllers using role based authorization.

This is important because frontend checks alone are not secure.

Examples:

* Manager can read products, orders, shipments, incidents and audit logs.
* Manager cannot create or update operational records.
* WarehouseStaff can manage warehouse operations.
* WarehouseStaff cannot view audit logs.
* Only Admin can create, update and delete products directly.

The frontend also hides actions that the current user is not allowed to perform, but backend authorization is the real security boundary.

## OWASP A01 Broken Access Control

Broken access control is handled by requiring authentication and checking roles in backend endpoints.

Examples of protected behavior:

* Manager receives 403 Forbidden when trying to create incidents.
* Manager cannot create orders, update shipments or change inventory.
* WarehouseStaff cannot access audit logs.
* Product write operations are limited to Admin.

Frontend role based rendering improves usability, but the backend still blocks unauthorized requests.

## OWASP A02 Cryptographic Failures

WarehouseOps avoids plain text password storage.

Security related choices:

* Passwords are stored as PBKDF2 hashes.
* JWT secret is not stored in appsettings.json.
* JWT secret is loaded from user secrets locally.
* JWT secret is loaded from environment variables in Docker.
* .env is ignored by Git.
* .env.example shows required variables without being the real secret store.

For production, secret management should be moved to a dedicated secret manager.

## OWASP A03 Injection

WarehouseOps uses Entity Framework Core for database access.

The project does not build raw SQL strings from user input.

Input is passed through service methods and EF Core queries instead of manually concatenated SQL.

This reduces SQL injection risk.

## OWASP A04 Insecure Design

WarehouseOps separates responsibilities across layers.

Design choices:

* Controllers handle HTTP only.
* Services contain business rules.
* Repositories contain persistence logic.
* Domain contains entities and enums.
* Tests verify important business rules.

Examples of business rules:

* Orders cannot reserve more stock than available.
* Products must exist in inventory before they can be ordered.
* Cancelled orders cannot receive shipments.
* Closed incidents cannot be reopened.
* Resolution notes are required when closing incidents.

## OWASP A05 Security Misconfiguration

WarehouseOps includes configuration choices to reduce misconfiguration risk.

Implemented choices:

* Docker Compose binds frontend and backend to 127.0.0.1.
* SQL Server is not exposed to the host machine in Docker Compose.
* Swagger is only enabled in Development.
* JWT secret length is checked during startup.
* Global exception handling is registered in the API pipeline.

Docker access:

| Service | Exposure |
|---|---|
| frontend | 127.0.0.1:3000 |
| backend | 127.0.0.1:5059 |
| sqlserver | Internal Docker network only |

## OWASP A07 Identification and Authentication Failures

WarehouseOps uses token based authentication and role claims.

Implemented choices:

* Users must log in before accessing protected endpoints.
* Invalid credentials return an unauthorized response.
* JWT tokens expire.
* Role claims are included in the token and checked by the backend.

Current limitation:

* Demo users are hardcoded for local development.
* There is no refresh token flow.
* Token storage is suitable for the portfolio version, not a production identity architecture.

## OWASP A09 Security Logging and Monitoring Failures

WarehouseOps includes audit logging for important business changes.

Audit logs store:

* Entity name
* Action
* Performed by
* Performed at
* Change description

Audit logs use the currently authenticated user when available.

Examples:

* Product created by Admin
* Inventory updated by WarehouseStaff
* Order status changed
* Shipment status changed
* Incident closed

This supports traceability and makes it possible to see who changed what and when.

## Global exception handling

The API includes global exception handling middleware.

The purpose is to avoid returning internal technical details to users.

Unexpected exceptions are logged server side and returned as a safe error response.

The response includes:

* Title
* Safe message
* Status code
* Trace id

It does not return SQL errors or stack traces to the user.

## Input validation

WarehouseOps validates input in both frontend and backend.

Frontend validation uses:

* Zod
* React Hook Form

Backend validation is implemented in application services.

Examples:

* Product name is required.
* SKU is required.
* Price cannot be negative.
* Inventory quantity cannot be negative.
* Orders must contain at least one item.
* Incident title and description are required.
* Resolution notes are required when closing an incident.

Frontend validation improves user experience. Backend validation protects the system.

## Secrets

Secrets are not committed to the repository.

Local development uses user secrets for the JWT secret.

Docker uses environment variables from .env.

The repository contains .env.example to show which variables are needed.

The real .env file is ignored by Git.

## Audit log access

Audit logs are sensitive because they show operational activity.

Access is limited to:

* Admin
* Manager

WarehouseStaff users create audit log entries through their actions, but they cannot view the audit log page.

## Docker security choices

The Docker Compose setup is intentionally limited to local access.

Security choices:

* Backend is bound to 127.0.0.1.
* Frontend is bound to 127.0.0.1.
* SQL Server has no public port mapping.
* SQL Server is only reachable by backend inside the Docker network.
* Environment variables are used for local Docker secrets.
* Demo data uses fictional company data.

## Current security limitations

The following items are known limitations and possible future improvements:

* Replace demo users with ASP.NET Identity.
* Add refresh token support.
* Add stricter database uniqueness constraints.
* Add rate limiting to login.
* Add integration tests for authorization rules.
* Add centralized structured logging.
* Use a production secret manager for deployment.
* Improve frontend token storage for a production environment.

## Summary

WarehouseOps demonstrates security aware development through practical implementation:

* Authenticated API access
* Backend role based authorization
* Password hashing
* Environment based secrets
* Safe error handling
* EF Core based persistence
* Input validation
* Audit logging
* Local only Docker exposure

The main security principle is that protected actions must be enforced by the backend, not only hidden in the frontend.