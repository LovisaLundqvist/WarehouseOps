# WarehouseOps: Testing

**Last verified:** 2 November 2026
**Latest backend result:** **223 tests passed, 0 failed, 0 skipped**.

This document describes the automated and manual testing performed so far, explains which infrastructure the tests exercise, and distinguishes tested behavior from planned improvements.

## Tools and environments

- **xUnit:** .NET test framework.
- **Moq:** mocks for service and repository dependencies.
- **FluentAssertions:** readable assertions.
- **ASP.NET Core `WebApplicationFactory`:** HTTP-level integration tests for authentication, authorization and JWT behavior.
- **Docker Compose:** local manual integration against SQL Server, ASP.NET Core API and the built React frontend.
- **TypeScript compiler and Vite:** production frontend build checks.

The test suite is in `backend/WarehouseOps.Tests`. Local Docker smoke tests are conducted separately from the unit and HTTP test suite.

## Run backend tests

From the repository root:

```powershell
cd backend
dotnet restore WarehouseOps.sln
dotnet build WarehouseOps.sln --no-restore
dotnet test WarehouseOps.sln
```

**Verified result (2 November 2026):** `223` total, `223` succeeded, `0` failed, `0` skipped. This result is a snapshot of the current project, not a permanent fixed test count.

## Run frontend build checks

```powershell
cd frontend/warehouseops-client
npm install
npm run build
```

The latest local frontend build succeeded. The Vite bundler produced a non-blocking warning about a JavaScript chunk exceeding 500 kB. Docker also successfully built the frontend image and ran it using Nginx.

## Test categories

### Service-level business tests

Tests cover:

- **Products:** required fields, trimming, non-negative prices, duplicate SKUs, deletion restrictions, audit records.
- **Inventory:** product references, non-negative quantities, duplicate inventory records, low-stock mapping, concurrent updates and audit records.
- **Customers:** creation and update validation, security-relevant field handling and audit behavior.
- **Orders:** valid customers and order items, duplicate products, available stock, reservations, cancellation restoration, invalid state transitions, audit privacy and transactional behavior.
- **Shipments:** linked orders, tracking data, uniqueness, invalid shipment states, delivered timestamps and audit records.
- **Incidents:** required title and description, valid severity/entity type, state transitions, closure requirements, status filtering and audit records.
- **Transactions:** tests for decorators that coordinate persistence behavior across services.

Many service-level tests replace repository dependencies with mocks. They verify application rules but do not guarantee the same outcome under every SQL Server concurrency scenario.

### HTTP security integration tests

`Integration/SecurityHttpTests.cs` starts the API with `WebApplicationFactory`, uses test configuration and mocks selected application services, then sends real HTTP requests through middleware, routing and authorization.

Examples of tested behavior:

- Anonymous requests to protected endpoints receive `401 Unauthorized`.
- Missing or incorrect role authorization results in `403 Forbidden`.
- Admin can create a product, while Manager and WarehouseStaff cannot.
- WarehouseStaff cannot access audit history.
- The login endpoint rejects invalid credentials and rate-limits excessive attempts.
- Service methods are not invoked when middleware denies the request.

These HTTP tests **do not automatically connect to the Docker SQL Server**. Migrations and demo seeding are disabled in their test host.

### JWT validation tests

`Integration/JwtValidationHttpTests.cs` checks that the JWT middleware rejects tokens with:

- Incorrect issuer.
- Incorrect audience.
- Incorrect signing key.
- Expired lifetime.
- A validity window that begins in the future.
- No digital signature.
- No required role claim, which produces `403` for an otherwise valid token on a role-protected endpoint.

A correctly signed token with the expected role is accepted by the protected test endpoint.

### Identity and authentication tests

- `Integration/IdentityModelTests.cs` confirms that EF Core maps Identity users/roles, uses GUID keys, applies the `DisplayName` length limit and preserves business-entity mappings.
- `Services/AuthServiceTests.cs` verifies successful JWT issuance after successful authentication, rejection of invalid credentials and rejection of missing email/password input before Identity lookup.

Identity password verification and user provisioning were additionally tested through the **real local SQL Server API** using the Docker environment.

## Manual full-stack verification

The following checks were performed with the running local Docker setup:

| Check | Expected behavior | Observed result |
|---|---|---|
| SQL Server container | Healthy | Passed |
| API startup and Swagger | API available | Swagger returned `200` |
| Development Identity seeding | Three roles/users configured | Seed log completed successfully |
| Admin login | JWT with `Admin` role | Passed |
| WarehouseStaff login | JWT with `WarehouseStaff` role | Passed |
| Manager login | JWT with `Manager` role | Passed |
| Anonymous `GET /api/Products` | `401` | Passed |
| Invalid JWT for product delete | `401` | Passed |
| Admin `GET /api/AuditLogs` | `200` | Passed |
| WarehouseStaff `GET /api/AuditLogs` | `403` | Passed |
| Manager `GET /api/AuditLogs` | `200` | Passed |
| Admin deletes nonexistent product | `404` | Passed |
| WarehouseStaff/Manager delete product | `403` | Passed |
| React dashboard | Operational figures render | Passed |
| React role-specific navigation | Audit history hidden from WarehouseStaff | Passed |
| React logout | Returns to login; can log in again | Passed |

The deliberate delete test used a random nonexistent GUID to avoid modifying products. These results are point-in-time checks, not continuous automated monitoring.

## Important test constraints

- A successful mock-based HTTP integration test verifies middleware and API behavior but not the real database's query performance, constraints or concurrency.
- The Identity lockout threshold has been configured but has not yet undergone a dedicated real-database security regression test.
- SQL Injection testing with adversarial payloads has been planned but not yet executed. EF Core's use of parameterized queries reduces risk; this does not by itself prove complete protection.
- No comprehensive browser end-to-end test suite is currently automated.
- React UI screenshots document visual state, not exhaustive functional correctness.

## GitHub Actions

`.github/workflows/ci.yml` runs on push and pull requests to `main`. The current workflow:

1. Restores .NET packages.
2. Builds the backend.
3. Executes backend tests.
4. Installs frontend dependencies.
5. Builds the frontend.

The workflow does **not** currently run a real SQL Server service, penetration tests or Docker image-build validations.

## Next testing work

1. Verify Identity account lockout and release using an isolated test identity.
2. Add SQL Injection and unsafe input regression checks to appropriate API routes.
3. Add object-level access and mass-assignment tests.
4. Add tests checking that database exceptions and technical details are not returned to clients.
5. Add database-backed integration tests for migrations, relationships and concurrent order/inventory updates.
6. Add automated frontend component and end-to-end tests if scope permits.

Test results and remaining limitations should be updated after each substantial security or feature milestone.
