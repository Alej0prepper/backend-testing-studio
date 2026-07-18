# Guia Para Crear Un Plugin Desde Una API

Esta guia explica como convertir una API existente en un plugin declarativo de Backend Testing Studio.

Un plugin en este proyecto es una carpeta con archivos JSON. No debe contener codigo C# especifico de la API. El motor debe poder leer el plugin, entender endpoints, payloads, variables, assertions y escenarios sin conocer la API por dentro.

## Antes De Empezar

Necesitas tener clara esta informacion de la API:

- URL base de cada ambiente.
- Metodo y path de cada endpoint.
- Headers requeridos.
- Tipo de autenticacion.
- Query parameters.
- Path parameters.
- Body esperado.
- Respuestas esperadas.
- Codigos HTTP correctos.
- Flujos funcionales reales.
- Valores que se deben guardar entre pasos.

Ejemplo:

```text
API: DummyJSON
Base URL: https://dummyjson.com
Auth: POST /auth/login devuelve accessToken
Modulo auth: login y usuario actual
Modulo products: listar, consultar, buscar y crear simulado
Modulo carts: carritos
Modulo users: usuarios
```

## Regla Principal

El plugin debe respetar:

- `PLUGIN_SPEC.md`
- `ARCHITECTURE.md`
- `PROJECT_RULES.md`

Reglas que no se pueden romper:

- El plugin es declarativo.
- El plugin vive en una carpeta dentro de `plugins/`.
- El plugin no modifica el Core.
- El plugin no llama directamente a `HttpClient`.
- El plugin no conoce implementaciones internas del motor.
- Los secretos no se hardcodean en endpoints ni payloads.
- Las variables usan `{{VariableName}}`.

## Estructura Obligatoria

Crea una carpeta con kebab-case:

```text
plugins/mi-api/
├── plugin.json
├── variables.json
├── README.md
├── environments/
│   └── mi-api-live.json
├── modules/
│   └── recurso.json
├── payloads/
│   └── crear-recurso.json
├── assertions/
│   └── status-ok.json
└── scenarios/
    └── flujo-basico.json
```

Convenciones:

- Carpetas en kebab-case.
- Archivos en kebab-case.
- IDs estables en kebab-case.
- Variables en PascalCase: `{{Token}}`, `{{ProductId}}`, `{{UserId}}`.
- No usar YAML. El contrato canonico es JSON.

## Paso 1: Analizar La API

Haz una tabla antes de escribir JSON.

Ejemplo:

```text
Modulo      Endpoint              Metodo   Auth      Guarda variable
auth        /auth/login           POST     No        AccessToken
products    /products             GET      No        No
products    /products/{{Id}}      GET      No        No
carts       /carts/add            POST     Bearer    CartId
```

Decide que partes seran:

- Modulos.
- Endpoints.
- Payloads.
- Variables.
- Assertions.
- Escenarios.

No conviertas toda la API de una vez si es grande. Empieza por un flujo pequeno que pruebe valor real.

## Paso 2: Crear plugin.json

`plugin.json` identifica el plugin y lista sus modulos.

Ejemplo:

```json
{
  "id": "mi-api",
  "name": "Mi API",
  "version": "1.0.0",
  "schemaVersion": "1.0.0",
  "engineVersion": "0.16.0",
  "author": "Backend Testing Studio",
  "description": "Plugin declarativo para probar Mi API.",
  "defaultEnvironment": "mi-api-live",
  "modules": [
    "auth",
    "products"
  ],
  "tags": [
    "sample",
    "backend",
    "api-testing"
  ],
  "repositoryUrl": "https://example.com",
  "supportUrl": "https://example.com/docs"
}
```

Checklist:

- `id` coincide con la carpeta.
- `defaultEnvironment` coincide con el archivo de environment.
- Cada item de `modules` tiene un archivo en `modules/`.
- `version` usa semver.
- `schemaVersion` coincide con el contrato soportado.

## Paso 3: Crear variables.json

`variables.json` define valores reutilizables y valores que pueden capturarse en runtime.

Ejemplo:

```json
[
  {
    "name": "BaseUrl",
    "type": "String",
    "defaultValue": "https://api.example.com",
    "required": true,
    "sensitive": false,
    "description": "URL base de la API.",
    "scope": "Environment",
    "exportable": false
  },
  {
    "name": "AccessToken",
    "type": "String",
    "defaultValue": "",
    "required": false,
    "sensitive": true,
    "description": "Token devuelto por el login.",
    "scope": "Runtime",
    "exportable": false
  },
  {
    "name": "ProductId",
    "type": "Number",
    "defaultValue": "1",
    "required": true,
    "sensitive": false,
    "description": "Producto usado en pruebas.",
    "scope": "Scenario",
    "exportable": true
  }
]
```

Buenas practicas:

- Marca tokens, passwords y API keys como `sensitive: true`.
- No pongas secretos reales como `defaultValue`.
- Usa nombres claros y estables.
- Define variables runtime para IDs que salen de respuestas.

## Paso 4: Crear environments

Cada environment define un destino de ejecucion.

Archivo:

```text
plugins/mi-api/environments/mi-api-live.json
```

Ejemplo con Bearer:

```json
{
  "id": "mi-api-live",
  "name": "Mi API Live",
  "baseUrl": "https://api.example.com",
  "authentication": {
    "type": "Bearer",
    "token": "{{AccessToken}}"
  },
  "headers": {
    "Accept": "application/json"
  },
  "timeout": 30000,
  "variables": {
    "ProductId": "1"
  },
  "notes": "Ambiente publico de pruebas."
}
```

Ejemplo con ApiKey:

```json
{
  "id": "mi-api-live",
  "name": "Mi API Live",
  "baseUrl": "https://api.example.com",
  "authentication": {
    "type": "ApiKey",
    "name": "x-api-key",
    "value": "{{ApiKey}}",
    "location": "Header"
  },
  "headers": {
    "Accept": "application/json"
  }
}
```

Ejemplo con Basic:

```json
{
  "id": "mi-api-live",
  "name": "Mi API Live",
  "baseUrl": "https://api.example.com",
  "authentication": {
    "type": "Basic",
    "username": "{{Username}}",
    "password": "{{Password}}"
  },
  "headers": {
    "Accept": "application/json"
  }
}
```

Checklist:

- `baseUrl` no debe terminar en path de endpoint especifico.
- Los secretos se referencian con variables.
- Headers comunes van aqui.
- Headers especificos del endpoint van en el modulo o endpoint.

## Paso 5: Crear Modulos

Un modulo agrupa endpoints por area funcional.

Archivo:

```text
plugins/mi-api/modules/products.json
```

Ejemplo:

```json
{
  "id": "products",
  "name": "Products",
  "description": "Endpoints del catalogo de productos.",
  "basePath": "",
  "tags": [
    "products",
    "catalog"
  ],
  "defaultHeaders": {
    "Content-Type": "application/json",
    "Accept": "application/json"
  },
  "endpoints": [
    {
      "id": "list-products",
      "method": "GET",
      "path": "/products",
      "name": "List products",
      "description": "Lista productos con paginacion.",
      "module": "products",
      "query": {
        "limit": "{{Limit}}",
        "skip": "{{Skip}}"
      },
      "assertions": [
        "status-ok"
      ],
      "expectedStatusCodes": [
        200
      ]
    },
    {
      "id": "get-product",
      "method": "GET",
      "path": "/products/{{ProductId}}",
      "name": "Get product",
      "description": "Consulta un producto por ID.",
      "module": "products",
      "assertions": [
        "status-ok",
        "product-id-matches"
      ],
      "expectedStatusCodes": [
        200
      ]
    }
  ]
}
```

Checklist:

- Cada endpoint tiene `id`, `method`, `path` y `name`.
- `method` usa GET, POST, PUT, PATCH o DELETE.
- Las variables van con `{{VariableName}}`.
- Los endpoints referencian assertions existentes.
- Si el endpoint envia body, referencia un payload.

## Paso 6: Crear Payloads

Los payloads son bodies reutilizables.

Archivo:

```text
plugins/mi-api/payloads/product-add.json
```

Ejemplo:

```json
{
  "id": "product-add",
  "name": "Add product",
  "description": "Payload para crear un producto.",
  "contentType": "application/json",
  "tags": [
    "products",
    "create"
  ],
  "variables": [
    "ProductTitle",
    "ProductPrice"
  ],
  "body": {
    "title": "{{ProductTitle}}",
    "price": "{{ProductPrice}}"
  }
}
```

Luego el endpoint lo referencia asi:

```json
{
  "id": "add-product",
  "method": "POST",
  "path": "/products/add",
  "name": "Add product",
  "module": "products",
  "request": {
    "mode": "payload",
    "payload": "product-add"
  },
  "payload": "product-add",
  "assertions": [
    "status-created",
    "product-title-matches"
  ],
  "expectedStatusCodes": [
    201
  ]
}
```

Buenas practicas:

- Un payload debe representar una intencion clara.
- No dupliques el mismo JSON con nombres distintos.
- Usa variables para valores que cambian.
- Mantén ejemplos pequenos y ejecutables.

## Paso 7: Crear Assertions

Las assertions validan respuestas sin acoplarse a una API concreta.

Archivo:

```text
plugins/mi-api/assertions/status-ok.json
```

Ejemplo status:

```json
{
  "id": "status-ok",
  "type": "StatusCode",
  "target": "status",
  "expected": 200,
  "description": "La operacion debe retornar HTTP 200.",
  "severity": "Error",
  "message": "Se esperaba HTTP 200.",
  "statusCode": 200
}
```

Ejemplo JSONPath:

```json
{
  "id": "product-id-matches",
  "type": "JSONPath",
  "target": "$.id",
  "expected": "{{ProductId}}",
  "description": "El ID del producto debe coincidir.",
  "severity": "Error",
  "message": "El producto devuelto no coincide con ProductId."
}
```

Ejemplo Header:

```json
{
  "id": "content-type-json",
  "type": "Headers",
  "target": "Content-Type",
  "expected": "application/json",
  "description": "La respuesta debe ser JSON.",
  "severity": "Warning",
  "message": "Content-Type no indica JSON."
}
```

Tipos soportados por el motor:

- `StatusCode`
- `JSONPath`
- `Headers`
- `Equals`
- `Contains`
- `Null`
- `NotNull`
- `MaxTime`

Checklist:

- Toda assertion referenciada por endpoint o scenario existe.
- Los mensajes explican el fallo.
- `severity` diferencia errores bloqueantes de advertencias.
- No uses assertions que dependan de datos inestables si la API publica cambia mucho.

## Paso 8: Guardar Variables Desde Respuestas

Si un endpoint crea un recurso, guarda el ID para usarlo despues.

Ejemplo:

```json
{
  "id": "add-product",
  "method": "POST",
  "path": "/products/add",
  "name": "Add product",
  "module": "products",
  "request": {
    "mode": "payload",
    "payload": "product-add"
  },
  "saveVariables": [
    {
      "name": "ProductId",
      "source": "JsonPath",
      "jsonPath": "$.id",
      "required": true
    }
  ],
  "assertions": [
    "status-created",
    "product-id-matches"
  ]
}
```

Reglas:

- Si `required` es `true` y no se encuentra el valor, el step debe fallar.
- La variable guardada puede usarse despues como `{{ProductId}}`.
- No guardes tokens o secretos como exportables.

## Paso 9: Crear Escenarios

Un escenario conecta endpoints en un flujo.

Archivo:

```text
plugins/mi-api/scenarios/products-flow.json
```

Ejemplo:

```json
{
  "id": "products-flow",
  "name": "Products flow",
  "description": "Lista productos, consulta uno y crea un producto simulado.",
  "tags": [
    "products",
    "smoke"
  ],
  "onFailure": "Stop",
  "variables": {
    "Limit": "10",
    "Skip": "0",
    "ProductId": "1",
    "ProductTitle": "Backend Testing Studio Product",
    "ProductPrice": "99"
  },
  "steps": [
    {
      "execute": "list-products",
      "with": {
        "Limit": "10",
        "Skip": "0"
      },
      "assertions": [
        "status-ok"
      ],
      "enabled": true
    },
    {
      "execute": "get-product",
      "with": {
        "ProductId": "{{ProductId}}"
      },
      "assertions": [
        "status-ok",
        "product-id-matches"
      ],
      "enabled": true
    },
    {
      "execute": "add-product",
      "with": {
        "payload": "product-add"
      },
      "saveVariables": [
        {
          "name": "ProductId",
          "source": "JsonPath",
          "jsonPath": "$.id",
          "required": true
        }
      ],
      "assertions": [
        "status-created",
        "product-id-matches"
      ],
      "enabled": true
    }
  ],
  "outputs": [
    "ProductId"
  ]
}
```

Buenas practicas:

- Crea primero escenarios smoke.
- Usa `onFailure: "Stop"` para flujos dependientes.
- No mezcles demasiadas areas funcionales en un solo escenario.
- Los escenarios deben poder repetirse sin romper datos reales.
- Si la API modifica datos reales, usa ambientes sandbox.

## Paso 10: Crear README Del Plugin

Cada plugin debe explicar como se usa.

Archivo:

```text
plugins/mi-api/README.md
```

Plantilla:

```markdown
# Mi API Plugin

Plugin declarativo para Mi API.

## Environment

- Base URL: `https://api.example.com`
- Auth: Bearer token desde `POST /auth/login`

## Modules

- `auth`: login y usuario actual.
- `products`: catalogo de productos.

## Scenarios

- `products-flow`: lista, consulta y crea un producto.

## Variables

- `AccessToken`
- `ProductId`
- `ProductTitle`

## Notes

Este plugin debe ejecutarse contra el ambiente sandbox.
```

## Paso 11: Validar Referencias

Antes de considerar terminado el plugin, revisa:

- `plugin.json.modules` apunta a archivos existentes en `modules/`.
- Cada endpoint referenciado por un scenario existe.
- Cada payload referenciado por un endpoint existe.
- Cada assertion referenciada por endpoint o scenario existe.
- Cada variable usada con `{{Variable}}` existe en `variables.json`, environment o scenario.
- Cada `defaultEnvironment` existe en `environments/`.
- No hay secretos reales en JSON.
- Los IDs son unicos dentro del plugin.

## Paso 12: Probar Manualmente La API

Antes de cargar el plugin en el producto, prueba los endpoints base con API Explorer:

1. Crea el Environment con la BaseUrl.
2. Configura autenticacion si aplica.
3. Ejecuta login si aplica.
4. Copia el token a una variable.
5. Prueba cada endpoint principal.
6. Confirma status codes.
7. Confirma JSONPath de variables capturadas.
8. Ajusta payloads y assertions.

Esto evita crear escenarios sobre endpoints que todavia no estan verificados.

## Ejemplo De Flujo Completo

Para una API de productos:

```text
1. Crear plugins/products-api/
2. Crear plugin.json
3. Crear variables.json con ProductId, Token y valores del payload
4. Crear environments/products-api-live.json
5. Crear modules/auth.json si hay login
6. Crear modules/products.json
7. Crear payloads/product-create.json
8. Crear assertions/status-ok.json
9. Crear assertions/product-id-matches.json
10. Crear scenarios/products-crud-flow.json
11. Crear README.md del plugin
12. Validar referencias
13. Probar endpoints en API Explorer
14. Ejecutar tests del proyecto
```

## Como Nombrar Archivos E IDs

Correcto:

```text
plugins/swagger-petstore/
modules/pet.json
payloads/pet-create.json
assertions/pet-id-matches.json
scenarios/pet-crud-lifecycle.json
```

Incorrecto:

```text
plugins/SwaggerPetStore/
modules/PetModule.json
payloads/createPet.json
assertions/check_pet_id.json
```

Regla:

- Carpetas y archivos: kebab-case.
- Variables: PascalCase.
- IDs: kebab-case.
- Nombres visibles: texto normal claro.

## Checklist Final

Usa esta lista antes de hacer commit:

```text
[ ] El plugin esta dentro de plugins/{plugin-id}/
[ ] Existe plugin.json
[ ] Existe variables.json
[ ] Existe README.md
[ ] Existe al menos un environment
[ ] Existe al menos un modulo
[ ] Existe al menos un endpoint
[ ] Existe al menos un payload si hay POST/PUT/PATCH con body
[ ] Existe al menos una assertion por endpoint importante
[ ] Existe al menos un scenario funcional
[ ] No hay secretos hardcodeados
[ ] Todas las variables {{Variable}} estan definidas
[ ] Todos los payloads referenciados existen
[ ] Todas las assertions referenciadas existen
[ ] Todos los endpoints usados en scenarios existen
[ ] El plugin respeta PLUGIN_SPEC.md
[ ] El plugin tiene README propio
```

## Commit Recomendado

Si el plugin es nuevo:

```bash
git add plugins/mi-api
git commit -m "Create Mi API plugin"
```

Si agregas mas endpoints a un plugin existente:

```bash
git add plugins/mi-api
git commit -m "Add Mi API product endpoints"
```

## Nota Sobre El Estado Actual Del Producto

El formato declarativo ya existe en el repositorio y hay ejemplos completos en:

- `plugins/swagger-petstore/`
- `plugins/dummyjson/`

Pero la carga y ejecucion automatica de plugins declarativos desde la UI todavia esta pendiente. Mientras tanto, esta guia sirve para crear plugins compatibles con el contrato objetivo del producto y listos para integrarse cuando el loader declarativo quede conectado.
