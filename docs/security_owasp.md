# WarehouseOps: Security and OWASP

**Last reviewed:** 2 November 2026
**Scope:** Local development and portfolio demonstration, not a production security certification.

This document separates security measures implemented in code, controls exercised by automated or manual tests, and tests or mitigations still planned. OWASP category references below use the **OWASP Top 10: 2021** classification.

## Security objectives

- Authenticate API requests and authorize operations by role in the backend.
- Store password hashes through a standard identity framework, not plain-text passwords.
- Keep application secrets out of source control.
- Use safe database queries and validate input at trust boundaries.
- Avoid exposing internal exception details through HTTP responses.
- Record important business operations in audit history.
- Limit attack surface in the local Docker environment.
- Verify important security behavior with regression tests.

## Authentication and Identity

`POST /api/Auth/login` accepts an email address and password. The backend uses `IAuthUserAuthenticator` and an Infrastructure implementation based on `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`.

ASP.NET Core Identity stores users, roles, credentials, lockout state and related records in SQL Server using the standard `AspNet*` tables. Password storage is handled by Identity's password hasher. Passwords are never stored as plain text in the business model.

Configured Identity options in `WarehouseOps.Api/Program.cs` include:

- Unique email addresses.
- Confirmed email required for sign-in.
- Passwords at least 12 characters long, requiring uppercase, lowercase, digits and a non-alphanumeric character.
- Account lockout after five failed password attempts, for 15 minutes.
- Login endpoint rate limiting of five requests per client IP per minute, with no queue.

**Verification status:** Successful real SQL Server logins were confirmed for `Admin`, `WarehouseStaff` and `Manager`. The rate-limiter has automated HTTP test coverage. The Identity account-lockout threshold itself has **not yet been separately verified against SQL Server**.

### Development demo identities

`DemoIdentitySeeder` creates local demonstration identities using password values supplied through configuration. It runs only when the host environment is `Development` and `Database:SeedDemoUsers` is enabled. The three password values are supplied by Docker environment variables derived from the ignored local `.env` file:

| Account | Configuration key in `.env` |
|---|---|
| Admin | `DEMO_ADMIN_PASSWORD` |
| WarehouseStaff | `DEMO_STAFF_PASSWORD` |
| Manager | `DEMO_MANAGER_PASSWORD` |

The frontend exposes demo **email addresses**, not passwords. The seeder checks existing users and does not silently replace their passwords or assign an unexpected role. Demo account provisioning is for the development environment, not production.

## JWT handling

The application issues a signed JWT after successful Identity authentication. The JWT bearer middleware validates issuer, audience, signature and lifetime, and controllers enforce role claims. An invalid token produces an HTTP `401`; a valid token without sufficient permissions produces `403`.

The React application currently saves the login state and JWT in browser `localStorage`. Logout removes that local state and the interface returns to the login screen. This **does not invalidate the JWT on the server**; any previously copied token can remain usable until its expiry. Because `localStorage` can be read by JavaScript executing on the page, a future XSS vulnerability could expose the token. A production-ready design needs a separate assessment of secure session storage, HTTPS, revocation and token rotation.

JWT checks currently covered by automated HTTP tests include invalid signing key, issuer, audience, expired token, not-yet-valid token, unsigned token and missing role claim.

## Authorization and OWASP A01: Broken Access Control

Authorization is defined in the backend, principally with ASP.NET Core role authorization attributes. Frontend role-based rendering is a usability feature and is **not** treated as the security boundary.

| Permission | Admin | WarehouseStaff | Manager |
|---|:---:|:---:|:---:|
| Read products and business data | Yes | Yes | Yes |
| Write products | Yes | No | No |
| Manage warehouse operations | Yes | Yes | No |
| View audit history | Yes | No | Yes |

Verified checks include:

- Anonymous read of products returns `401`.
- Invalid JWT for a protected operation returns `401`.
- WarehouseStaff access to audit logs returns `403`.
- Manager and WarehouseStaff attempts to delete a product return `403`.
- Admin deletion of a deliberately nonexistent product reaches the endpoint and returns `404`.
- Admin and Manager can read audit history; all three roles can read products.

The delete test used a newly generated GUID, so it did not intentionally remove data. Automated HTTP integration tests also check that unauthorized service methods are not executed.

**Open work:** Broader resource ownership checks and IDOR/BOLA testing on individual entities and nested API routes.

## OWASP A02: Cryptographic Failures

- Password hashing is delegated to ASP.NET Core Identity.
- The JWT signing secret is supplied from user secrets or environment configuration and validated for minimum length at startup.
- `.env` is ignored by Git; `.env.example` contains only placeholders.
- Docker restricts the app's exposed ports to localhost.

**Limitations:** The local Docker setup uses HTTP; production deployments would require TLS termination and appropriate security headers. ASP.NET Core Data Protection emitted warnings because keys are stored inside the ephemeral API container and are not encrypted at rest. Those settings need dedicated production configuration if Data Protection-dependent functionality is introduced.

## OWASP A03: Injection

The application uses Entity Framework Core repositories and LINQ for normal database operations, rather than constructing raw SQL with user-supplied values. This reduces SQL Injection risk, but code structure alone is **not equivalent to completed adversarial testing**.

**Status: planned, not yet completed.** The next dedicated security-testing phase will:

1. Review all repository code for raw SQL execution and unsafe dynamic query building.
2. Exercise input paths such as product search, categories, customer lookups and other query parameters with malicious-looking values.
3. Verify that input is handled as data, SQL errors are not disclosed, and unauthorized records are not returned.
4. Verify that existing database records and tables remain unchanged and add suitable regression tests.
5. Conduct the tests only in an isolated local test environment with disposable data or a backup.

Example input strings for that future local test phase include `' OR '1'='1` and `'; DROP TABLE Products;--`. These strings are listed as **test cases**, not evidence that a test has already passed.

## OWASP A04: Insecure Design

Business rules are implemented in application services rather than controllers. Repositories isolate persistence. Domain entities and enums represent business concepts. Tests cover invalid order states, insufficient stock, duplicate records, terminal shipment/incident statuses and audit changes.

The transaction decorators in the Application layer manage related business mutations and help avoid partially completed operations.

**Open work:** Threat modelling and a systematic review of concurrent operations and cross-entity invariants.

## OWASP A05: Security Misconfiguration

Current local-development configuration includes:

- Swagger only in Development.
- A startup check for the JWT secret length.
- SQL Server available only on the internal Docker network, without a host port mapping.
- API bound to `127.0.0.1:5059`; frontend bound to `127.0.0.1:3000`.
- Authentication and authorization middleware enabled for protected endpoints.
- Environment-based credentials rather than hardcoded secrets.

**Open work:** Deployment TLS, Data Protection key persistence and encryption, HTTP security headers, reverse-proxy configuration, production secret manager and environment-specific hardening.

## OWASP A07: Identification and Authentication Failures

ASP.NET Core Identity is implemented rather than a static list of demo password hashes. There are multiple defenses on the login endpoint: Identity password verification, account lockout configuration and per-IP rate limiting.

**Verified:** Correct logins for all three roles, invalid credential rejection, login request rate limit and JWT token validation tests. **To verify:** Lockout duration, account recovery, interactions between rate limiting and lockout, and server-side token invalidation or shortened session lifetime.

## OWASP A09: Security Logging and Monitoring Failures

The application stores audit history for important operational changes. Audit entries include the entity, action, actor, time and change description. Backend access to audit history is limited to `Admin` and `Manager`. Audit records support traceability but are not a complete intrusion-detection or security-monitoring system.

**Open work:** Dedicated authentication/security event logging, centralized log retention, correlation and alerting without exposing secrets or full tokens.

## Input validation and error handling

The React frontend uses Zod and React Hook Form for client-side validation. Backend services independently validate domain inputs, including required fields, enum validity, non-negative inventory values and order status constraints.

A global exception-handling middleware produces safe responses for unexpected exceptions, while retaining server-side diagnostics. The HTTP surface should not disclose SQL query text, connection credentials or stack traces.

**Open work:** Negative tests for oversized inputs, encoding edge cases, malformed JSON, attempted mass assignment and technical exception disclosure across all endpoint groups.

## Security test evidence

| Control | Method | Latest confirmed result |
|---|---|---|
| Service rules, HTTP authentication/authorization, JWT validation and model checks | `dotnet test WarehouseOps.sln` | 223 passed, 0 failed, 0 skipped |
| Development Identity login via SQL Server | Manual local HTTP calls | Admin, WarehouseStaff, Manager succeeded |
| Anonymous product read | Manual local HTTP call | `401` |
| Malformed JWT | Manual local HTTP call | `401` |
| Restricted product delete | Manual local HTTP calls | WarehouseStaff `403`, Manager `403` |
| Admin delete with nonexistent GUID | Manual local HTTP call | `404` |
| Audit-history read policy | Manual local HTTP calls | Admin/Manager `200`, WarehouseStaff `403` |
| Role-specific browser UI and logout | Manual browser checks | Verified in local Docker frontend |
| SQL Injection penetration tests | Planned | Not yet executed |
| Real-database lockout tests | Planned | Not yet executed |

All confirmed results above were obtained for the local portfolio implementation by 2 November 2026. The automated HTTP suite generally uses mocks and is not a substitute for a full database-backed end-to-end security test.

## Remaining security priorities

1. Verify Identity lockout and rate-limiting edge cases.
2. Test SQL Injection and unsafe input handling against a local disposable dataset.
3. Expand access-control tests for object-level permissions and mass assignment.
4. Test unexpected-error responses for sensitive information disclosure.
5. Review JWT storage, expiry and revocation strategy.
6. Harden configuration for any deployment beyond localhost.

The guiding principle remains: **authorization belongs in the backend, and security claims require test evidence.**
