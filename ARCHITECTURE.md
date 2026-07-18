# Backend Testing Studio - Architecture

## 1. Purpose

Backend Testing Studio is a declarative platform to design, execute, validate, and report backend API tests without hard-coding per-API logic into the engine.

The product follows Clean Architecture and a strict plugin-driven model:

1. The UI never talks directly to infrastructure implementations.
2. The Core defines the application rules and public contracts.
3. Infrastructure projects implement those contracts.
4. Plugins describe APIs declaratively through JSON manifests and content files.

## 2. Architecture Summary

The system is divided into six logical concerns:

1. Presentation.
2. Application and domain rules.
3. HTTP execution.
4. Local persistence.
5. Scenario and assertion execution.
6. Plugin discovery and indexing.

The UI is a Blazor Server application that acts as the composition root and the only user-facing entry point. It delegates all business operations to Core services.

## 3. Projects and Responsibilities

### BackendTestingStudio.UI

Responsible for the user interface, routing, layout, forms, state display, and user interactions.

Rules:

1. No direct access to `HttpClient`.
2. No direct access to SQLite.
3. No business rules in components.
4. All actions go through application services exposed by Core.
5. UI components are thin and presentational.

### BackendTestingStudio.Core

Responsible for the application contract and the main orchestration rules.

Contains:

1. Use cases.
2. Domain entities and value objects.
3. Interfaces for HTTP, storage, plugin discovery, scenario execution, assertion evaluation, and reporting.
4. Execution models.
5. Validation rules that are independent from infrastructure.

Core is the source of truth for public contracts. Infrastructure projects implement the interfaces defined here.

### BackendTestingStudio.Http

Responsible for outbound HTTP execution.

Contains:

1. `IHttpEngine` implementation.
2. Request construction and serialization.
3. Response deserialization.
4. Authentication handlers.
5. Retry and timeout policies.
6. Request/response logging and correlation.

### BackendTestingStudio.Storage

Responsible for SQLite persistence.

Contains:

1. Repository implementations.
2. Migrations and schema management.
3. Local run history.
4. Cached plugin indexes.
5. Persisted environment, variable, and report snapshots.

### BackendTestingStudio.Scenarios

Responsible for interpreting and executing scenario definitions.

Contains:

1. Scenario parser and validator.
2. Scenario execution engine.
3. Step resolution.
4. Variable flow between steps.
5. Execution context management.

### BackendTestingStudio.Assertions

Responsible for evaluating assertions against responses, variables, and execution state.

Contains:

1. Assertion contracts.
2. Assertion evaluators.
3. Built-in assertion types.
4. Failure diagnostics.

### BackendTestingStudio.Reporting

Responsible for execution summaries and report projections.

Contains:

1. Run result aggregation.
2. Report models.
3. Export-ready projections.
4. Derived metrics.

### BackendTestingStudio.Plugins

Responsible for plugin discovery, loading, validation, indexing, and compatibility checks.

Contains:

1. Plugin folder discovery.
2. Manifest parsing.
3. Schema validation.
4. Dependency resolution inside a plugin.
5. Catalog generation for the UI.

## 4. Allowed Dependencies

The dependency direction is strictly one-way.

| From | Allowed dependencies |
| --- | --- |
| UI | Core |
| Core | None of the infrastructure projects; only BCL and its own contracts |
| Http | Core |
| Storage | Core |
| Scenarios | Core |
| Assertions | Core |
| Reporting | Core |
| Plugins | Core |

Forbidden dependencies:

1. Core must not reference UI, Http, Storage, Scenarios, Assertions, Reporting, or Plugins.
2. UI must not reference storage or HTTP implementations directly.
3. Plugins must not reference concrete engine internals.
4. No project may depend on another project for a private implementation detail.

## 5. Clean Architecture Mapping

### Domain / Enterprise Rules

Owned by Core:

1. Plugin identity.
2. Environment definitions.
3. Scenario execution intent.
4. Assertion intent.
5. Execution results and state.

### Application Layer

Owned by Core:

1. Use case orchestration.
2. Transaction boundaries.
3. Validation before execution.
4. Coordination of HTTP, storage, scenarios, assertions, and reporting.

### Infrastructure Layer

Owned by Http, Storage, Scenarios, Assertions, Reporting, and Plugins:

1. External communication.
2. File system access.
3. SQLite access.
4. JSON parsing and validation.
5. Execution of infrastructure-specific strategies.

### Presentation Layer

Owned by UI:

1. Routing.
2. Layout.
3. Forms.
4. Interaction state.
5. Display of execution results.

## 6. Data Flow

### Plugin Discovery Flow

1. UI asks Core for the available plugin catalog.
2. Core asks the plugin subsystem for discovered manifests.
3. Plugins project scans the plugin root directory.
4. Each plugin is validated against `PLUGIN_SPEC.md`.
5. The validated catalog is cached in Storage.
6. UI renders the catalog and available actions.

### Scenario Execution Flow

1. User selects a plugin, environment, and scenario.
2. UI sends the request to Core.
3. Core builds an execution plan from plugin metadata and scenario steps.
4. Core resolves variables and payload references.
5. Core asks the HTTP engine to execute each request.
6. Responses are sent to the assertion engine.
7. Assertions produce pass or fail outcomes.
8. Scenario results and execution traces are persisted.
9. Reporting creates summaries and projections.
10. UI displays the final result.

### Variable Resolution Flow

1. Load plugin variables.
2. Apply environment overrides.
3. Apply scenario inputs.
4. Apply runtime values from prior responses.
5. Apply step-level values.
6. Resolve placeholders only at execution time.

## 7. Separation of Layers

### UI Layer

The UI only renders state and collects input. It does not contain workflow logic, request building, or persistence logic.

### Application Layer

The Core contains orchestration and rules. It decides what must happen and in what order, but not how HTTP or SQLite are implemented.

### Infrastructure Layer

Infrastructure contains the technical details of execution and persistence. It may parse files, call remote APIs, write SQLite records, and build report artifacts, but it does not own business decisions.

## 8. Patterns Used

1. Clean Architecture.
2. Dependency Inversion.
3. Repository pattern.
4. Strategy pattern for execution behaviors.
5. Factory pattern for manifest and model creation.
6. Pipeline pattern for HTTP execution and assertion evaluation.
7. Adapter pattern between Core contracts and infrastructure implementations.
8. Specification-style validation for plugin manifests and scenarios.
9. Read model projections for reporting.

## 9. SOLID Principles

### Single Responsibility

Each entity, service, repository, and component has one reason to change.

### Open/Closed

New plugin capabilities, assertion types, and execution behaviors are introduced by extension, not by editing stable rules.

### Liskov Substitution

Any implementation of a Core contract must behave consistently with the contract expectations.

### Interface Segregation

Contracts are small and focused. UI, HTTP, storage, scenario execution, and plugin discovery each have dedicated interfaces.

### Dependency Inversion

Core defines abstractions. Infrastructure depends on abstractions, not on the reverse.

## 10. Dependency Injection

1. The UI is the composition root.
2. All infrastructure implementations are registered through module-level registration methods.
3. Core receives dependencies only through constructor injection.
4. No service locator pattern.
5. No static access to application services.

## 11. Plugin Architecture

The plugin architecture is declarative.

1. A plugin is a folder.
2. A plugin is defined by JSON manifests and related content files.
3. The engine loads metadata, environments, modules, endpoints, payloads, scenarios, variables, and assertions from files.
4. The engine never requires plugin-specific C# code.
5. The engine builds UI and execution behavior from the plugin contract.

### Plugin Loading Stages

1. Discover.
2. Read.
3. Validate.
4. Index.
5. Cache.
6. Activate.
7. Execute.

## 12. DTO Conventions

1. DTOs are transport contracts only.
2. DTOs never contain business rules.
3. DTOs are immutable by default.
4. DTOs use clear names that match the user intent, not infrastructure internals.
5. DTOs are versioned only when the contract changes in a breaking way.
6. UI-facing DTOs are separate from persistence models.

## 13. Service Conventions

1. Services represent use cases.
2. Services orchestrate, they do not own low-level implementation details.
3. Services are stateless whenever possible.
4. Services return results and diagnostics, not raw infrastructure objects.
5. Services validate inputs before calling infrastructure.

## 14. Repository Conventions

1. Repositories encapsulate persistence concerns.
2. Repositories do not contain business rules.
3. Repositories return domain models or read models, never ad hoc database shapes.
4. Repositories hide SQLite schema details from the rest of the system.
5. Repositories are designed for testability and replaceability.

## 15. UI Conventions

1. Components are thin.
2. Pages delegate to services.
3. Forms map to DTOs, not to infrastructure entities.
4. UI state is explicit and local unless shared state is required.
5. UI does not depend on plugin file paths or SQLite schema details.

## 16. SQLite Conventions

1. SQLite is the local persistence layer only.
2. SQLite stores operational state, execution history, and cached indexes.
3. SQLite is not the source of truth for plugin files.
4. Schema changes must be versioned.
5. Write operations must be transactional.
6. Tables use stable names and explicit indexes for lookup fields.
7. The database model must support future pagination and report growth.

## 17. Scalability Strategy

1. Keep plugins external and declarative so they scale by content, not by code changes.
2. Keep Core free of infrastructure details so new engines can be added later.
3. Use cached plugin indexes to reduce discovery cost.
4. Use read models for reporting to avoid mixing execution writes with UI reads.
5. Isolate scenario execution so parallelization can be added later without breaking contracts.
6. Maintain strict contract boundaries so new storage engines or HTTP engines can be introduced with minimal impact.

## 18. Source of Truth

The following documents define the non-negotiable rules for all future work:

1. `ARCHITECTURE.md`
2. `PROJECT_RULES.md`
3. `PLUGIN_SPEC.md`

