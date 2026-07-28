# Backend Testing Studio — Architecture

## Dependency direction

```text
UI ─┐
CLI ├─> Application ─> Core
    │       │
    ├─> Plugins ─────┤
    ├─> Scenarios ───┤
    ├─> Assertions ──┤
    ├─> Reporting ───┤
    ├─> HTTP ────────┤
    └─> Storage ─────┘
```

`Core` owns contracts and execution models and has no infrastructure dependency. UI and CLI are composition roots/adapters. Business orchestration lives in `ScenarioRunService`, not in Razor components.

## Canonical execution flow

```text
plugin.json
  -> DeclarativePluginLoader (parse + diagnostics)
  -> PluginCompiler (environment + endpoint + payload + assertions)
  -> ScenarioRunService (secrets + precedence + safety + timeout)
  -> ScenarioEngine
       -> IHttpEngine
       -> IAssertionEngine
       -> response variable capture
  -> central redaction
  -> IReportEngine
  -> IScenarioRunRepository (SQLite)
  -> UI / CLI / JSON / HTML / JUnit
```

Invalid plugins never reach the compiler. All unresolved required placeholders fail before HTTP. UI and CLI use the same loader, compiler, runner, redactor, reporting and persistence contracts.

## Projects

- `BackendTestingStudio.Core`: contracts, immutable execution and reporting models.
- `BackendTestingStudio.Application`: workspace state, secret/redaction services and scenario-run orchestration.
- `BackendTestingStudio.Plugins`: JSON loader, semantic validator and compiler.
- `BackendTestingStudio.Http`: the only direct `HttpClient` adapter.
- `BackendTestingStudio.Assertions`: deterministic assertion evaluation.
- `BackendTestingStudio.Scenarios`: sequential scenario engine and variable captures.
- `BackendTestingStudio.Reporting`: logical report plus JSON/HTML/Markdown/JUnit exporters.
- `BackendTestingStudio.Storage`: SQLite repositories and versioned run schema.
- `BackendTestingStudio.UI`: Blazor presentation and DI composition root.
- `BackendTestingStudio.Cli`: headless validation/execution adapter and CI exit codes.

## Security boundaries

- Plugins cannot execute code.
- Plugin secret defaults are rejected.
- UI secrets are session-only; CLI secrets come from environment variables.
- The central redactor runs before results are returned or persisted.
- Legacy environment authentication values are no longer persisted; existing plaintext auth columns are scrubbed on repository initialization.
- Base hosts require an allowlist match.
- Redirect following and global TLS bypass are disabled.
- Production mutation requires explicit authorization.
- POST, PUT, PATCH and DELETE are not retried.

## Persistence

Plugin definitions remain files and are never copied into SQLite as authoritative data. Runs persist sanitized plugin/environment snapshots and a sanitized logical report. The `schema_migrations` table versions the run schema. Default retention keeps at most 200 recent runs and removes runs older than 30 days. Reports larger than 2 MiB are rejected.
