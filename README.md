<<<<<<< HEAD
# backend-testing-studio
An extensible plugin-based platform for testing, validating, and automating business scenarios across REST APIs, gRPC services, and microservice architectures.
=======
# Backend Testing Studio

Backend Testing Studio es una herramienta local para probar APIs backend desde una interfaz web. El proyecto está organizado por capas y motores desacoplados para soportar exploración HTTP, ambientes, variables, historial, payloads reutilizables, assertions, escenarios, reportes y plugins.

El objetivo del producto es permitir definir pruebas reutilizables de APIs sin acoplar la UI a implementaciones concretas como `HttpClient`, SQLite o plugins específicos.

## Estado Actual

Funcional desde la UI:

- Dashboard y layout principal.
- Navegación lateral y superior.
- Gestión de ambientes.
- Variables por ambiente.
- Headers por ambiente.
- Autenticación por ambiente: Bearer, Basic y ApiKey.
- API Explorer para enviar requests HTTP.
- Métodos HTTP: GET, POST, PUT, PATCH y DELETE.
- Headers, query parameters y body JSON.
- Multipart básico.
- Resolución de variables con formato `{{Variable}}`.
- Historial de requests en SQLite.
- Repetir requests desde History.
- Biblioteca de payloads JSON reutilizables.
- Catálogo de plugins instalados desde módulos compilados.

Implementado como motor o librería, pero todavía sin pantalla completa de operación:

- Assertion Engine.
- Scenario Engine.
- Reporting Engine.
- Exportación de reportes a HTML, Markdown y JSON.

Plugins declarativos incluidos:

- Swagger PetStore.
- DummyJSON.

Limitaciones actuales:

- Los plugins declarativos bajo `plugins/` ya tienen estructura, payloads, assertions y escenarios, pero todavía no se cargan ni ejecutan automáticamente desde la UI.
- La UI de Scenarios todavía no permite crear o ejecutar escenarios.
- La UI de Reports todavía no lista ejecuciones ni exporta reportes desde pantalla.
- OAuth no está implementado.

## Requisitos

- Linux, macOS o Windows con terminal.
- .NET SDK compatible con los proyectos del repositorio.
- Navegador web moderno.
- Git.

El proyecto usa SQLite para persistencia local. La base de datos se genera automáticamente al ejecutar la aplicación.

## Cómo Ejecutar

Desde la raíz del repositorio:

```bash
./run.sh
```

Alternativa directa con `dotnet`:

```bash
dotnet run --project BackendTestingStudio.UI/BackendTestingStudio.UI.csproj
```

Cuando la aplicación levante, la terminal mostrará una línea similar a:

```text
Now listening on: http://127.0.0.1:XXXXX
```

Abre esa URL en el navegador. El puerto puede cambiar en cada ejecución.

Para detener la aplicación:

```text
Ctrl+C
```

## Cómo Probar

Compilar toda la solución:

```bash
dotnet build BackendTestingStudio.sln
```

Ejecutar todas las pruebas:

```bash
dotnet test BackendTestingStudio.sln
```

Ejecutar pruebas de un proyecto específico:

```bash
dotnet test BackendTestingStudio.Http.Tests/BackendTestingStudio.Http.Tests.csproj
dotnet test BackendTestingStudio.Storage.Tests/BackendTestingStudio.Storage.Tests.csproj
dotnet test BackendTestingStudio.Assertions.Tests/BackendTestingStudio.Assertions.Tests.csproj
dotnet test BackendTestingStudio.Scenarios.Tests/BackendTestingStudio.Scenarios.Tests.csproj
dotnet test BackendTestingStudio.Reporting.Tests/BackendTestingStudio.Reporting.Tests.csproj
dotnet test BackendTestingStudio.Plugins.Tests/BackendTestingStudio.Plugins.Tests.csproj
```

## Estructura del Repositorio

```text
BackendTestingStudio/
├── BackendTestingStudio.UI/             # Aplicación web y pantallas
├── BackendTestingStudio.Core/           # Modelos, contratos y reglas centrales
├── BackendTestingStudio.Http/           # Motor HTTP desacoplado
├── BackendTestingStudio.Storage/        # Persistencia SQLite y repositorios
├── BackendTestingStudio.Assertions/     # Motor de validaciones
├── BackendTestingStudio.Scenarios/      # Motor de ejecución de escenarios
├── BackendTestingStudio.Reporting/      # Generación y exportación de reportes
├── BackendTestingStudio.Plugins/        # Modelo e infraestructura de plugins
├── BackendTestingStudio.*.Tests/        # Pruebas automatizadas por módulo
├── plugins/                             # Plugins declarativos de ejemplo
├── ARCHITECTURE.md                      # Arquitectura general
├── PROJECT_RULES.md                     # Reglas obligatorias del proyecto
├── CODING_STANDARDS.md                  # Estándares de código
├── CONTRIBUTING.md                      # Guía de contribución
├── PLUGIN_SPEC.md                       # Especificación de plugins
├── ROADMAP.md                           # Plan del producto por versiones
├── USER_MANUAL.md                       # Manual de usuario
├── Promts_guide.md                      # Guía de prompts aplicados
├── progreso                             # Seguimiento de avance por prompt
└── run.sh                               # Script de ejecución local
```

## Responsabilidad de Cada Proyecto

### BackendTestingStudio.UI

Contiene la aplicación web. Sus responsabilidades son:

- Renderizar la interfaz.
- Recibir interacción del usuario.
- Orquestar casos de uso llamando servicios.
- Mostrar resultados.

Regla importante: la UI no debe usar `HttpClient` directamente. Toda ejecución HTTP debe pasar por `IHttpEngine`.

### BackendTestingStudio.Core

Contiene contratos, modelos y abstracciones centrales. Debe mantenerse independiente de UI, SQLite, motores concretos y plugins externos.

Responsabilidades:

- Entidades del dominio.
- DTOs compartidos.
- Interfaces principales.
- Contratos para servicios, repositorios y motores.

### BackendTestingStudio.Http

Implementa el motor HTTP genérico.

Responsabilidades:

- Ejecutar requests GET, POST, PUT, PATCH y DELETE.
- Resolver headers.
- Resolver query parameters.
- Enviar JSON.
- Enviar multipart.
- Aplicar autenticación Bearer, Basic y ApiKey.
- Resolver variables runtime antes de ejecutar el request.

### BackendTestingStudio.Storage

Implementa persistencia local con SQLite.

Responsabilidades:

- Repositorios.
- Inicialización de base de datos.
- CRUD de ambientes.
- CRUD de payloads.
- Persistencia de historial.
- Persistencia de variables y headers.

### BackendTestingStudio.Assertions

Implementa validaciones desacopladas de cualquier API específica.

Soporta:

- StatusCode.
- JSONPath.
- Headers.
- Equals.
- Contains.
- Null.
- NotNull.
- Tiempo máximo.

### BackendTestingStudio.Scenarios

Implementa el motor de escenarios.

Un escenario puede tener múltiples steps. Cada step puede:

- Ejecutar requests.
- Guardar variables.
- Ejecutar assertions.
- Usar variables de steps anteriores.
- Detener el flujo cuando falle.

### BackendTestingStudio.Reporting

Genera reportes de ejecución.

Cada reporte puede incluir:

- Resumen.
- Tiempo total.
- Steps.
- Assertions.
- Variables.
- Errores.

Formatos soportados:

- HTML.
- Markdown.
- JSON.

### BackendTestingStudio.Plugins

Define el modelo de plugins y su infraestructura base.

Un plugin puede declarar:

- Nombre.
- Versión.
- Autor.
- Descripción.
- Endpoints.
- Escenarios.
- Payloads.
- Variables.
- Assertions.
- Ambientes.

## Pantallas Disponibles

### Dashboard

Vista principal del workspace. Muestra el estado general de la aplicación y acceso a las secciones principales.

### Environments

Permite crear, editar y eliminar ambientes.

Un ambiente puede contener:

- Nombre.
- Base URL.
- Variables.
- Headers.
- Configuración de autenticación.

### API Explorer

Permite construir y ejecutar un request manualmente.

Flujo básico:

1. Seleccionar un Environment.
2. Elegir método HTTP.
3. Escribir URL o path.
4. Agregar headers si aplica.
5. Agregar query parameters si aplica.
6. Agregar body JSON si aplica.
7. Seleccionar payload si aplica.
8. Enviar request.
9. Revisar status, headers, body y tiempo.

### Payloads

Permite guardar JSON reutilizable.

Cada payload puede tener:

- Nombre.
- Descripción.
- JSON.
- Variables.
- Tags.

### History

Guarda automáticamente requests ejecutados desde API Explorer.

Permite revisar:

- Fecha.
- Método.
- URL.
- Environment.
- Headers.
- Body.
- Response.
- Tiempo.
- Status.

También permite repetir un request.

### Plugins

Muestra plugins instalados desde el sistema actual de módulos compilados.

Nota: los plugins declarativos en carpetas JSON existen en `plugins/`, pero la integración completa de carga y ejecución desde UI todavía está pendiente.

## Plugins Incluidos

### Swagger PetStore

Ubicación:

```text
plugins/swagger-petstore/
```

Incluye:

- Crear mascota.
- Consultar mascota.
- Actualizar mascota.
- Eliminar mascota.
- Escenarios CRUD.
- Payloads de creación y actualización.
- Assertions de status y contenido.

### DummyJSON

Ubicación:

```text
plugins/dummyjson/
```

Incluye:

- Login.
- Products.
- Cart.
- Users.
- Escenarios completos de flujo.
- Payloads reutilizables.
- Assertions por módulo.

## Estructura Esperada de un Plugin Declarativo

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

La especificación completa está en `PLUGIN_SPEC.md`.

## Reglas de Arquitectura

Las decisiones nuevas deben respetar:

- `ARCHITECTURE.md`
- `PROJECT_RULES.md`
- `PLUGIN_SPEC.md`

Reglas clave:

- La UI nunca accede directamente a `HttpClient`.
- Toda llamada HTTP pasa por `IHttpEngine`.
- El Core nunca depende de Plugins.
- El Core nunca depende de UI.
- Los plugins no conocen la implementación interna del motor.
- La persistencia SQLite debe estar detrás de repositorios o servicios.
- No duplicar lógica.
- Todo código debe ser testeable.
- Cada entidad debe tener una única responsabilidad.

## Flujo de Datos

Flujo manual desde API Explorer:

```text
UI
→ Servicio de aplicación
→ IHttpEngine
→ Autenticación / variables / headers / query / body
→ API externa
→ IHttpEngineResponse
→ Historial SQLite
→ UI
```

Flujo esperado para escenarios:

```text
Scenario Engine
→ Step
→ IHttpEngine
→ Assertion Engine
→ Runtime Variables
→ Reporting Engine
```

Flujo esperado para plugins:

```text
Plugin
→ Endpoints / Payloads / Variables / Scenarios
→ Scenario Engine o API Explorer
→ IHttpEngine
```

## Persistencia Local

SQLite se usa para datos locales de la aplicación.

Datos persistidos actualmente:

- Ambientes.
- Variables de ambiente.
- Headers de ambiente.
- Payloads.
- Historial de requests.

La base de datos es un artefacto local generado al ejecutar la aplicación y no debe subirse al repositorio.

## Git y Push

Ver remotos:

```bash
git remote -v
```

Primer push de la rama `master`:

```bash
git push -u origin master
```

Push posterior:

```bash
git push
```

Si Git pide usuario o token, autentica desde tu terminal local.

## Problemas Conocidos

### Límite de inotify en Linux

Si aparece un error similar a:

```text
The configured user limit (128) on the number of inotify instances has been reached
```

Ejecuta con:

```bash
DOTNET_USE_POLLING_FILE_WATCHER=1 ./run.sh
```

O aumenta el límite de watchers del sistema.

### Error 404 en `_framework/blazor.web.js`

Si el navegador o logs muestran:

```text
GET /_framework/blazor.web.js 404
```

Prueba:

```bash
dotnet restore
dotnet build BackendTestingStudio.sln
./run.sh
```

También verifica que estás abriendo la URL exacta que imprime `dotnet run`, porque el puerto cambia.

### Warning de SQLitePCLRaw

Puede aparecer una advertencia de vulnerabilidad para una versión transitiva de SQLite. El proyecto puede compilar, pero conviene actualizar paquetes en una iteración dedicada para cerrar esa alerta.

## Documentación Relacionada

- `ARCHITECTURE.md`: arquitectura y separación de responsabilidades.
- `PROJECT_RULES.md`: reglas obligatorias.
- `CODING_STANDARDS.md`: convenciones de código.
- `CONTRIBUTING.md`: cómo contribuir.
- `PLUGIN_SPEC.md`: contrato de plugins.
- `ROADMAP.md`: planificación por versiones.
- `USER_MANUAL.md`: manual de uso paso a paso.
- `progreso`: seguimiento de prompts aplicados.

## Próximos Pasos Técnicos

1. Integrar carga real de plugins declarativos desde `plugins/`.
2. Crear UI para ejecutar escenarios.
3. Conectar Scenario Engine con Reporting Engine desde la UI.
4. Agregar pantalla de Reports.
5. Permitir exportar reportes desde la aplicación.
6. Endurecer validaciones de JSON plugin contra `PLUGIN_SPEC.md`.
7. Actualizar dependencias con advertencias de seguridad.
>>>>>>> master
