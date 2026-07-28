# Plugin creation guide

Backend Testing Studio schema 1.0 uses one self-contained `plugin.json`; there are no module, payload, assertion or scenario sidecar files.

1. Collect the OpenAPI document, base URLs, authentication flow, test accounts, safe CRUD entity, cleanup behavior and forbidden endpoints.
2. Use [PROMPT_GENERADOR_PLUGIN_API.md](PROMPT_GENERADOR_PLUGIN_API.md) with that material.
3. Validate the result against [schemas/plugin.schema.v1.json](schemas/plugin.schema.v1.json).
4. Run:

   ```bash
   dotnet run --project BackendTestingStudio.Cli -- \
     validate --plugin /absolute/path/plugin.json
   ```

5. Review all mutating endpoints, production levels, `allowedHosts`, sensitive variables and assertions.
6. Test against development/staging. Keep secrets in UI session memory or `BTS_SECRET_*` environment variables.
7. Export JSON, HTML and JUnit, and confirm no sensitive value appears.

The complete contract and supported assertion/JSONPath subsets are documented in [PLUGIN_SPEC.md](PLUGIN_SPEC.md).
