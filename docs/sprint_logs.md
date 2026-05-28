# Sprint Logs

## Sprint 1

### Sprint goal
Create the initial project structure and backend foundation for WarehouseOps.

### Planned work
- Create repository structure.
- Create backend solution.
- Add Domain entities and enums.
- Configure Entity Framework Core.
- Create ApplicationDbContext.
- Create initial database migration.

### Completed work
- Created the WarehouseOps project structure.
- Created backend solution with Api, Application, Domain, Infrastructure and Tests projects.
- Added Domain entities for products, inventory, customers, orders, shipments, incidents and audit logs.
- Added enums for order status, shipment status, incident status and user roles.
- Added Entity Framework Core packages.
- Created ApplicationDbContext.
- Configured SQL Server connection string.
- Created and applied the initial database migration.
- Verified that the database WarehouseOpsDb was created successfully.

### Problems
- Git was not installed at the start and had to be installed.
- bin and obj folders were accidentally committed, then removed from Git and blocked with .gitignore.
- EF Core migration first showed decimal precision warnings for money related fields.
- Decimal precision was fixed with HasPrecision(18, 2) in ApplicationDbContext.

### What to improve next sprint
- Start building the Product API.
- Keep commits smaller and check git status before every commit.
- Continue documenting completed work after each working step.
