# Swagger PetStore plugin

`plugin.json` is the complete and only executable definition. It includes a development environment and a sequential create/read/update/delete pet scenario.

```bash
dotnet run --project BackendTestingStudio.Cli -- validate --plugin plugins/swagger-petstore/plugin.json
dotnet run --project BackendTestingStudio.Cli -- run \
  --plugin plugins/swagger-petstore/plugin.json \
  --scenario pet-crud-lifecycle \
  --environment swagger-petstore-live
```

The public PetStore is shared and not a reliable CI target. Repository tests validate and compile this plugin without depending on the external service.
