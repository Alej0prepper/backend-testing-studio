# Backend Testing Studio roadmap

## MVP — implemented

- Canonical self-contained plugin schema 1.0 and semantic validator.
- Plugin compiler and shared application runner.
- Deterministic variables, response captures and central secret redaction.
- Scenario and report UI, endpoint handoff to API Explorer.
- Sanitized versioned SQLite runs with retention and size policy.
- JSON, HTML and JUnit output.
- Headless validate/list/run CLI and stable exit codes.
- Production mutation guard, timeout/cancellation and host restrictions.
- Linux CI build/test/dependency audit.

## P1

- OpenAPI 3 importer from file/URL.
- Run filtering by tags and modules.
- Run comparison for status, contract and timing changes.
- Cookie/session support.
- Better multipart file selection.
- Assisted assertion/scenario authoring.

## P2

- Parallel execution.
- Scheduling and monitoring.
- Postman/Bruno import.
- GraphQL and gRPC.
- Collaboration and plugin distribution.
- Load/performance testing.

Breaking contract changes require a new schema major version. Schema 1.x additions must remain backward compatible.
