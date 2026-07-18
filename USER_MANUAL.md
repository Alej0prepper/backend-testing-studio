# Manual de usuario - Backend Testing Studio

## 1. Objetivo de este manual

Este manual explica cómo levantar Backend Testing Studio, comprobar que la interfaz está funcionando y probar una API desde cero.

También separa claramente:

1. Lo que ya puede hacerse desde la interfaz.
2. Lo que existe solamente como motor o librería.
3. Lo que todavía no está implementado.

La aplicación está en desarrollo. Tener un motor implementado no significa necesariamente que ya exista una pantalla para usarlo.

## 2. Estado funcional actual

| Capacidad | Estado | Disponible desde la UI |
| --- | --- | --- |
| Dashboard y navegación | Implementado | Sí |
| Ambientes | Implementado | Sí |
| Variables de ambiente | Implementado | Sí |
| Bearer, Basic y ApiKey | Implementado | Sí |
| API Explorer | Implementado | Sí |
| GET, POST, PUT, PATCH y DELETE | Implementado | Sí |
| Headers y query parameters | Implementado | Sí |
| Body JSON | Implementado | Sí |
| Multipart | Implementado con campos de texto/binarios básicos | Sí |
| Historial SQLite | Implementado | Sí |
| Repetir un request | Implementado | Sí |
| Biblioteca de payloads JSON | Implementado | Sí |
| Resolución de `{{Variables}}` | Implementado | Sí |
| Catálogo de plugins | Implementado parcialmente | Solo consulta |
| Cargar plugins desde carpetas JSON | No implementado | No |
| Assertions | Motor implementado | No |
| Scenario Engine | Motor implementado | No |
| Crear y ejecutar escenarios | No conectado a la UI | No |
| Reports | Planificado | No |
| Settings | Planificado | No |

## 3. Requisitos

Se necesita:

1. .NET SDK 10.
2. Acceso a la API que se desea probar.
3. Un navegador moderno.
4. Acceso de escritura a la carpeta de ejecución para crear SQLite.

Comprobar .NET:

```bash
dotnet --version
```

La versión debe comenzar por `10.`.

## 4. Levantar la aplicación

Desde la raíz del repositorio:

```bash
./run.sh
```

También puede usarse:

```bash
dotnet run --project BackendTestingStudio.UI --launch-profile http
```

El puerto es dinámico. Hay que buscar en la terminal una línea similar a:

```text
Now listening on: http://127.0.0.1:34643
```

Abrir exactamente esa dirección en el navegador.

Para detener la aplicación, volver a la terminal y presionar `Ctrl+C`.

### 4.1 Cómo saber si Blazor cargó correctamente

La página no solo debe verse con estilos. Los botones también deben responder.

Comprobación rápida:

1. Abrir `Environments`.
2. Presionar `Add variable`.
3. Debe aparecer otra fila inmediatamente.
4. Presionar `Reset`.
5. Los campos deben volver a su estado inicial.

Si los botones no hacen nada y la terminal muestra un 404 para `/_framework/blazor.web.js`, revisar la sección de solución de problemas.

## 5. Recorrido rápido: primer GET

Este ejercicio usa JSONPlaceholder, una API pública de prueba. Si no hay conexión a Internet, se puede sustituir por cualquier API local.

### Paso 1: crear el ambiente

Abrir `Environments` y completar:

| Campo | Valor |
| --- | --- |
| Name | `JSONPlaceholder` |
| Base URL | `https://jsonplaceholder.typicode.com` |
| Authentication | `None` |

En `Variables`, agregar:

| Name | Value |
| --- | --- |
| `PostId` | `1` |

En `Headers`, agregar:

| Name | Value |
| --- | --- |
| `Accept` | `application/json` |

Presionar `Save environment`.

El ambiente debe aparecer en `Environment list`.

### Paso 2: construir el request

Abrir `API Explorer` y completar:

| Campo | Valor |
| --- | --- |
| Environment | `JSONPlaceholder` |
| Method | `Get` |
| URL | `/posts/{{PostId}}` |
| Body | `None` |

La URL se construye combinando:

```text
https://jsonplaceholder.typicode.com + /posts/{{PostId}}
```

Antes de enviar, el motor reemplaza `{{PostId}}` por `1`.

URL final:

```text
https://jsonplaceholder.typicode.com/posts/1
```

Presionar `Send request`.

### Paso 3: revisar el resultado

En el panel `Response` deben aparecer:

1. Status `200 OK`.
2. Tiempo de respuesta en milisegundos.
3. Headers de la respuesta.
4. Body JSON.

El request se guarda automáticamente en `History`.

## 6. Ambientes

Un ambiente agrupa configuración que se reutiliza entre requests.

Cada ambiente contiene:

1. Nombre.
2. Base URL.
3. Autenticación.
4. Variables.
5. Headers.

### 6.1 Base URL

Ejemplos válidos:

```text
https://api.midominio.com
http://localhost:8080
http://127.0.0.1:5000/api
```

Si en API Explorer se escribe una URL relativa como `/orders`, se combina con la Base URL.

Si se escribe una URL absoluta como `https://otra-api.test/orders`, esa URL tiene prioridad.

### 6.2 Headers de ambiente

Se agregan automáticamente a cada request que use el ambiente.

Ejemplos:

| Name | Value |
| --- | --- |
| `Accept` | `application/json` |
| `X-Tenant-Id` | `tenant-01` |
| `X-Correlation-Id` | `manual-test` |

Si API Explorer define un header con el mismo nombre, el valor del Explorer reemplaza el valor del ambiente.

### 6.3 Editar o eliminar

En `Environment list`:

1. `Edit` carga el ambiente en el editor.
2. Modificar los campos.
3. Presionar `Save environment`.
4. `Delete` elimina el ambiente de SQLite.

## 7. Variables runtime

Las variables usan esta sintaxis:

```text
{{NombreVariable}}
```

Son case-insensitive, pero se recomienda conservar siempre el mismo nombre.

Pueden utilizarse en:

1. URL.
2. Headers.
3. Query parameters.
4. Body JSON.
5. Autenticación.
6. Partes multipart de texto.

### 7.1 Ejemplo completo

Variables del ambiente:

| Name | Value |
| --- | --- |
| `BrandId` | `15` |
| `Token` | `abc123` |

URL:

```text
/brands/{{BrandId}}/products
```

Header:

```text
X-Debug-Token: {{Token}}
```

Body:

```json
{
  "brandId": "{{BrandId}}",
  "requestedBy": "manual-test"
}
```

### 7.2 Variables no encontradas

Actualmente un placeholder desconocido puede permanecer sin resolver. Antes de enviar, revisar que todas las variables usadas existan en el ambiente o en el payload.

## 8. API Explorer

### 8.1 Métodos soportados

El selector permite:

```text
GET
POST
PUT
PATCH
DELETE
```

### 8.2 Headers del request

Usar `Add header` para agregar filas.

Ejemplo:

| Name | Value |
| --- | --- |
| `Content-Language` | `es-CO` |
| `X-Request-Source` | `BackendTestingStudio` |

### 8.3 Query parameters

Usar `Add parameter`.

Ejemplo:

| Name | Value |
| --- | --- |
| `page` | `1` |
| `pageSize` | `20` |
| `brandId` | `{{BrandId}}` |

El motor codifica los nombres y valores y los agrega a la URL.

### 8.4 Body None

Usarlo normalmente para GET y DELETE cuando la API no espera body.

### 8.5 Body JSON

Usarlo para enviar JSON en POST, PUT o PATCH.

Ejemplo:

```json
{
  "name": "Producto de prueba",
  "active": true,
  "brandId": "{{BrandId}}"
}
```

El content type predeterminado es `application/json`.

### 8.6 Multipart

El modo `Multipart` permite definir:

1. Name.
2. Value.
3. File name.
4. Content type.

En la UI actual no existe un selector de archivos del sistema. El campo `Value` se convierte a bytes y se envía como contenido de la parte. Sirve para campos multipart y pruebas básicas, pero todavía no es una experiencia completa de subida de archivos.

### 8.7 Reset

`Reset` limpia URL, headers, query params, selección de payload y respuesta actual. También vuelve a seleccionar el primer ambiente disponible.

## 9. Biblioteca de payloads JSON

Sí: actualmente se pueden crear, guardar, editar, eliminar y usar JSONs desde la interfaz.

Los payloads se guardan en SQLite.

### 9.1 Crear un payload

Abrir `Payloads` y completar:

| Campo | Valor |
| --- | --- |
| Name | `Create Post` |
| Description | `Body para crear un post de prueba` |

En `JSON / Template`:

```json
{
  "title": "{{Title}}",
  "body": "Creado desde Backend Testing Studio",
  "userId": 1
}
```

En `Variables`:

| Name | Value |
| --- | --- |
| `Title` | `Mi primer request` |

En `Tags`:

```text
demo
posts
```

Presionar `Save payload`.

### 9.2 Aplicar el payload en API Explorer

Abrir `API Explorer` y configurar:

| Campo | Valor |
| --- | --- |
| Environment | `JSONPlaceholder` |
| Method | `Post` |
| URL | `/posts` |
| Body | `Json` |

En `Payload library`:

1. Seleccionar `Create Post`.
2. Presionar `Apply payload`.
3. Comprobar que el JSON aparece en el editor.
4. Presionar `Send request`.

La variable `{{Title}}` se reemplaza con el valor definido en el payload al aplicarlo.

Resultado esperado en JSONPlaceholder:

1. Status `201 Created`.
2. Body con los valores enviados y un `id` simulado.

### 9.3 Diferencia entre variables de payload y ambiente

Las variables de payload se aplican cuando se presiona `Apply payload`.

Las variables de ambiente se resuelven al ejecutar el request.

Por ejemplo, un payload puede conservar:

```json
{
  "brandId": "{{BrandId}}"
}
```

Si `BrandId` no está definido dentro del payload, el placeholder permanece. Después, al enviar, el motor puede resolverlo usando la variable `BrandId` del ambiente.

## 10. Autenticación

La autenticación se configura en `Environments` y se aplica automáticamente mediante `IHttpEngine`.

### 10.1 Bearer

Seleccionar `Bearer` y escribir solamente el token:

```text
eyJhbGciOi...
```

El motor envía:

```http
Authorization: Bearer eyJhbGciOi...
```

El campo también puede contener una variable:

```text
{{Token}}
```

### 10.2 Basic

Seleccionar `Basic` y completar usuario y contraseña.

El motor construye el header `Authorization: Basic ...` automáticamente.

### 10.3 ApiKey

Seleccionar `ApiKey` y completar:

| Campo | Ejemplo |
| --- | --- |
| Header name | `X-API-Key` |
| Value | `{{ApiKey}}` |

### 10.4 OAuth

OAuth todavía no está implementado.

## 11. Historial

Cada request enviado desde API Explorer se guarda automáticamente.

Abrir `History` para consultar:

1. Fecha y hora.
2. Método.
3. URL final.
4. Ambiente.
5. Headers.
6. Body.
7. Status.
8. Headers de respuesta.
9. Body de respuesta.
10. Tiempo de ejecución.

### 11.1 Repetir un request

1. Seleccionar una entrada.
2. Revisar el snapshot.
3. Presionar `Repeat request`.
4. La aplicación vuelve a ejecutarlo.
5. El resultado repetido se guarda como una nueva entrada.

Si el ambiente todavía existe, al repetir se vuelven a aplicar sus variables actuales.

## 12. Plugins

### 12.1 Respuesta corta

No se puede copiar todavía una carpeta con `plugin.json` dentro de la aplicación y esperar que se cargue.

`PLUGIN_SPEC.md` define ese formato como arquitectura objetivo, pero el parser y el loader de carpetas JSON aún no están implementados.

### 12.2 Qué hace el loader actual

El loader actual:

1. Revisa assemblies ya cargados cuyo nombre comienza por `BackendTestingStudio`.
2. Busca clases concretas que implementen `IPluginModule`.
3. Crea esas clases mediante un constructor sin parámetros.
4. Muestra su `PluginDefinition` en la pantalla `Plugins`.

Existe un plugin integrado llamado `Built-in Scenarios` que sirve para comprobar el catálogo.

### 12.3 Limitaciones actuales de plugins

Actualmente:

1. No hay botón para instalar un plugin.
2. No hay carpeta vigilada de plugins.
3. No se interpreta `plugin.json`.
4. No se validan carpetas contra `PLUGIN_SPEC.md`.
5. Los endpoints del plugin no se importan a API Explorer.
6. Los escenarios del plugin no se ejecutan desde la UI.
7. La pantalla `Plugins` es un catálogo de lectura.

### 12.4 Agregar un plugin durante desarrollo

La forma soportada por el código actual es crear una clase C# dentro del proyecto `BackendTestingStudio.Plugins` que implemente `IPluginModule`.

Ejemplo mínimo:

```csharp
using BackendTestingStudio.Core.Plugins;

namespace BackendTestingStudio.Plugins;

public sealed class DemoPlugin : IPluginModule
{
    public PluginDefinition Definition { get; } = new(
        name: "Demo API",
        version: new Version(1, 0, 0),
        author: "Equipo Demo",
        description: "Plugin de ejemplo.",
        endpoints:
        [
            new PluginEndpointDefinition(
                "Get product",
                "GET",
                "/products/{{ProductId}}",
                "Obtiene un producto.")
        ]);
}
```

Después hay que recompilar y reiniciar:

```bash
dotnet build BackendTestingStudio.sln
./run.sh
```

La clase debe aparecer en `Plugins`. Esto requiere modificar y compilar el proyecto; no equivale todavía a instalar un plugin externo.

## 13. Scenario Engine y Assertions

Los motores existen y tienen pruebas automatizadas, pero no están registrados ni conectados a una pantalla de ejecución.

Por eso, desde la aplicación levantada todavía no se puede:

1. Crear escenarios.
2. Agregar steps desde un formulario.
3. Configurar assertions visualmente.
4. Ejecutar un escenario.
5. Ver resultados de assertions.

El Scenario Engine ya puede, desde código:

1. Ejecutar steps ordenados.
2. Ejecutar los cinco métodos HTTP.
3. Capturar variables desde JSONPath, headers, status o body.
4. Usar variables capturadas en steps posteriores.
5. Ejecutar assertions.
6. Detener o continuar el flujo según `StopOnFailure`.

Las pruebas de referencia están en `BackendTestingStudio.Scenarios.Tests/ScenarioEngineTests.cs`.

## 14. Persistencia SQLite

Ambientes, payloads e historial se guardan en:

```text
BackendTestingStudio.UI/bin/Debug/net10.0/backend-testing-studio.environments.db
```

La ruta se calcula usando `AppContext.BaseDirectory`, por lo que puede cambiar entre Debug, Release o una publicación.

No editar el archivo mientras la aplicación está ejecutándose.

Si se elimina la base de datos, se pierde:

1. Ambientes.
2. Variables y headers de ambientes.
3. Payloads.
4. Historial.

El esquema se vuelve a crear cuando los repositorios se inicializan.

## 15. Ejecutar las pruebas automatizadas

Desde la raíz:

```bash
dotnet test BackendTestingStudio.sln
```

También por módulo:

```bash
dotnet test BackendTestingStudio.Http.Tests
dotnet test BackendTestingStudio.Storage.Tests
dotnet test BackendTestingStudio.Assertions.Tests
dotnet test BackendTestingStudio.Scenarios.Tests
```

La suite actual cubre HTTP, almacenamiento, plugins, assertions y escenarios.

## 16. Solución de problemas

### 16.1 La página aparece como HTML plano

Comprobar en la terminal si hay 404 para:

```text
/_framework/blazor.web.js
```

Regenerar dependencias y assets:

```bash
dotnet restore BackendTestingStudio.UI/BackendTestingStudio.UI.csproj
dotnet build BackendTestingStudio.UI/BackendTestingStudio.UI.csproj
./run.sh
```

La máquina debe poder acceder a NuGet para descargar los assets internos de ASP.NET Core 10 que no estén en caché.

### 16.2 Los botones no responden

Es el mismo síntoma de un runtime Blazor ausente. Revisar:

1. Que `/_framework/blazor.web.js` responda 200.
2. Que no haya errores rojos en la consola del navegador.
3. Que la aplicación se haya levantado con el perfil `http` o en ambiente Development.

### 16.3 Límite de inotify

Si aparece:

```text
The configured user limit on the number of inotify instances has been reached
```

El proyecto configura `DOTNET_USE_POLLING_FILE_WATCHER=1` en sus perfiles. Levantarlo con:

```bash
dotnet run --project BackendTestingStudio.UI --launch-profile http
```

Si persiste, aumentar los límites del sistema:

```bash
sudo sysctl fs.inotify.max_user_instances=512
sudo sysctl fs.inotify.max_user_watches=524288
```

### 16.4 Puerto ocupado o permiso de socket

Usar el perfil con puerto dinámico:

```bash
./run.sh
```

No reutilizar manualmente un puerto que ya esté ocupado.

### 16.5 Error al llamar una API HTTPS local

Puede ser un certificado de desarrollo no confiable. Probar primero con HTTP local o confiar el certificado de .NET según el sistema operativo.

No desactivar globalmente la validación TLS como solución permanente.

### 16.6 Advertencia de SQLite

La solución actualmente muestra una advertencia de seguridad para `SQLitePCLRaw.lib.e_sqlite3 2.1.11`. Es una deuda técnica pendiente. No exponer esta aplicación a Internet ni usar datos sensibles hasta actualizar y verificar esa dependencia.

## 17. Flujo recomendado de uso diario

1. Levantar la aplicación con `./run.sh`.
2. Crear o seleccionar un ambiente.
3. Definir variables y autenticación.
4. Crear payloads JSON reutilizables si son necesarios.
5. Abrir API Explorer.
6. Seleccionar ambiente y método.
7. Escribir URL, headers y query parameters.
8. Seleccionar `None`, `Json` o `Multipart`.
9. Aplicar un payload si corresponde.
10. Enviar el request.
11. Revisar status, headers, body y tiempo.
12. Abrir History para auditar o repetir el request.

## 18. Próximas capacidades necesarias

Para que el producto complete el flujo descrito en `PLUGIN_SPEC.md`, todavía se necesita:

1. Loader de carpetas y archivos JSON.
2. Validación de plugins contra el contrato.
3. Instalación y desinstalación de plugins desde UI.
4. Integración de endpoints y payloads de plugins con API Explorer.
5. Editor visual de escenarios.
6. Integración del Scenario Engine y Assertion Engine mediante DI.
7. Pantalla de ejecución de escenarios.
8. Reportes de resultados.
