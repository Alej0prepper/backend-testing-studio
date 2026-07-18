# Backend Testing Studio - Project Rules

## 1. Priority

These rules are mandatory. If any future decision conflicts with them, these rules win.

## 2. Global Rules

1. Never access `HttpClient` directly from the UI.
2. Every HTTP call must go through `IHttpEngine`.
3. No plugin may know the internal implementation of the engine.
4. Core must never depend on Plugins.
5. Do not duplicate logic across UI, Core, and infrastructure.
6. All code must be testable.
7. Every entity must have a single responsibility.
8. Business logic must never live in UI components.
9. SQLite access must be isolated behind repositories.
10. Plugin files are declarative; there is no plugin-specific C# code in the plugin contract.

## 3. Architecture Rules

1. The UI is only a composition root and presentation layer.
2. The Core owns use cases, contracts, and orchestration rules.
3. Infrastructure projects implement contracts, they do not define business policy.
4. Dependencies must always point inward.
5. The application must stay functional if a concrete infrastructure implementation is replaced.
6. Every new capability must be added at the lowest layer that can own it.

## 4. Execution Rules

1. Scenario execution must be deterministic for the same inputs, environment, and plugin version.
2. A scenario step must fail fast when a required dependency is missing.
3. Assertions must not mutate execution state.
4. Variable resolution must happen through a single engine, not ad hoc string replacement.
5. Request construction must be validated before execution.
6. A failed assertion must be recorded as a first-class result, not hidden as an exception.

## 5. Plugin Rules

1. Plugins are discovered from folders.
2. Plugins are validated before activation.
3. A plugin that fails validation must not be executed.
4. A plugin manifest must be the canonical identity of that plugin.
5. Plugin metadata must not be inferred from folder names alone.
6. Plugin compatibility must be checked before the UI exposes execution actions.
7. If a manifest, scenario, payload, or assertion is invalid, the engine must report the validation error clearly.
8. The engine must not silently ignore unknown required fields.

## 6. Data Rules

1. Persist only what is needed for execution history, reports, and cached discovery.
2. Do not store duplicated authoritative copies of plugin definitions in SQLite.
3. Use SQLite for local persistence, not as the system of record for plugins.
4. Store enough execution trace data to explain failures later.
5. Preserve environment and scenario snapshots with each run.

## 7. UI Rules

1. UI components must not contain request logic.
2. UI components must not access repositories directly.
3. UI must render state received from services.
4. UI must not contain plugin parsing rules.
5. UI must not depend on storage schema or file system structure.

## 8. Service Rules

1. Services must orchestrate rather than implement low-level protocol details.
2. Services must be small enough to test in isolation.
3. Services must depend on abstractions, never concrete infrastructure.
4. Services must expose clear inputs and outputs.
5. Services must not return infrastructure-specific exceptions without translation.

## 9. Repository Rules

1. Repositories own persistence access only.
2. Repositories must not implement business validation.
3. Repositories must not compose scenarios or assertions.
4. Repositories must hide SQL details from callers.
5. Repositories must be replaceable without changing Core contracts.

## 10. Error Handling Rules

1. Failures must be explicit.
2. Exceptions must be translated into domain-safe results at the boundary when appropriate.
3. Do not use generic exceptions when a typed exception is required.
4. Do not swallow errors.
5. Every failure must preserve enough context to diagnose the problem.

## 11. Logging Rules

1. Every external call must be traceable.
2. Log the plugin id, environment, scenario id, endpoint id, and correlation id for execution flows.
3. Log failures with actionable context.
4. Do not log sensitive values in plain text.
5. Logs must be structured.

## 12. Testability Rules

1. Every use case must be unit-testable.
2. Every plugin validation rule must be testable.
3. Every assertion type must have deterministic tests.
4. Every repository implementation must have integration coverage when practical.
5. Tests must not depend on uncontrolled external state.

## 13. Extension Rules

1. New features must not break existing plugin contracts without a version bump.
2. New assertion types must be added as extensions, not by rewriting the assertion engine.
3. New execution behaviors must respect the current plugin schema version.
4. Any breaking contract change must be documented before implementation.

## 14. Documentation Rules

1. If architecture changes, the governing documents must be updated first.
2. If a plugin field changes, `PLUGIN_SPEC.md` must be updated in the same change.
3. If a coding convention changes, `CODING_STANDARDS.md` must be updated in the same change.
4. If onboarding changes, `CONTRIBUTING.md` must be updated in the same change.

