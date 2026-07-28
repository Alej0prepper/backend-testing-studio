# Plugin design decision

The exploratory multi-file plugin proposal has been superseded by the schema 1.0 decision:

- one explicit, self-contained `plugin.json`;
- no compiled plugin classes;
- no sidecar manifests or folder naming conventions;
- local secret values stay outside the file;
- UI and CLI use the same loader, compiler and runner.

See [PLUGIN_SPEC.md](PLUGIN_SPEC.md) and [schemas/plugin.schema.v1.json](schemas/plugin.schema.v1.json).
