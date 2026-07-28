# Backend Testing Studio

Backend Testing Studio is a local-first REST testing lab for declarative, version-controlled API test packages. One portable `plugin.json` describes environments, endpoints, payloads, assertions, variables and business scenarios; the Blazor UI and headless CLI execute the exact same contract and orchestration service.

## MVP capabilities

- Canonical, self-contained `plugin.json` schema 1.0.
- Structural, semantic, reference, cycle and engine-compatibility validation.
- REST GET, POST, PUT, PATCH and DELETE through `IHttpEngine`.
- None, Bearer, Basic and API Key authentication.
- Deterministic variables: runtime override > local secret > scenario > environment > plugin default.
- Sequential scenarios, response captures, stop/continue on failure and cancellation.
- Status, header, JSONPath and maximum-time assertions.
- Sanitized request/response evidence, correlation IDs and typed error categories.
- SQLite run history with retention and report-size limits.
- JSON, HTML and JUnit exports.
- `validate`, `list` and `run` CLI commands with stable exit codes.
- Production mutation guard, host allowlists, normal TLS validation and no redirect/retry of mutations.
- DummyJSON and Swagger PetStore canonical example plugins.

OAuth, GraphQL, gRPC, WebSockets, load testing, parallel execution, cloud collaboration, arbitrary plugin scripts and automatic OpenAPI import are outside MVP schema 1.0.

## Requirements

- .NET SDK 10.0.100 or a newer 10.0 patch (controlled by `global.json`).

## Run the UI

```bash
dotnet restore BackendTestingStudio.slnx
dotnet run --project BackendTestingStudio.UI
```

Open the Plugins page, enter an absolute path to a `plugin.json`, select **Open and validate**, then use Scenarios or **Open in Explorer**.

## Run the CLI

```bash
dotnet run --project BackendTestingStudio.Cli -- \
  validate --plugin plugins/dummyjson/plugin.json

dotnet run --project BackendTestingStudio.Cli -- \
  list --plugin plugins/dummyjson/plugin.json

BTS_SECRET_DUMMYJSON_PASSWORD='your-test-password' \
dotnet run --project BackendTestingStudio.Cli -- \
  run \
  --plugin plugins/dummyjson/plugin.json \
  --scenario login-and-auth-user \
  --environment dummyjson-live \
  --json artifacts/run.json \
  --html artifacts/run.html \
  --junit artifacts/run.xml
```

Exit codes:

- `0`: passed
- `1`: scenario ran but one or more tests failed
- `2`: plugin/configuration invalid or production guard blocked execution
- `3`: timeout, cancellation or execution error

Secrets are kept in process memory or injected through environment variables. Sensitive variable values are forbidden in `plugin.json` and are redacted before UI results, SQLite persistence, logs and reports.

## Build and test

```bash
dotnet restore BackendTestingStudio.slnx
dotnet build BackendTestingStudio.slnx -c Release --no-restore
dotnet test BackendTestingStudio.slnx -c Release --no-build --no-restore
dotnet list BackendTestingStudio.slnx package --vulnerable --include-transitive
```

The test suite uses controlled handlers/stubs and does not require an external API.

## Contract and authoring

- [Plugin specification](PLUGIN_SPEC.md)
- [JSON Schema 1.0](schemas/plugin.schema.v1.json)
- [Prompt to generate a production-ready plugin from an API](PROMPT_GENERADOR_PLUGIN_API.md)
- [Architecture](ARCHITECTURE.md)
- [User manual](USER_MANUAL.md)

Each example folder contains only its canonical `plugin.json` plus documentation; there are no secondary manifests.
