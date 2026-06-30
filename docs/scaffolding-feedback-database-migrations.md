# Scaffolding Feedback: Database Migrations

## Purpose

Add durable scaffold guidance for EF Core database migrations in apps with multiple hosted services, scaled runtime instances, Aspire local orchestration, Azure deployment, and third-party SQL-backed libraries.

## Core Guidance To Add

### One migration owner

Scaffolded solutions should have exactly one migration owner process, for example `{App}.DatabaseMigrator`.

Runtime hosts must not call `Database.MigrateAsync`, create schemas, patch tables, or generate deployment scripts during startup. This applies to API hosts, Functions, schedulers, workers, and any scaled-out runtime process.

Reason: scaled-out Azure apps can start multiple instances at once. Runtime migrations create race conditions, broaden runtime identity permissions, and make startup failure modes harder to control.

Scaffold details:

- add a host project under `src/Host/{App}.DatabaseMigrator`
- include it in the solution
- include a Dockerfile if the app deploys containers
- wire it into Aspire AppHost
- run it once in Azure deployment before runtime rollout
- make it log each target before and after migration
- let unhandled migration failures terminate the process

### Reusable migration support belongs in the EF data package

If the scaffold already has an EF data package, put shared migration runner primitives there rather than creating a separate package by default.

Recommended primitives:

- `IDatabaseMigrationTarget`
- `DatabaseMigrationRunner`
- EF Core target registration by logical name, order, and `DbContext`
- ordered migration steps for custom data work before and after schema migration
- fail-fast behavior with nonzero process exit on first failed target

Avoid single-app package sprawl. Start local in the EF data package, then extract to a feed package only after the contract proves stable.

### Support data transformations, not only schema diffs

Migration infrastructure should treat migrations as executable deployment units. Schema changes often need data movement, staging, backfills, relation rebuilding, or compatibility shims.

Add first-class support for:

- pre-schema steps
- EF schema migration
- post-schema steps
- long command timeout only in the migrator process
- deterministic target ordering
- fail-fast execution

If temp tables must carry data from pre-schema to post-schema steps, the runner must own one open database connection for the full target. If that is not guaranteed, use durable staging tables in a migration schema and drop them after the migration completes.

Scaffold should expose at least two extension points:

- C# migration steps for app-specific logic that needs EF services or strongly typed context access
- SQL migration steps for staging, backfill, compatibility copies, or cleanup scripts

Set long SQL command timeouts only on migrator DbContext registrations. Do not copy migration timeout defaults into API, Scheduler, Functions, or request-path services.

### Keep logical connection names stable

Use separate logical connection string names even when local Aspire points them to one physical database.

Example shape:

- `{App}DbContextTrxn`
- `{App}DbContextQuery`
- `{App}FlowEngineDbContext`
- `{ThirdPartyStore}DbContext`

Local Aspire can map these to one SQL database for developer simplicity. Azure can later split them into separate databases by changing infrastructure and connection strings, not runtime code.

Scaffold appsettings should include every logical connection name in each host that needs it. Missing names should fail fast in hosts that require that store.

### Multiple schemas in one local database

When local topology uses one SQL resource, keep logical stores separated by schema and migration history table.

Recommended pattern:

- app domain schema: app-owned schema, app-owned history table
- workflow or FlowEngine schema: dedicated schema and history table
- scheduler or third-party schema: dedicated schema and history table

Each EF context should configure its own migration history table and schema.

Design-time factories must match runtime migration configuration. This matters for:

- schema names
- migration history tables
- explicit migrations assembly
- provider choice
- SQL compatibility level

Add or update EF design-time factories when adding a logical migration target.

### Third-party SQL stores need app-owned migration context

For libraries such as TickerQ that support EF Core persistence, scaffold an app-owned context and migration set instead of letting runtime startup create or patch schema.

Recommended TickerQ shape:

- `{App}TickerQDbContext`
- schema `Scheduler`
- history table `Scheduler.__EFMigrationsHistory_TickerQ`
- migrations under `Migrations/TickerQ`
- explicit migrations assembly
- scheduler startup validates required tables only

Do not let the scheduler call auto-create or deployment-script features at startup.

TickerQ-specific lesson:

- current upstream migration tooling has had context discovery failures in new projects
- scaffold should prefer an explicit app-owned context name over relying on a library-provided context name
- prove `dotnet ef migrations add` and real SQL apply before treating the integration as done
- keep generated migrations under a separate folder so third-party operational schema does not mix with app domain migrations

### Aspire local orchestration

For local Aspire:

- add a database migrator project to AppHost
- reference the same local SQL database with all logical connection names
- make runtime hosts wait for migrator completion
- do not start API, Scheduler, Functions, or workers until migrations complete

This keeps the local loop simple while matching production release order.

Scaffold should preserve simple local topology:

- one SQL resource
- one physical database
- multiple schemas
- multiple logical connection string names pointing to that database
- runtime hosts waiting on migrator completion

Do not require local developers to run multiple SQL containers just because production may split databases later.

### Azure deployment order

For Azure:

1. Deploy infrastructure.
2. Run the migrator once as a pipeline step or Container Apps Job.
3. Deploy or start runtime apps.

Runtime apps should not need DDL permissions. The migrator identity should have schema DDL plus migration-history permissions. Runtime identities should get least-privilege DML only.

Important auth gap for scaffold docs: Azure SQL Entra auth connection strings do not create contained database users or grants. Scaffolded infra should either implement this SQL data-plane step or leave a clear comment where SQL identities are wired.

Container Apps Job guidance:

- manual trigger
- parallelism `1`
- replica completion count `1`
- retry limit `0` unless a migration is explicitly restart-safe
- timeout long enough for data movement
- pipeline polls the concrete job execution until terminal status

GitHub Actions or equivalent pipeline should gate runtime deployment on migration success.

For non-container hosting, equivalent guidance still applies: run a single migrator process before runtime rollout.

### Runtime host changes

API, Functions, Scheduler, workers, and any other runtime host should:

- register DbContexts for normal domain work only
- never call EF schema migration APIs at startup
- never create schemas or tables at startup
- fail fast when a required connection string is missing
- validate required third-party operational schema if startup depends on it
- run with no DDL permission in Azure

Scheduler-specific guidance:

- scheduler uses its own logical operational-store connection string
- scheduler uses app domain connection strings only for domain work
- scheduler can validate required third-party tables at startup
- scheduler must not auto-create third-party schema in production startup

### Deployment configuration

Scaffolded configuration should separate:

- transactional DbContext connection
- query DbContext connection
- workflow engine connection
- third-party scheduler connection

Development config may point all names to the same local database. Production config may point names to the same database or split databases later.

Infrastructure should output or wire all logical connection strings explicitly. Do not infer one logical store from another at runtime except as a deliberate local fallback in migrator-only registration.

### Tests to scaffold

Add tests proportional to the blast radius:

- unit tests for migration target ordering
- unit tests for fail-fast behavior
- unit tests proving later targets do not run after failure
- SQL integration test applying all targets to a fresh database
- SQL integration test running migrator twice for idempotency
- SQL integration test proving third-party schema validation fails when schema is missing
- SQL integration test proving custom pre/post data migration steps can move data
- Aspire topology test proving runtime hosts depend on migrator completion

Tests that require SQL containers should report `Assert.Inconclusive(...)` when the container cannot start. They should not silently pass.

Also update existing integration tests that used to rely on runtime startup migrations. Those tests must prepare their database explicitly before booting runtime hosts.

Useful test helpers:

- create isolated database per test
- create each logical DbContext with the same migration history settings as production migrator
- build a migration runner directly in integration tests
- assert idempotency by running the migrator twice
- assert missing third-party schema validation returns false or throws the expected startup failure

### Generated artifacts and project wiring

When scaffold adds this pattern, include:

- migrator project in the solution
- Dockerfile for migrator if app deploys containers
- AppHost project reference to migrator
- appsettings entries for every logical connection string
- design-time context factory for every migration target
- third-party migration folder when relevant
- Azure infra module for one-shot migration job when using Container Apps
- deployment workflow step that runs migrator before runtime rollout

## Anti-Patterns To Call Out

- `Database.MigrateAsync` in API startup
- `Database.MigrateAsync` in Functions startup
- scheduler-owned schema creation
- third-party `AutoCreateSchema` enabled in production startup
- runtime identities with DDL permissions
- one physical database name hardcoded into runtime code
- shared migration history table across unrelated contexts
- broad catch-and-ignore around migration failures
- retry loops as a substitute for fixing migration ownership
- tests that pass because startup silently skipped unavailable SQL
- one appsettings connection string reused implicitly by unrelated stores
- custom data transformations hidden inside runtime hosted services

## Reference-App Evidence

TaskFlow implementation points that can inform scaffold updates:

- migrator host: `src/Host/TaskFlow.DatabaseMigrator`
- reusable migration support: `src/Infrastructure/TaskFlow.Infrastructure.Data/MigrationSupport`
- TickerQ EF context: `src/Infrastructure/TaskFlow.Infrastructure.Data/TaskFlowTickerQDbContext.cs`
- TickerQ schema validator: `src/Infrastructure/TaskFlow.Infrastructure.Data/TaskFlowTickerQSchemaValidator.cs`
- TickerQ migrations: `src/Infrastructure/TaskFlow.Infrastructure.Data/Migrations/TickerQ`
- EF design-time wiring: `src/Infrastructure/TaskFlow.Infrastructure.Data/DesignTimeDbContextFactory.cs`
- bootstrapper runtime migration removal: `src/Host/TaskFlow.Bootstrapper/RegisterServices.cs`
- DbContext registration split: `src/Host/TaskFlow.Bootstrapper/Registration/RegisterServices.Database.cs`
- scheduler operational-store validation: `src/Host/TaskFlow.Scheduler/RegisterSchedulerServices.cs`
- Aspire wiring: `src/Host/Aspire/AppHost/AppHost.cs`
- Azure job wiring: `infra/main.bicep`, `infra/modules/container-app-job.bicep`
- Azure SQL auth-gap comments: `infra/main.bicep`, `infra/modules/sql-database.bicep`
- deployment ordering: `.github/workflows/deploy.yml`
- migration tests: `src/Test/Test.Unit/Infrastructure/DatabaseMigrationRunnerTests.cs`, `src/Test/Test.Integration/DatabaseMigratorIntegrationTests.cs`
- integration test prep after runtime migrations removed: `src/Test/Test.Integration/AiWorkflowIntegrationTests.cs`
- Aspire topology test: `src/Test/Test.Aspire/AppHostMigratorTopologyTests.cs`

## Suggested Scaffold Instruction Text

When an app uses EF Core and more than one runtime host can touch the same database, generate a dedicated database migrator host. Runtime hosts must not mutate schema at startup. Register each logical database or schema as an ordered migration target. The migrator must fail fast and exit nonzero on the first target failure.

When a migration can require data transformation, support ordered pre-schema and post-schema migration steps. Prefer EF migrations for schema-coupled data work. Use pre/post steps for deployment-owned staging, backfills, compatibility copies, or cleanup that does not belong in runtime startup.

When using Aspire locally, model logical stores with separate connection string names even if they point to one physical SQL database. Runtime projects must wait for migrator completion.

When deploying to Azure, run the migrator once before rolling out scaled runtime apps. Grant DDL only to the migrator identity. Grant runtime identities DML only. Do not assume an Entra auth SQL connection string creates database users or grants.

When adding a third-party SQL-backed library, create an app-owned EF migration context for that library's operational schema. Use explicit schema, migration history table, migrations folder, and migrations assembly. Runtime startup may validate the schema but must not create or patch it.

When removing runtime migrations from an existing app, update tests that previously relied on app startup to create their databases. Test setup should run the migrator or direct EF migrations before constructing the runtime host.
