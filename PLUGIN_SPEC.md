# Backend Testing Studio — Plugin Contract 1.0

`plugin.json` is the only executable and authoritative definition of an API. The UI and CLI load the same file through `IDeclarativePluginLoader`; compiled C# plugins and fragmented manifests are not supported.

The versioned JSON Schema is [`schemas/plugin.schema.v1.json`](schemas/plugin.schema.v1.json). Unknown properties are rejected.

## Top-level contract

Required top-level fields:

- `id`, `name`, `version`, `schemaVersion`, `engineVersion`, `author`, `description`
- `defaultEnvironment`
- `variables`, `environments`, `modules`, `payloads`, `assertions`, `scenarios`

`schemaVersion` must be compatible with `1.0.0`. `engineVersion` is the minimum required engine. IDs use kebab-case and must be unique within their type; endpoint IDs are globally unique inside a plugin.

## Variables and secrets

Interpolation uses `{{VariableName}}`. A placeholder that remains unresolved fails before HTTP is sent.

Precedence, from highest to lowest:

1. CLI/UI runtime override
2. Session or environment-variable secret
3. Scenario value
4. Environment value
5. Non-secret plugin default

Sensitive variables:

- set `sensitive: true`;
- must not have `defaultValue`;
- are accepted in the UI only for the current process;
- are read by the CLI from `BTS_SECRET_<PLUGIN_ID>_<VARIABLE>` or `BTS_SECRET_<VARIABLE>`;
- are redacted from request/response evidence, runs and exports;
- computed sensitive variables, such as access tokens, are captured at runtime and then redacted.

## Environments

Each environment defines `id`, `name`, `baseUrl`, `allowedHosts`, optional headers/variables/authentication, `level`, and timeout.

Supported authentication types are `None`, `Bearer`, `Basic`, and `ApiKey`. Secret values must be placeholders that reference sensitive variables.

`level` is `Development`, `Staging`, or `Production`. Scenarios containing POST, PUT, PATCH or DELETE are blocked in Production unless the user explicitly authorizes them (`--allow-production` in the CLI).

The base URL host must be present in `allowedHosts`. Redirect following is disabled, TLS validation cannot be globally bypassed, and mutating methods are never retried automatically.

## Modules and endpoints

Modules group endpoints and may define `basePath`, tags, and default headers. Endpoints support GET, POST, PUT, PATCH and DELETE and can define:

- headers and query values;
- a reusable `payload` reference or inline `body`;
- assertion references;
- variables captured from the response;
- tags and timeout metadata.

## Payloads

A payload has `id`, `contentType`, and JSON `content`. Interpolation is applied to the serialized content before sending.

## Assertions

Schema 1.0 supports:

| Type | Required fields | Operators |
| --- | --- | --- |
| `StatusCode` | `expected` | `Equals` |
| `Header` | `header`, optional `expected` | `Equals`, `Contains`, `Null`, `NotNull` |
| `JsonPath` | `path`, optional `expected` | `Equals`, `Contains`, `Null`, `NotNull` |
| `MaxTime` | `maximumMilliseconds` | `MaxTime` |

The JSONPath subset is root `$`, property access (`$.user.id`), array index (`$[0]`) and wildcard (`$.items[*]`).

## Scenarios

A scenario contains ordered steps. Each step references an endpoint through `execute` and can override variables, assertions, captures, enabled state, and stop behavior. `dependsOn` documents ordering dependencies and is checked for missing references and cycles; execution remains sequential in schema 1.0.

Capture sources are `JsonPath`, `Header`, `StatusCode`, and `Body`. Required captures fail the step when absent. Captured values are available to later steps.

## Validation diagnostics

An invalid plugin is never executable. Each diagnostic includes:

- severity;
- exact file;
- JSON path;
- stable rule identifier;
- actionable message.

Validation covers JSON structure and unknown fields, required metadata, compatibility, unique IDs, references, allowed methods/hosts, secret defaults, capture requirements, and dependency cycles.

## Minimal commands

```bash
dotnet run --project BackendTestingStudio.Cli -- validate --plugin /path/plugin.json
dotnet run --project BackendTestingStudio.Cli -- list --plugin /path/plugin.json
dotnet run --project BackendTestingStudio.Cli -- run \
  --plugin /path/plugin.json \
  --scenario smoke \
  --environment dev \
  --junit artifacts/results.xml \
  --html artifacts/results.html
```

Exit codes are `0` passed, `1` test failed, `2` invalid plugin/configuration or production guard, and `3` execution error/timeout/cancellation.
