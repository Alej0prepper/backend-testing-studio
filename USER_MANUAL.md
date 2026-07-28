# Backend Testing Studio — User Manual

## 1. Start

```bash
dotnet run --project BackendTestingStudio.UI
```

Open the local URL printed by ASP.NET Core.

## 2. Open a plugin

1. Go to **Plugins**.
2. Enter the absolute path to a self-contained `plugin.json`.
3. Select **Open and validate**.
4. Fix every error diagnostic before execution. Diagnostics include the file, JSON path, rule and message.

Use **Reload** after editing the file. A plugin marked Invalid cannot run.

## 3. Explore an endpoint

From the active plugin card, choose **Open in Explorer**. Method, absolute URL, headers, query and body are preloaded. Manual explorer requests still pass only through `IHttpEngine` and their history is sanitized.

## 4. Run a scenario

1. Go to **Scenarios**.
2. Select environment and scenario.
3. Enter required secrets. They remain only in memory for the process lifetime.
4. Optionally enter runtime overrides as `Name=Value`, one per line.
5. Select **Run** or **Cancel**.

Mutating Production scenarios require the explicit authorization checkbox. Results show sanitized request/response snapshots, duration, status, correlation ID, captures, each assertion and a typed technical error.

## 5. Reports

The Reports page lists persisted sanitized runs. Open one and export JSON, HTML or JUnit. Counts and logical status are generated from the same `ExecutionReport`.

The default retention is 30 days and at most 200 runs. A single report cannot exceed 2 MiB.

## 6. CLI and CI

```bash
dotnet run --project BackendTestingStudio.Cli -- validate --plugin /path/plugin.json
dotnet run --project BackendTestingStudio.Cli -- list --plugin /path/plugin.json
dotnet run --project BackendTestingStudio.Cli -- run \
  --plugin /path/plugin.json \
  --scenario smoke \
  --environment staging \
  --var Tenant=qa \
  --junit artifacts/results.xml \
  --html artifacts/results.html
```

For a variable `ClientSecret` in plugin `orders-api`, set either:

```bash
export BTS_SECRET_ORDERS_API_CLIENTSECRET='...'
# or
export BTS_SECRET_CLIENTSECRET='...'
```

Scoped names take precedence. Do not pass secrets through `--var`.

Use `--allow-production` only after reviewing all mutating endpoints. Use `--timeout 60000` to override the environment timeout.

Exit codes are `0` passed, `1` assertion failed, `2` invalid plugin/configuration/production guard, `3` execution failure.

## 7. Author a plugin

Start with [PLUGIN_SPEC.md](PLUGIN_SPEC.md), validate against [schemas/plugin.schema.v1.json](schemas/plugin.schema.v1.json), and run the CLI validator. To generate a first complete draft from an OpenAPI document or endpoint inventory, use [PROMPT_GENERADOR_PLUGIN_API.md](PROMPT_GENERADOR_PLUGIN_API.md).

Only `plugin.json` is executable. Never add credentials, tokens, API keys or session cookies to it.
