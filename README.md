# Backend Testing Studio

Backend Testing Studio is a local tool for testing backend APIs from a web interface. The project is organized in layers and decoupled engines to support HTTP exploration, environments, variables, history, reusable payloads, assertions, scenarios, reports, and plugins.

The product goal is to define reusable API tests without coupling the UI to concrete implementations such as `HttpClient`, SQLite, or specific plugins.

## Current Status

Functional from the UI:

- Dashboard and main layout.
- Sidebar and top navigation.
- Environment management.
- Environment variables.
- Environment headers.
- Environment authentication: Bearer, Basic, and ApiKey.
- API Explorer for sending HTTP requests.
- HTTP methods: GET, POST, PUT, PATCH, and DELETE.
- Headers, query parameters, and JSON body.
- Basic multipart support.
- Variable resolution using the `{{Variable}}` format.
- Request history persisted in SQLite.
- Repeat requests from History.
- Reusable JSON payload library.
- Installed plugin catalog from compiled modules.

Implemented as engines or libraries, but not yet exposed through a complete operating screen:

- Assertion Engine.
- Scenario Engine.
- Reporting Engine.
- Report export to HTML, Markdown, and JSON.

Included declarative plugins:

- Swagger PetStore.
- DummyJSON.

Current limitations:

- Declarative plugins under `plugins/` already include structure, payloads, assertions, and scenarios, but they are not automatically loaded or executed from the UI yet.
- The Scenarios UI does not yet allow creating or running scenarios.
- The Reports UI does not yet list executions or export reports from the screen.
- OAuth is not implemented.

## Requirements

- Linux, macOS, or Windows with a terminal.
- .NET SDK compatible with the repository projects.
- Modern web browser.
- Git.

The project uses SQLite for local persistence. The database is generated automatically when the application runs.

## How To Run

From the repository root:

```bash
./run.sh
```

Direct `dotnet` alternative:

```bash
dotnet run --project BackendTestingStudio.UI/BackendTestingStudio.UI.csproj
```

When the application starts, the terminal will show a line similar to:

```text
Now listening on: http://127.0.0.1:XXXXX
```

Open that URL in the browser. The port can change on each run.

To stop the application:

```text
Ctrl+C
```

## How To Test

Build the full solution:

```bash
dotnet build BackendTestingStudio.sln
```

Run all tests:

```bash
dotnet test BackendTestingStudio.sln
```

Run tests for a specific project:

```bash
dotnet test BackendTestingStudio.Http.Tests/BackendTestingStudio.Http.Tests.csproj
dotnet test BackendTestingStudio.Storage.Tests/BackendTestingStudio.Storage.Tests.csproj
dotnet test BackendTestingStudio.Assertions.Tests/BackendTestingStudio.Assertions.Tests.csproj
dotnet test BackendTestingStudio.Scenarios.Tests/BackendTestingStudio.Scenarios.Tests.csproj
dotnet test BackendTestingStudio.Reporting.Tests/BackendTestingStudio.Reporting.Tests.csproj
dotnet test BackendTestingStudio.Plugins.Tests/BackendTestingStudio.Plugins.Tests.csproj
```

## Repository Structure

```text
BackendTestingStudio/
├── BackendTestingStudio.UI/             # Web application and screens
├── BackendTestingStudio.Core/           # Core models, contracts, and rules
├── BackendTestingStudio.Http/           # Decoupled HTTP engine
├── BackendTestingStudio.Storage/        # SQLite persistence and repositories
├── BackendTestingStudio.Assertions/     # Validation engine
├── BackendTestingStudio.Scenarios/      # Scenario execution engine
├── BackendTestingStudio.Reporting/      # Report generation and export
├── BackendTestingStudio.Plugins/        # Plugin model and base infrastructure
├── BackendTestingStudio.*.Tests/        # Automated tests by module
├── plugins/                             # Example declarative plugins
├── ARCHITECTURE.md                      # General architecture
├── PROJECT_RULES.md                     # Mandatory project rules
├── CODING_STANDARDS.md                  # Coding standards
├── CONTRIBUTING.md                      # Contribution guide
├── PLUGIN_SPEC.md                       # Plugin specification
├── ROADMAP.md                           # Product roadmap by version
├── USER_MANUAL.md                       # User manual
├── Promts_guide.md                      # Applied prompts guide
├── progreso                             # Prompt progress tracking
└── run.sh                               # Local run script
```

## Project Responsibilities

### BackendTestingStudio.UI

Contains the web application. Its responsibilities are:

- Render the interface.
- Receive user interaction.
- Orchestrate use cases by calling services.
- Display results.

Important rule: the UI must not use `HttpClient` directly. Every HTTP execution must go through `IHttpEngine`.

### BackendTestingStudio.Core

Contains core contracts, models, and abstractions. It must remain independent from UI, SQLite, concrete engines, and external plugins.

Responsibilities:

- Domain entities.
- Shared DTOs.
- Main interfaces.
- Contracts for services, repositories, and engines.

### BackendTestingStudio.Http

Implements the generic HTTP engine.

Responsibilities:

- Execute GET, POST, PUT, PATCH, and DELETE requests.
- Resolve headers.
- Resolve query parameters.
- Send JSON.
- Send multipart content.
- Apply Bearer, Basic, and ApiKey authentication.
- Resolve runtime variables before executing the request.

### BackendTestingStudio.Storage

Implements local persistence with SQLite.

Responsibilities:

- Repositories.
- Database initialization.
- Environment CRUD.
- Payload CRUD.
- History persistence.
- Variable and header persistence.

### BackendTestingStudio.Assertions

Implements validations decoupled from any specific API.

Supports:

- StatusCode.
- JSONPath.
- Headers.
- Equals.
- Contains.
- Null.
- NotNull.
- Maximum time.

### BackendTestingStudio.Scenarios

Implements the scenario engine.

A scenario can have multiple steps. Each step can:

- Execute requests.
- Save variables.
- Execute assertions.
- Use variables from previous steps.
- Stop the flow when it fails.

### BackendTestingStudio.Reporting

Generates execution reports.

Each report can include:

- Summary.
- Total time.
- Steps.
- Assertions.
- Variables.
- Errors.

Supported formats:

- HTML.
- Markdown.
- JSON.

### BackendTestingStudio.Plugins

Defines the plugin model and its base infrastructure.

A plugin can declare:

- Name.
- Version.
- Author.
- Description.
- Endpoints.
- Scenarios.
- Payloads.
- Variables.
- Assertions.
- Environments.

## Available Screens

### Dashboard

Main workspace view. Shows the general application state and access to the main sections.

### Environments

Allows creating, editing, and deleting environments.

An environment can contain:

- Name.
- Base URL.
- Variables.
- Headers.
- Authentication configuration.

### API Explorer

Allows manually building and executing a request.

Basic flow:

1. Select an Environment.
2. Choose the HTTP method.
3. Write a URL or path.
4. Add headers when needed.
5. Add query parameters when needed.
6. Add a JSON body when needed.
7. Select a payload when needed.
8. Send the request.
9. Review status, headers, body, and time.

### Payloads

Allows saving reusable JSON.

Each payload can have:

- Name.
- Description.
- JSON.
- Variables.
- Tags.

### History

Automatically saves requests executed from API Explorer.

Allows reviewing:

- Date.
- Method.
- URL.
- Environment.
- Headers.
- Body.
- Response.
- Time.
- Status.

It also allows repeating a request.

### Plugins

Shows installed plugins from the current compiled-module system.

Note: declarative plugins in JSON folders exist under `plugins/`, but full loading and execution integration from the UI is still pending.

## Included Plugins

### Swagger PetStore

Location:

```text
plugins/swagger-petstore/
```

Includes:

- Create pet.
- Get pet.
- Update pet.
- Delete pet.
- CRUD scenarios.
- Create and update payloads.
- Status and content assertions.

### DummyJSON

Location:

```text
plugins/dummyjson/
```

Includes:

- Login.
- Products.
- Cart.
- Users.
- Complete workflow scenarios.
- Reusable payloads.
- Assertions by module.

## Expected Declarative Plugin Structure

```text
plugins/{plugin-id}/
├── plugin.json
├── variables.json
├── README.md
├── environments/
│   └── {environment}.json
├── modules/
│   └── {module}.json
├── payloads/
│   └── {payload}.json
├── assertions/
│   └── {assertion}.json
└── scenarios/
    └── {scenario}.json
```

The full specification is available in `PLUGIN_SPEC.md`.

## Architecture Rules

New decisions must respect:

- `ARCHITECTURE.md`
- `PROJECT_RULES.md`
- `PLUGIN_SPEC.md`

Key rules:

- The UI never accesses `HttpClient` directly.
- Every HTTP call goes through `IHttpEngine`.
- Core never depends on Plugins.
- Core never depends on UI.
- Plugins do not know the engine's internal implementation.
- SQLite persistence must stay behind repositories or services.
- Do not duplicate logic.
- All code must be testable.
- Every entity must have a single responsibility.

## Data Flow

Manual flow from API Explorer:

```text
UI
→ Application service
→ IHttpEngine
→ Authentication / variables / headers / query / body
→ External API
→ IHttpEngineResponse
→ SQLite history
→ UI
```

Expected scenario flow:

```text
Scenario Engine
→ Step
→ IHttpEngine
→ Assertion Engine
→ Runtime Variables
→ Reporting Engine
```

Expected plugin flow:

```text
Plugin
→ Endpoints / Payloads / Variables / Scenarios
→ Scenario Engine or API Explorer
→ IHttpEngine
```

## Local Persistence

SQLite is used for local application data.

Currently persisted data:

- Environments.
- Environment variables.
- Environment headers.
- Payloads.
- Request history.

The database is a local artifact generated when the application runs and must not be committed to the repository.

## Git And Push

View remotes:

```bash
git remote -v
```

First push for the `master` branch:

```bash
git push -u origin master
```

Subsequent push:

```bash
git push
```

If Git asks for a username or token, authenticate from your local terminal.

## Known Issues

### Linux inotify Limit

If an error similar to this appears:

```text
The configured user limit (128) on the number of inotify instances has been reached
```

Run with:

```bash
DOTNET_USE_POLLING_FILE_WATCHER=1 ./run.sh
```

Or increase the system watcher limit.

### 404 Error For `_framework/blazor.web.js`

If the browser or logs show:

```text
GET /_framework/blazor.web.js 404
```

Try:

```bash
dotnet restore
dotnet build BackendTestingStudio.sln
./run.sh
```

Also verify that you are opening the exact URL printed by `dotnet run`, because the port changes.

### SQLitePCLRaw Warning

A vulnerability warning may appear for a transitive SQLite package version. The project can compile, but dependencies should be updated in a dedicated iteration to close that alert.

## Related Documentation

- `ARCHITECTURE.md`: architecture and responsibility separation.
- `PROJECT_RULES.md`: mandatory rules.
- `CODING_STANDARDS.md`: coding conventions.
- `CONTRIBUTING.md`: how to contribute.
- `PLUGIN_SPEC.md`: plugin contract.
- `PLUGIN_CREATION_GUIDE.md`: practical guide for creating plugins from an API.
- `ROADMAP.md`: roadmap by version.
- `USER_MANUAL.md`: step-by-step user manual.
- `progreso`: applied prompt tracking.

## Technical Next Steps

1. Integrate real declarative plugin loading from `plugins/`.
2. Create UI for running scenarios.
3. Connect Scenario Engine with Reporting Engine from the UI.
4. Add the Reports screen.
5. Allow exporting reports from the application.
6. Harden JSON plugin validation against `PLUGIN_SPEC.md`.
7. Update dependencies with security warnings.
