# Backend Testing Studio - Roadmap

## v0.1 - Architectural Foundation

Goals:

1. Finalize the architecture documents.
2. Lock the plugin specification.
3. Define the Core contracts.
4. Define the project boundaries.
5. Define the validation rules for plugins, scenarios, and assertions.

Deliverables:

1. `ARCHITECTURE.md`
2. `PROJECT_RULES.md`
3. `CODING_STANDARDS.md`
4. `CONTRIBUTING.md`
5. `PLUGIN_SPEC.md`
6. `ROADMAP.md`

Success criteria:

1. The architecture is unambiguous.
2. The plugin format is deterministic.
3. Future implementation work can start without changing the core design.

## v0.2 - Solution Bootstrap

Goals:

1. Create the solution structure.
2. Create the projects defined by the architecture.
3. Wire the project references according to the dependency rules.
4. Set up the composition root in the UI project.
5. Establish the base configuration for logging, dependency injection, and local storage access.

Success criteria:

1. The solution builds.
2. The boundaries are respected.
3. No feature logic is implemented yet.

## v0.3 - Plugin Discovery and Catalog

Goals:

1. Load plugin folders.
2. Parse plugin metadata.
3. Validate manifests.
4. Show the plugin catalog in the UI.
5. Cache discovery results locally.

Success criteria:

1. Plugins can be discovered without hard-coded API knowledge.
2. Invalid plugins are rejected with clear diagnostics.

## v0.4 - Environment and Variable Management

Goals:

1. Load environments from plugin definitions.
2. Add variable resolution and precedence.
3. Support secure handling of sensitive values.
4. Persist environment snapshots for runs.

Success criteria:

1. Users can configure execution context per plugin.
2. Variable resolution is deterministic.

## v0.5 - Endpoint Execution Engine

Goals:

1. Execute declarative endpoints through `IHttpEngine`.
2. Support payload substitution.
3. Support request headers, query strings, and path parameters.
4. Capture response bodies and metadata.

Success criteria:

1. A plugin endpoint can be executed end to end.
2. Execution results are traceable.

## v0.6 - Assertions and Validation

Goals:

1. Implement built-in assertion types.
2. Evaluate assertions against responses and captured variables.
3. Store assertion results with diagnostics.

Success criteria:

1. Assertion failures are explicit and readable.
2. Assertion behavior is deterministic.

## v0.7 - Scenario Orchestration

Goals:

1. Execute ordered scenarios.
2. Resolve step dependencies.
3. Capture variables between steps.
4. Support reusable endpoint and scenario references.

Success criteria:

1. A scenario can drive a complete API test flow.
2. The execution trace explains each step.

## v0.8 - Reporting and History

Goals:

1. Persist run history.
2. Build execution summaries.
3. Add plugin, scenario, and assertion-level reporting views.
4. Support filtering and drill-down.

Success criteria:

1. Users can review what happened after execution.
2. Reports are linked to the exact plugin version used.

## v0.9 - Usability and Scale Hardening

Goals:

1. Improve UI generation from plugin metadata.
2. Add caching and performance improvements.
3. Improve validation diagnostics.
4. Harden SQLite migrations and schema versioning.
5. Prepare for larger plugin catalogs.

Success criteria:

1. The product remains responsive with growing plugin sets.
2. The contracts remain stable under load.

## v1.0 - Stable Product

Goals:

1. Freeze the public plugin contract except for additive changes.
2. Stabilize the execution model.
3. Document supported extension points.
4. Treat architecture and plugin compatibility as release-quality guarantees.

Success criteria:

1. The product can be used repeatedly without architectural churn.
2. Plugin authors can rely on the published contract.

