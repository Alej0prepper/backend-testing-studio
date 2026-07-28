# Prompt maestro: convertir una API en un plugin listo para Backend Testing Studio

Copia el bloque completo en ChatGPT. Adjunta el OpenAPI JSON/YAML y cualquier documentación relevante. Reemplaza los campos entre `<< >>`.

```text
Actúa como arquitecto de pruebas de APIs REST y generador estricto de contratos para Backend Testing Studio.

OBJETIVO
Analiza la API que adjunto y crea un plugin de pruebas completo, seguro, determinista y directamente ejecutable por Backend Testing Studio. El resultado ejecutable debe ser UN SOLO archivo llamado plugin.json. No generes carpetas, sidecars, clases C#, scripts, archivos de ambientes, payloads, assertions ni scenarios separados.

DATOS DE LA API
- Nombre: <<NOMBRE_API>>
- Propietario/autor del plugin: <<AUTOR>>
- OpenAPI o documentación: <<ADJUNTO_O_PEGAR_AQUI>>
- Base URL de development: <<URL_DEV_O_NO_DISPONIBLE>>
- Base URL de staging: <<URL_STAGING_O_NO_DISPONIBLE>>
- Autenticación y renovación: <<DESCRIPCION_AUTH>>
- Roles/perfiles de prueba: <<ROLES>>
- Entidad permitida para CRUD: <<ENTIDAD_SEGURA>>
- Estrategia de cleanup: <<REGLA_O_ENDPOINT_CLEANUP>>
- Datos semilla estables: <<DATOS_SEMILLA>>
- Headers obligatorios (tenant, idioma, correlation id, etc.): <<HEADERS>>
- Endpoints prohibidos o peligrosos: <<ENDPOINTS_PROHIBIDOS>>
- Límites aceptables de requests: <<LIMITES>>

CONTRATO OBLIGATORIO
El JSON debe cumplir Backend Testing Studio schema 1.0:

1. Propiedades raíz obligatorias:
   id, name, version, schemaVersion, engineVersion, author, description,
   defaultEnvironment, tags, variables, environments, modules, payloads,
   assertions, scenarios.

2. Usa exactamente:
   "schemaVersion": "1.0.0"
   "engineVersion": "1.0.0"
   "version": "1.0.0"

3. IDs:
   - kebab-case;
   - únicos por tipo;
   - IDs de endpoint globalmente únicos;
   - cada referencia debe existir.

4. Variables:
   {
     "name": "PascalCaseName",
     "type": "string|number|boolean|json",
     "defaultValue": "solo si NO es secreta",
     "required": true|false,
     "sensitive": true|false,
     "computed": true|false,
     "exportable": true|false,
     "description": "..."
   }

   Marca como sensitive tokens, passwords, API keys, client secrets, cookies y credenciales.
   Una variable sensitive NUNCA puede tener defaultValue.
   Los tokens capturados son sensitive=true, computed=true, exportable=false.
   Usa placeholders {{VariableName}}.
   No incluyas ningún secreto real, ni siquiera valores de ejemplo plausibles.

5. Environments:
   {
     "id": "dev",
     "name": "Development",
     "baseUrl": "https://...",
     "level": "Development|Staging|Production",
     "allowedHosts": ["host-exacto"],
     "headers": { "Accept": "application/json" },
     "variables": { "Variable": "valor-no-secreto" },
     "authentication": {
       "type": "None|Bearer|Basic|ApiKey",
       "token": "{{AccessToken}}",
       "username": "{{Username}}",
       "password": "{{Password}}",
       "headerName": "X-Api-Key",
       "value": "{{ApiKey}}"
     },
     "timeoutMilliseconds": 30000
   }

   allowedHosts debe incluir exactamente el host de baseUrl.
   No inventes una URL si no fue proporcionada. Si falta toda base URL, detente y pídela antes de generar el JSON.
   No marques un ambiente como Production salvo que se haya identificado explícitamente.

6. Modules:
   {
     "id": "orders",
     "name": "Orders",
     "description": "...",
     "basePath": "/api",
     "tags": ["orders"],
     "defaultHeaders": { "Content-Type": "application/json" },
     "endpoints": [...]
   }

7. Endpoints:
   {
     "id": "create-order",
     "name": "Create order",
     "method": "GET|POST|PUT|PATCH|DELETE",
     "path": "/orders/{{OrderId}}",
     "description": "...",
     "tags": ["orders", "mutation"],
     "headers": {},
     "query": {},
     "payload": "order-create",
     "assertions": ["status-created", "order-id-present"],
     "saveVariables": [
       { "name": "OrderId", "source": "JsonPath", "path": "$.id", "required": true }
     ]
   }

   No uses request, form, expectedStatusCodes, jsonPath, statusCode, target,
   severity, outputs ni otras propiedades de contratos antiguos.
   Un endpoint usa payload O body inline, nunca ambos.
   No añadas endpoints prohibidos a escenarios automáticos.

8. Payloads:
   {
     "id": "order-create",
     "description": "...",
     "contentType": "application/json",
     "content": { "name": "{{OrderName}}" }
   }

9. Assertions soportadas:
   - Status:
     { "id": "status-ok", "type": "StatusCode", "expected": 200 }
   - JSONPath:
     { "id": "id-present", "type": "JsonPath", "path": "$.id", "operator": "NotNull" }
     { "id": "id-matches", "type": "JsonPath", "path": "$.id", "operator": "Equals", "expected": "{{OrderId}}" }
   - Header:
     { "id": "content-json", "type": "Header", "header": "Content-Type", "operator": "Contains", "expected": "application/json" }
   - Tiempo:
     { "id": "under-2s", "type": "MaxTime", "operator": "MaxTime", "maximumMilliseconds": 2000 }

   Operadores permitidos: Equals, Contains, Null, NotNull, MaxTime.
   JSONPath permitido: $, propiedades como $.data.id, índices como $[0] y wildcard como $.items[*].
   No generes regex, JSON Schema assertions, scripts ni JSONPath con filtros.

10. Scenarios:
    {
      "id": "authenticated-order-smoke",
      "name": "Authenticated order smoke",
      "description": "...",
      "tags": ["smoke", "auth"],
      "onFailure": "Stop|Continue",
      "variables": {},
      "steps": [
        {
          "id": "login",
          "execute": "login",
          "enabled": true,
          "assertions": [],
          "saveVariables": [],
          "dependsOn": []
        },
        {
          "id": "create",
          "execute": "create-order",
          "with": { "payload": "order-create" },
          "dependsOn": ["login"]
        }
      ]
    }

    La ejecución es secuencial. dependsOn no puede tener referencias ausentes ni ciclos.
    Los pasos posteriores deben consumir variables capturadas por pasos anteriores.

COBERTURA MÍNIMA
Si la documentación lo permite sin inventar comportamiento, incluye:
- un smoke de salud o lectura;
- un smoke autenticado: login -> captura token -> endpoint protegido;
- un CRUD seguro: create -> read -> update -> delete/cleanup;
- negativos para credenciales inválidas (401/403), validación inválida (400/422)
  y recurso inexistente (404), solo si status y contrato están documentados;
- assertions de status, campos críticos y Content-Type;
- tags smoke, regression, auth, negative, crud y destructive donde correspondan.

REGLAS DE SEGURIDAD Y CALIDAD
- Nunca pruebes mutaciones sobre Production por diseño.
- Nunca incluyas secretos ni datos personales reales.
- No inventes endpoints, campos, status, roles o respuestas.
- No uses código arbitrario ni scripts.
- Evita datos compartidos que vuelvan flaky el CRUD; usa variables de entrada o
  valores únicos proporcionados por el usuario.
- Incluye cleanup si existe; si no existe, no afirmes que el flujo es repetible.
- No reintentes POST, PUT, PATCH o DELETE.
- Toda variable requerida por un placeholder debe declararse o capturarse antes.
- Toda assertion, endpoint, payload, environment y dependencia referenciada debe existir.
- Un valor sensitive no puede aparecer como default ni en environments.
- Usa exactamente las propiedades descritas; no mezcles el formato fragmentado anterior.

PROCESO INTERNO ANTES DE RESPONDER
1. Extrae endpoints, auth, request/response y status documentados.
2. Marca información faltante y riesgos.
3. Diseña IDs y mapa de referencias.
4. Construye el JSON.
5. Haz una auditoría de referencias:
   environments, endpoints, payloads, assertions, variables y dependsOn.
6. Busca placeholders con regex conceptual {{...}} y confirma que cada uno tenga fuente.
7. Confirma que no haya secretos inline.
8. Confirma que el JSON sea sintácticamente válido y autosuficiente.
9. Confirma que todos los métodos sean GET, POST, PUT, PATCH o DELETE.
10. Confirma que cada allowedHosts contenga el host exacto de baseUrl.

FORMATO DE RESPUESTA
Responde en este orden:

A. "Suposiciones y bloqueos": lista breve. Si falta baseUrl, auth esencial o contrato
   suficiente para un plugin seguro, no inventes: formula preguntas concretas y no
   generes un plugin que finja estar listo.

B. "plugin.json": un único bloque ```json con el archivo COMPLETO. Debe empezar con {
   y terminar con }. No uses comentarios, TODO, elipsis ni placeholders editoriales
   como <<...>> dentro del JSON. Los únicos placeholders permitidos son variables
   runtime válidas con formato {{VariableName}}.

C. "Variables secretas para ejecutar": tabla con variable y nombre exacto:
   BTS_SECRET_<PLUGIN_ID_NORMALIZADO>_<VARIABLE_NORMALIZADA>
   donde todo va en mayúsculas y caracteres no alfanuméricos se vuelven _.
   No incluyas valores.

D. "Comandos de validación y primera corrida":
   dotnet run --project BackendTestingStudio.Cli -- validate --plugin /ruta/plugin.json
   dotnet run --project BackendTestingStudio.Cli -- list --plugin /ruta/plugin.json
   dotnet run --project BackendTestingStudio.Cli -- run --plugin /ruta/plugin.json
     --scenario <smoke-id> --environment <dev-o-staging-id>
     --json artifacts/run.json --html artifacts/run.html --junit artifacts/run.xml

E. "Cobertura y límites": endpoints incluidos/excluidos, cleanup, peligros conocidos
   y datos que aún debe proporcionar el usuario.

No digas que el plugin está listo si tu propia auditoría detecta una referencia rota,
un secreto inline, un endpoint inventado o información esencial ausente.
```

Después de recibir la respuesta, guarda únicamente el bloque JSON como `plugin.json` y ejecuta primero `validate`. No lleves el plugin a un ambiente real hasta revisar manualmente mutaciones, hosts permitidos, secretos y cleanup.
