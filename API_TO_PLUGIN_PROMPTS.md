# API To Plugin Prompt Pack

Esta serie de prompts sirve para tomar una API determinada y generar un plugin declarativo compatible con Backend Testing Studio.

Usa estos prompts en orden. La idea es no pedir "crea todo el plugin" de una vez, sino obligar al modelo a analizar, disenar, generar y validar por etapas.

## Contexto Fijo Para Todos Los Prompts

Copia este bloque al inicio de cada prompt si estas trabajando en una conversacion nueva:

```text
Estoy creando un plugin declarativo para Backend Testing Studio.

Reglas obligatorias:

- El plugin es una carpeta dentro de plugins/{plugin-id}/.
- El formato canonico es JSON.
- No se permite YAML.
- No se debe crear codigo C# especifico de la API.
- No se debe modificar el Core, HttpEngine, UI ni Storage.
- El plugin debe respetar PLUGIN_SPEC.md.
- El plugin debe respetar ARCHITECTURE.md y PROJECT_RULES.md.
- Los archivos y carpetas usan kebab-case.
- Los IDs usan kebab-case.
- Las variables usan PascalCase y se referencian como {{VariableName}}.
- Los secretos no se hardcodean.
- Bearer, Basic y ApiKey deben declararse en el environment cuando aplique.
- Los endpoints deben ser declarativos.
- Los payloads deben ser reutilizables.
- Los scenarios deben usar endpoints, payloads, variables y assertions del plugin.

Estructura requerida:

plugins/{plugin-id}/
├── plugin.json
├── variables.json
├── README.md
├── environments/
├── modules/
├── payloads/
├── assertions/
└── scenarios/

Estado actual del producto:

- El formato declarativo ya existe.
- Hay ejemplos en plugins/swagger-petstore y plugins/dummyjson.
- La carga/ejecucion automatica de plugins declarativos desde UI todavia esta pendiente.
- El plugin debe quedar listo para cuando el loader declarativo este conectado.
```

## Prompt 1: Analizar La API

Usa este prompt cuando tienes documentacion, Swagger/OpenAPI, Postman collection, curl examples o una descripcion manual de la API.

```text
Analiza esta API para convertirla en un plugin declarativo de Backend Testing Studio.

No generes archivos todavia.

Necesito que produzcas:

1. Resumen de la API.
2. Ambientes detectados.
3. Tipo de autenticacion.
4. Modulos funcionales sugeridos.
5. Endpoints por modulo.
6. Variables necesarias.
7. Payloads reutilizables necesarios.
8. Assertions recomendadas.
9. Scenarios recomendados.
10. Riesgos o datos faltantes.

Reglas:

- No inventes endpoints que no aparezcan en la documentacion.
- Si falta informacion, marcala como "pendiente".
- No hardcodees secretos.
- Usa nombres en kebab-case para IDs.
- Usa variables PascalCase con formato {{VariableName}}.
- Prioriza primero un flujo smoke pequeno y luego flujos CRUD o funcionales.

Documentacion de la API:

{PEGAR_AQUI_DOCUMENTACION_DE_LA_API}
```

Resultado esperado:

```text
Plugin candidate:
- id:
- name:
- defaultEnvironment:
- auth:

Modules:
- auth
- products
- users

Variables:
- AccessToken
- ProductId
- UserId

Scenarios:
- login-smoke
- products-smoke
- users-smoke
```

## Prompt 2: Disenar El Plugin

Usa este prompt despues del analisis. Sirve para cerrar el diseno antes de escribir JSON.

```text
Con base en el analisis anterior, disena el plugin declarativo completo para Backend Testing Studio.

No generes archivos todavia.

Entrega el diseno en estas secciones:

1. Nombre del plugin.
2. Plugin ID.
3. Version inicial.
4. Schema version.
5. Engine version.
6. Default environment.
7. Estructura exacta de carpetas y archivos.
8. Lista de variables con type, defaultValue, required, sensitive, scope y exportable.
9. Lista de environments.
10. Lista de modules.
11. Lista de endpoints por module.
12. Lista de payloads.
13. Lista de assertions.
14. Lista de scenarios.
15. Mapa de dependencias entre scenarios, endpoints, payloads, assertions y variables.

Reglas:

- No modifiques el contrato definido por PLUGIN_SPEC.md.
- No uses campos ambiguos.
- No mezcles dos fuentes de body para el mismo endpoint.
- Si un endpoint usa body, debe usar request.mode = "payload" y referenciar un payload existente.
- Cada assertion referenciada debe existir.
- Cada payload referenciado debe existir.
- Cada variable usada como {{VariableName}} debe estar definida.
- Cada endpoint usado en un scenario debe existir.
```

## Prompt 3: Generar La Estructura De Archivos

Usa este prompt cuando el diseno ya esta aprobado.

```text
Genera la estructura completa de archivos para el plugin declarativo de Backend Testing Studio.

No generes explicaciones largas.

Devuelve cada archivo con:

1. Ruta relativa.
2. Contenido completo.

Reglas:

- La carpeta raiz debe ser plugins/{plugin-id}/.
- Cada archivo JSON debe ser valido.
- No uses comentarios dentro de JSON.
- No uses YAML.
- No generes codigo C#.
- No modifiques archivos fuera de plugins/{plugin-id}/.
- Incluye plugin.json.
- Incluye variables.json.
- Incluye README.md.
- Incluye al menos un environment.
- Incluye modules.
- Incluye payloads si hay endpoints con body.
- Incluye assertions.
- Incluye scenarios.
- Los archivos deben seguir kebab-case.
- Los IDs deben seguir kebab-case.
- Las variables deben seguir PascalCase.

Diseno aprobado:

{PEGAR_AQUI_DISENO_APROBADO}
```

## Prompt 4: Generar Solo plugin.json

Usa este prompt si quieres construir el plugin archivo por archivo.

```text
Genera solamente plugins/{plugin-id}/plugin.json para Backend Testing Studio.

Reglas:

- Debe ser JSON valido.
- Debe incluir id, name, version, schemaVersion, engineVersion, author, description, defaultEnvironment y modules.
- Puede incluir tags, repositoryUrl y supportUrl.
- No incluyas entryPoint.
- El id debe coincidir con la carpeta.
- defaultEnvironment debe coincidir con un environment que se creara despues.
- modules debe listar IDs de modulos en kebab-case.

Datos del plugin:

{PEGAR_AQUI_DATOS_DEL_PLUGIN}
```

## Prompt 5: Generar variables.json

```text
Genera solamente plugins/{plugin-id}/variables.json para Backend Testing Studio.

Reglas:

- Debe ser JSON valido.
- Debe ser un arreglo de variables.
- Cada variable debe tener name, type, defaultValue, required, sensitive, description, scope y exportable.
- Los secretos deben tener sensitive: true.
- No hardcodees tokens, passwords ni API keys reales.
- Las variables que vienen de respuestas deben tener scope Runtime o Scenario segun aplique.
- Los nombres de variables deben estar en PascalCase.

Variables necesarias:

{PEGAR_AQUI_LISTA_DE_VARIABLES}
```

## Prompt 6: Generar environments

```text
Genera los archivos JSON dentro de plugins/{plugin-id}/environments/ para Backend Testing Studio.

Reglas:

- Cada archivo debe ser JSON valido.
- Cada environment debe tener id, name, baseUrl y authentication.
- Headers comunes deben ir en headers.
- Secrets deben referenciar variables con {{VariableName}}.
- No hardcodees secretos reales.
- Usa Bearer, Basic, ApiKey o None segun aplique.
- Si hay ApiKey, define name, value y location.
- Si hay Bearer, define token.
- Si hay Basic, define username y password.

Ambientes y autenticacion:

{PEGAR_AQUI_AMBIENTES_Y_AUTH}
```

## Prompt 7: Generar modules Y endpoints

```text
Genera los archivos JSON dentro de plugins/{plugin-id}/modules/ para Backend Testing Studio.

Reglas:

- Cada module debe tener id, name y endpoints.
- Cada endpoint debe tener id, method, path y name.
- method solo puede ser GET, POST, PUT, PATCH o DELETE.
- Los path parameters deben usar {{VariableName}}.
- Query parameters deben declararse en query.
- Headers especificos deben declararse en headers.
- Si el endpoint usa body, debe definir request.mode = "payload" y payload = "{payload-id}".
- No mezcles payload, form e inline body en el mismo endpoint.
- expectedStatusCodes debe incluir los status correctos.
- assertions debe referenciar IDs existentes o planeados.
- saveVariables debe usar JsonPath cuando se capture informacion de la respuesta.

Modulos y endpoints:

{PEGAR_AQUI_TABLA_DE_ENDPOINTS}
```

## Prompt 8: Generar payloads

```text
Genera los archivos JSON dentro de plugins/{plugin-id}/payloads/ para Backend Testing Studio.

Reglas:

- Cada payload debe ser JSON valido.
- Cada payload debe tener id, name, description, contentType, tags, variables y body.
- contentType debe ser application/json salvo que la API requiera otro formato.
- Usa {{VariableName}} para valores dinamicos.
- No hardcodees secretos.
- No dupliques payloads equivalentes.

Payloads requeridos:

{PEGAR_AQUI_PAYLOADS_REQUERIDOS}
```

## Prompt 9: Generar assertions

```text
Genera los archivos JSON dentro de plugins/{plugin-id}/assertions/ para Backend Testing Studio.

Reglas:

- Cada assertion debe ser JSON valido.
- Cada assertion debe tener id, type, target, expected, description, severity y message.
- Usa tipos soportados: StatusCode, JSONPath, Headers, Equals, Contains, Null, NotNull, MaxTime.
- Cada assertion debe ser reutilizable cuando tenga sentido.
- No crees assertions fragiles contra valores que cambian constantemente.
- Usa {{VariableName}} cuando la expectation dependa de una variable.

Assertions requeridas:

{PEGAR_AQUI_ASSERTIONS_REQUERIDAS}
```

## Prompt 10: Generar scenarios

```text
Genera los archivos JSON dentro de plugins/{plugin-id}/scenarios/ para Backend Testing Studio.

Reglas:

- Cada scenario debe ser JSON valido.
- Cada scenario debe tener id, name, description, tags, onFailure, variables, steps y outputs.
- onFailure debe ser Stop cuando los steps dependan entre si.
- Cada step debe usar execute con un endpoint existente.
- Cada step debe declarar assertions existentes.
- Cada step puede usar with para pasar variables.
- Cada step puede usar saveVariables con JsonPath.
- No uses endpoints que no existan.
- No uses payloads que no existan.
- No uses assertions que no existan.
- No uses variables no definidas.

Scenarios requeridos:

{PEGAR_AQUI_SCENARIOS_REQUERIDOS}
```

## Prompt 11: Generar README Del Plugin

```text
Genera plugins/{plugin-id}/README.md para documentar el plugin.

Debe incluir:

1. Nombre del plugin.
2. Descripcion corta.
3. Base URL.
4. Tipo de autenticacion.
5. Modulos.
6. Endpoints principales.
7. Scenarios.
8. Variables importantes.
9. Notas de seguridad.
10. Limitaciones conocidas de la API.

Reglas:

- No incluyas secretos reales.
- Explica si la API simula escrituras.
- Explica si la API es publica y puede cambiar datos.
- Mantén el README practico para un usuario que quiere ejecutar pruebas.

Datos del plugin:

{PEGAR_AQUI_RESUMEN_DEL_PLUGIN}
```

## Prompt 12: Validar El Plugin Generado

Usa este prompt cuando ya tienes todos los archivos.

```text
Valida este plugin declarativo de Backend Testing Studio.

No generes archivos nuevos todavia.

Revisa:

1. JSON valido.
2. Estructura de carpetas requerida.
3. plugin.json completo.
4. variables.json completo.
5. environments completos.
6. modules completos.
7. endpoints completos.
8. payloads referenciados existentes.
9. assertions referenciadas existentes.
10. endpoints usados por scenarios existentes.
11. variables {{VariableName}} definidas.
12. secrets no hardcodeados.
13. IDs en kebab-case.
14. variables en PascalCase.
15. compatibilidad con PLUGIN_SPEC.md.

Entrega:

- Errores bloqueantes.
- Advertencias.
- Correcciones recomendadas.
- Lista final de archivos validos.

Plugin:

{PEGAR_AQUI_ARBOL_Y_CONTENIDO_DE_ARCHIVOS}
```

## Prompt 13: Corregir El Plugin

```text
Corrige el plugin declarativo de Backend Testing Studio con base en esta validacion.

Reglas:

- Devuelve solamente los archivos que cambian.
- Cada archivo debe incluir ruta relativa y contenido completo.
- No cambies decisiones correctas.
- No agregues endpoints inventados.
- No hardcodees secretos.
- Mantén compatibilidad con PLUGIN_SPEC.md.

Validacion:

{PEGAR_AQUI_VALIDACION}

Archivos actuales:

{PEGAR_AQUI_ARCHIVOS_ACTUALES}
```

## Prompt 14: Crear Un Plugin Smoke Primero

Este prompt es util cuando la API es grande y conviene empezar pequeno.

```text
Crea una primera version smoke del plugin declarativo para Backend Testing Studio.

Objetivo:

- Solo incluir el minimo necesario para comprobar que la API responde y que la autenticacion funciona.

Debe incluir:

1. plugin.json.
2. variables.json.
3. Un environment.
4. Un modulo auth si aplica.
5. Un modulo principal.
6. Maximo 3 endpoints.
7. Payloads minimos.
8. Assertions minimas.
9. Un scenario smoke.
10. README.md.

Reglas:

- No conviertas toda la API todavia.
- No inventes endpoints.
- No hardcodees secretos.
- Usa IDs en kebab-case.
- Usa variables PascalCase.
- Entrega cada archivo con ruta relativa y contenido completo.

API:

{PEGAR_AQUI_DOCUMENTACION_DE_LA_API}
```

## Prompt 15: Expandir Un Plugin Existente

```text
Expande este plugin declarativo existente de Backend Testing Studio.

Objetivo:

- Agregar nuevos endpoints y scenarios sin romper lo que ya existe.

Reglas:

- No cambies IDs existentes salvo que haya un error bloqueante.
- No elimines variables usadas.
- No elimines assertions usadas.
- No dupliques payloads.
- Nuevos archivos deben usar kebab-case.
- Nuevas variables deben usar PascalCase.
- Mantén compatibilidad con PLUGIN_SPEC.md.
- Devuelve solamente archivos nuevos o modificados.

Plugin actual:

{PEGAR_AQUI_ARBOL_Y_ARCHIVOS_ACTUALES}

Nueva documentacion de API:

{PEGAR_AQUI_NUEVOS_ENDPOINTS_O_FLUJOS}
```

## Prompt Maestro: Generar Plugin Completo Desde Una API

Usa este prompt solo cuando la API es pequena o la documentacion esta muy clara.

```text
Genera un plugin declarativo completo para Backend Testing Studio a partir de esta API.

Contexto:

- El plugin debe vivir en plugins/{plugin-id}/.
- El formato canonico es JSON.
- No se permite YAML.
- No se permite codigo C# especifico de la API.
- No se debe modificar Core, UI, HttpEngine ni Storage.
- El plugin debe respetar PLUGIN_SPEC.md, ARCHITECTURE.md y PROJECT_RULES.md.

Entregables:

1. Arbol de archivos.
2. plugin.json.
3. variables.json.
4. environments/*.json.
5. modules/*.json.
6. payloads/*.json.
7. assertions/*.json.
8. scenarios/*.json.
9. README.md.
10. Checklist de validacion.

Reglas de generacion:

- Cada archivo JSON debe ser valido.
- No uses comentarios dentro de JSON.
- Carpetas y archivos en kebab-case.
- IDs en kebab-case.
- Variables en PascalCase.
- Variables se referencian como {{VariableName}}.
- No hardcodees secretos.
- Si hay auth, declarala en environment.
- Si un endpoint tiene body, usa request.mode = "payload".
- Cada payload referenciado debe existir.
- Cada assertion referenciada debe existir.
- Cada endpoint usado en scenarios debe existir.
- Cada variable usada debe estar definida.
- Crea primero un scenario smoke.
- Crea scenarios funcionales solo si los endpoints estan claros.
- Si falta informacion, no inventes: marca pendiente.

API:

{PEGAR_AQUI_DOCUMENTACION_DE_LA_API}
```

## Prompt Para OpenAPI O Swagger

```text
Convierte esta especificacion OpenAPI/Swagger en un plugin declarativo de Backend Testing Studio.

No generes codigo C#.
No generes cliente HTTP.
No generes YAML.

Debes producir:

1. Modulos agrupados por tags de OpenAPI.
2. Endpoints por operationId o path.
3. Variables para path params, query params y secrets.
4. Payloads desde requestBody examples o schemas simples.
5. Assertions desde responses esperadas.
6. Scenarios smoke por modulo.
7. Un README del plugin.

Reglas:

- Usa operationId como base del endpoint id cuando exista.
- Si no hay operationId, genera IDs en kebab-case desde method + path.
- Usa servers[0].url como baseUrl si existe.
- Si hay securitySchemes, conviertelos a authentication declarativa.
- No incluyas schemas enormes si no son necesarios para payloads ejecutables.
- Si un schema es complejo, crea un payload minimo valido.

OpenAPI/Swagger:

{PEGAR_AQUI_OPENAPI}
```

## Prompt Para Postman Collection

```text
Convierte esta Postman Collection en un plugin declarativo de Backend Testing Studio.

No generes codigo.
No generes YAML.

Debes mapear:

- Folders de Postman a modules.
- Requests a endpoints.
- Collection variables a variables.json.
- Environment variables a environments/*.json.
- Body raw JSON a payloads.
- Tests de Postman a assertions cuando sea posible.
- Flujos relacionados a scenarios.

Reglas:

- No copies secretos reales.
- Reemplaza secrets por {{VariableName}}.
- Mantén IDs en kebab-case.
- Mantén variables en PascalCase.
- Si un test de Postman no se puede convertir, reportalo como pendiente.

Postman Collection:

{PEGAR_AQUI_COLLECTION}
```

## Prompt Para cURL

```text
Convierte estos ejemplos cURL en un plugin declarativo de Backend Testing Studio.

No generes codigo.
No generes YAML.

Debes inferir:

1. Environment baseUrl.
2. Authentication.
3. Modules.
4. Endpoints.
5. Headers.
6. Query params.
7. Payloads.
8. Variables.
9. Assertions minimas.
10. Scenario smoke.

Reglas:

- No mantengas tokens literales del cURL.
- Reemplaza Authorization por variables.
- Reemplaza IDs del path por variables.
- Crea payloads reutilizables para bodies JSON.
- Si un dato no se puede inferir con seguridad, marcalo pendiente.

cURL examples:

{PEGAR_AQUI_CURLS}
```

## Prompt De Revision Final Antes De Commit

```text
Haz una revision final del plugin declarativo antes de commit.

Modo review:

- Prioriza errores, riesgos, inconsistencias y referencias rotas.
- No hagas resumen largo.
- Si no encuentras problemas, dilo explicitamente.

Revisa:

- Contrato PLUGIN_SPEC.md.
- Estructura requerida.
- JSON valido.
- IDs duplicados.
- Variables faltantes.
- Payloads faltantes.
- Assertions faltantes.
- Endpoints faltantes.
- Secrets hardcodeados.
- Scenarios que no pueden ejecutarse.
- Auth mal declarada.
- Status codes incorrectos.
- JSONPath probablemente incorrecto.

Plugin:

{PEGAR_AQUI_ARBOL_Y_ARCHIVOS}
```

## Como Usar Esta Serie En La Practica

Flujo recomendado:

1. Ejecuta Prompt 1 para entender la API.
2. Ejecuta Prompt 2 para cerrar el diseno.
3. Ejecuta Prompt 14 si quieres una primera version smoke.
4. Ejecuta Prompts 4 a 11 si prefieres generar archivo por archivo.
5. Ejecuta Prompt 12 para validar.
6. Ejecuta Prompt 13 para corregir.
7. Ejecuta el Prompt de Revision Final.
8. Crea commit.

Para APIs pequenas, puedes usar el Prompt Maestro.

Para APIs grandes, no uses el Prompt Maestro al inicio. Divide por modulo y usa Prompt 15 para expandir progresivamente.

## Informacion Que Debes Pedir Si Falta

Si la documentacion de la API no incluye estos datos, pidelos antes de generar el plugin final:

- Base URL real.
- Ambiente sandbox o staging.
- Tipo de auth.
- Credenciales de prueba o forma de generar token.
- Endpoints principales.
- Ejemplos de request body.
- Ejemplos de response body.
- Status codes esperados.
- Reglas de negocio para validar.
- Si los endpoints mutan datos reales.
- Si hay rate limits.

## Salida Ideal Del Modelo

Cuando el modelo genere archivos, debe responder asi:

````text
Ruta: plugins/mi-api/plugin.json

```json
{
  "id": "mi-api"
}
```

Ruta: plugins/mi-api/variables.json

```json
[]
```
````

Cada archivo debe venir completo. No deben venir fragmentos incompletos.

## Advertencias

- No aceptes plugins con secretos reales.
- No aceptes JSON con comentarios.
- No aceptes endpoints inventados.
- No aceptes payloads que contradigan la documentacion de la API.
- No aceptes scenarios que dependan de datos que la API publica puede borrar sin control.
- No aceptes cambios al motor para "hacer funcionar" una API concreta.
