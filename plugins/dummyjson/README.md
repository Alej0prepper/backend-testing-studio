# DummyJSON plugin

`plugin.json` is the complete and only executable definition. It includes environments, variables, endpoints, payloads, assertions and scenarios for the login/authenticated-user flow and a product smoke flow.

```bash
dotnet run --project BackendTestingStudio.Cli -- validate --plugin plugins/dummyjson/plugin.json
export BTS_SECRET_DUMMYJSON_PASSWORD='your DummyJSON test password'
dotnet run --project BackendTestingStudio.Cli -- run \
  --plugin plugins/dummyjson/plugin.json \
  --scenario login-and-auth-user \
  --environment dummyjson-live
```

The API is external and its mutating product endpoint is simulated. Automated repository tests use a controlled HTTP stub rather than this public service.
