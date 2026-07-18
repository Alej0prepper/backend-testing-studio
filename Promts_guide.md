Lo reorganizaría de la siguiente manera. La idea es que Codex actúe como un desarrollador más del equipo y que cada prompt produzca un resultado completo, compilable y listo para hacer commit. Nunca le pediría "haz toda la aplicación". Cada iteración debe ser pequeña, verificable y dejar el proyecto en un estado funcional.

---

# Prompt 0 — Definir la arquitectura del producto (SIN ESCRIBIR CÓDIGO)

> **Este es el prompt más importante de todos.**

Antes de escribir una sola línea de código quiero definir completamente la arquitectura del producto.

No implementes ninguna funcionalidad.

No crees ningún proyecto.

No escribas código.

Actúa como un Software Architect Senior.

Diseña la arquitectura completa del proyecto llamado:

**Backend Testing Studio**

Genera los siguientes documentos:

```
ARCHITECTURE.md
CONTRIBUTING.md
PROJECT_RULES.md
CODING_STANDARDS.md
PLUGIN_SPEC.md
ROADMAP.md
```

Los documentos deben definir detalladamente:

## ARCHITECTURE.md

* Arquitectura general
* Responsabilidades de cada proyecto
* Flujo de datos
* Separación de capas
* Dependencias permitidas
* Patrones utilizados
* Principios SOLID
* Clean Architecture
* Dependency Injection
* Plugin Architecture
* Convenciones para DTOs
* Convenciones para Services
* Convenciones para Repositories
* Convenciones para UI
* Convenciones para SQLite
* Estrategia para escalabilidad

## PROJECT_RULES.md

Definir reglas obligatorias.

Por ejemplo:

* Nunca acceder directamente al HttpClient desde la UI.
* Toda llamada HTTP pasa por IHttpEngine.
* Ningún plugin conoce la implementación interna del motor.
* El Core nunca depende de Plugins.
* No duplicar lógica.
* Todo código debe ser testeable.
* Toda entidad debe tener una única responsabilidad.

## CODING_STANDARDS.md

Definir estándares de código.

* Naming
* Carpetas
* Namespaces
* Async/Await
* Dependency Injection
* Logging
* Exceptions
* Comentarios
* XML Docs
* Organización de archivos

## CONTRIBUTING.md

Explicar cómo contribuir.

Cómo crear una feature.

Cómo crear un plugin.

Cómo crear un escenario.

Cómo crear un Assertion.

Cómo hacer Pull Requests.

## PLUGIN_SPEC.md

Este es probablemente el documento más importante.

Debe definir exactamente cómo funciona un plugin.

Formato.

Interfaces.

Carpetas.

JSON.

Payloads.

Variables.

Escenarios.

Endpoints.

Assertions.

Metadatos.

Versionado.

Compatibilidad.

## ROADMAP.md

Planificar el producto por versiones.

v0.1

v0.2

v0.3

...

v1.0

No escribir código.

Solo diseñar la arquitectura.

---

## IMPORTANTE

A partir de este momento, **todos los prompts posteriores deberán respetar estrictamente**:

* ARCHITECTURE.md
* PROJECT_RULES.md
* PLUGIN_SPEC.md

No se permitirá introducir decisiones que contradigan estos documentos.

---

# Prompt 1 — Inicializar la solución

> **En todos los prompts siguientes añade siempre este encabezado:**

```
Respeta estrictamente:

ARCHITECTURE.md
PROJECT_RULES.md
PLUGIN_SPEC.md

No introduzcas nuevas decisiones arquitectónicas.
No cambies convenciones ya definidas.
```

Ahora construye la solución inicial.

Tecnologías:

* .NET 10
* Blazor Server
* Clean Architecture
* SQLite
* HttpClientFactory
* Dependency Injection

La solución se llamará:

```
BackendTestingStudio
```

Crear los proyectos:

```
BackendTestingStudio.UI
BackendTestingStudio.Core
BackendTestingStudio.Http
BackendTestingStudio.Storage
BackendTestingStudio.Scenarios
BackendTestingStudio.Assertions
BackendTestingStudio.Reporting
BackendTestingStudio.Plugins
```

Configurar todas las referencias.

El proyecto debe compilar.

No implementar funcionalidades.

Solo la estructura.

### Commit

```
Initial solution structure
```

---

# Prompt 2 — Layout

Crear una interfaz moderna.

Implementar únicamente:

Sidebar

TopBar

Dashboard

Navegación

Menú:

```
Dashboard
API Explorer
Scenarios
History
Reports
Settings
```

No implementar páginas.

Solo navegación y layout.

### Commit

```
Create application shell
```

---

# Prompt 3 — HTTP Engine

Implementar un servicio desacoplado:

```
IHttpEngine
```

Debe soportar:

* GET
* POST
* PUT
* PATCH
* DELETE

Y además:

* Headers
* Query Parameters
* JSON
* Multipart
* Bearer
* Basic
* ApiKey

Registrar mediante DI.

Crear pruebas básicas.

No crear pantallas.

### Commit

```
Implement generic HTTP engine
```

---

# Prompt 4 — Environments

Crear el sistema de ambientes.

Entidades:

```
Environment

EnvironmentVariable
```

Cada ambiente tendrá:

* Nombre
* BaseUrl
* Variables
* Headers

Persistencia SQLite.

CRUD completo.

Página Environments.

### Commit

```
Add environment management
```

---

# Prompt 5 — API Explorer

Crear la pantalla API Explorer.

Debe permitir:

Seleccionar Environment.

Elegir método HTTP.

Escribir URL.

Agregar Headers.

Agregar Query Parameters.

Agregar Body.

Enviar Request.

Mostrar:

* Status
* Headers
* Body
* Tiempo

La UI nunca hablará directamente con HttpClient.

Siempre con IHttpEngine.

### Commit

```
Create API Explorer
```

---

# Prompt 6 — Historial

Guardar automáticamente:

* Fecha
* Método
* URL
* Environment
* Headers
* Body
* Response
* Tiempo
* Status

Persistir en SQLite.

Crear página History.

Permitir repetir un request.

### Commit

```
Implement request history
```

---

# Prompt 7 — Autenticación

Crear autenticación genérica.

Implementar:

* Bearer
* ApiKey
* Basic

No implementar OAuth todavía.

El HttpEngine debe aplicar automáticamente la autenticación configurada para el Environment.

### Commit

```
Implement authentication providers
```

---

# Prompt 8 — Sistema de Plugins

Crear el modelo de plugins.

Cada plugin tendrá:

* Nombre
* Versión
* Autor
* Descripción
* Endpoints
* Escenarios
* Payloads
* Variables

No cargar plugins todavía.

Solo la arquitectura.

### Commit

```
Create plugin model
```

---

# Prompt 9 — Plugin Loader

Implementar el cargador de plugins.

Debe descubrir automáticamente los plugins registrados.

Cada plugin implementará una interfaz común.

Mostrar plugins instalados en la UI.

### Commit

```
Implement plugin loader
```

---

# Prompt 10 — Biblioteca de Payloads

Crear una librería de payloads reutilizables.

Cada payload tendrá:

* Nombre
* Descripción
* JSON
* Variables
* Tags

Persistir en SQLite.

Integrar con API Explorer.

### Commit

```
Add payload library
```

---

# Prompt 11 — Variables

Crear un sistema global de variables.

Ejemplos:

```
{{Token}}

{{BrandId}}

{{ProductId}}

{{OrderId}}

{{UserId}}
```

El motor debe resolver automáticamente las variables antes de ejecutar el request.

### Commit

```
Implement runtime variables
```

---

# Prompt 12 — Assertions

Crear el motor de validaciones.

Debe soportar:

* StatusCode
* JSONPath
* Headers
* Equals
* Contains
* Null
* NotNull
* Tiempo máximo

Todo desacoplado de cualquier API.

### Commit

```
Implement assertion engine
```

---

# Prompt 13 — Scenario Engine

Implementar el motor de escenarios.

Cada escenario tendrá múltiples Steps.

Cada Step podrá:

* Ejecutar requests
* Guardar variables
* Ejecutar assertions
* Usar variables anteriores
* Detener el flujo cuando falle

No crear escenarios concretos todavía.

### Commit

```
Implement scenario engine
```

---

# Prompt 14 — Reporting

Crear reportes.

Cada ejecución debe generar:

* Resumen
* Tiempo
* Steps
* Assertions
* Variables
* Errores

Exportar:

* HTML
* Markdown
* JSON

### Commit

```
Add reporting engine
```

---

# Prompt 15 — Plugin Swagger PetStore

Crear un plugin completamente funcional utilizando Swagger PetStore.

No modificar el motor.

Implementar:

* Crear mascota
* Consultar mascota
* Actualizar mascota
* Eliminar mascota

Crear escenarios utilizando exclusivamente el sistema de plugins.

### Commit

```
Create PetStore plugin
```

---

# Prompt 16 — Plugin DummyJSON

Crear un segundo plugin utilizando DummyJSON.

No modificar el Core.

Implementar:

* Login
* Products
* Cart
* Users

Crear escenarios completos.

### Commit

```
Create DummyJSON plugin
```

---

# Prompt 17 — Plugin privado eStore CSA (NO PÚBLICO)

**Este plugin no formará parte del repositorio público.**

Crear un plugin privado llamado:

```
eStoreCSA
```

No modificar el motor.

Organizar los endpoints por módulos:

* Identity
* Catalog
* Sales
* Logistic
* Translation
* BrincoXpress

Crear escenarios de negocio como:

* Login
* Crear catálogo completo
* Crear producto
* Crear variante
* Crear carrito
* Crear destinatario
* Crear orden
* Consultar orden
* Validar permisos
* Validar respuestas mediante Assertions

Todo debe reutilizar exclusivamente la infraestructura ya creada.

### Commit

```
Create private eStore CSA plugin
```

---

# Resultado esperado

Al finalizar esta secuencia tendrás dos productos claramente separados:

## Repositorio público (GitHub)

```
BackendTestingStudio
│
├── Motor HTTP
├── API Explorer
├── Scenario Engine
├── Assertions
├── Reporting
├── Plugin System
├── SQLite
├── PetStore Plugin
├── DummyJSON Plugin
└── Documentación completa
```

Este será tu portfolio profesional y demostrará tu capacidad para diseñar herramientas de testing de nivel profesional.

## Repositorio privado

```
BackendTestingStudio.eStoreCSA
│
├── Plugin eStore CSA
├── Escenarios de negocio
├── Payloads
├── Variables
├── Assertions
└── Configuración de entornos
```

Aquí residirá todo lo relacionado con la API propietaria de tu empresa, sin exponer endpoints, contratos, modelos o lógica de negocio en el repositorio público.

De esta forma podrás enseñar al mundo una herramienta de ingeniería reutilizable y bien diseñada, mientras respetas completamente la confidencialidad del proyecto sobre el que trabajas.
