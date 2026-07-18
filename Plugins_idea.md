Aquí es donde está la clave de toda la arquitectura. Si diseñamos bien el sistema de plugins, **el motor nunca sabrá qué API está probando**. Da igual si mañana pruebas eStore CSA, GitHub, Stripe o una API de un banco.

## Yo NO haría un plugin en C#

Lo haría **100% declarativo**.

Es decir, un plugin es simplemente una carpeta con archivos JSON/YAML y payloads.

El motor interpreta esos archivos.

---

# Ejemplo

```
plugins/

estore-csa/

    plugin.json

    environments/

        local.json
        testing.json
        production.json

    modules/

        identity.json
        catalog.json
        sales.json
        logistic.json
        translation.json
        brincoxpress.json

    scenarios/

        login.json

        create-product.json

        buy-product.json

        create-complete-catalog.json

    payloads/

        login-admin.json

        brand.json

        product.json

        variant.json

        order.json

    assertions/

        success.json

        created.json

        unauthorized.json

    variables.json

    README.md
```

El motor solamente carga esta carpeta.

Nada más.

---

# plugin.json

Este archivo describe el plugin.

```json
{
    "id": "estore-csa",

    "name": "eStore CSA",

    "version": "1.0.0",

    "author": "Alejandro Alvarez",

    "description": "Plugin para la plataforma eStore CSA.",

    "defaultEnvironment": "local",

    "modules": [
        "identity",
        "catalog",
        "sales",
        "logistic",
        "translation",
        "brincoxpress"
    ]
}
```

No hay código.

---

# environments

```
testing.json
```

```json
{
    "name": "Testing",

    "baseUrl": "https://testing.myapi.com",

    "authentication": {

        "type": "Bearer",

        "loginScenario": "login-admin"
    }
}
```

Si mañana cambia la URL...

No recompilas nada.

---

# identity.json

Aquí aparece el módulo.

```json
{
    "name": "Identity",

    "endpoints": [

        {

            "id": "login",

            "method": "POST",

            "path": "/api/identity/auth/login",

            "payload": "login-admin",

            "assertions": [

                "success"

            ]
        },

        {

            "id": "users",

            "method": "GET",

            "path": "/api/identity/users"

        }

    ]
}
```

El motor ya sabe construir la UI.

No hay código.

---

# Payloads

login-admin.json

```json
{
    "email": "{{AdminEmail}}",

    "password": "{{AdminPassword}}"
}
```

El motor sustituye las variables.

---

# Variables

variables.json

```json
{
    "AdminEmail": "",

    "AdminPassword": "",

    "AccessToken": "",

    "BrandId": "",

    "CategoryId": "",

    "ProductId": "",

    "OrderId": ""
}
```

---

# Escenarios

Aquí está la magia.

buy-product.json

```json
{
    "name": "Comprar producto",

    "steps": [

        "login",

        "create-cart",

        "add-product",

        "create-recipient",

        "create-order",

        "get-order"
    ]
}
```

No hay código.

---

# ¿Y cómo sabe el motor qué hacer?

Porque cada endpoint ya está definido.

```text
create-order

↓

POST

/api/orders

↓

payload

↓

assertions

↓

guardar variable OrderId
```

---

# Assertions

success.json

```json
{
    "statusCode": 200,

    "body": {

        "success": true

    }
}
```

---

created.json

```json
{
    "statusCode": 201
}
```

---

# Pero aún iría más lejos

Yo no definiría solamente endpoints.

Definiría el formulario que genera la UI.

Por ejemplo.

```json
{
    "id": "createBrand",

    "method": "POST",

    "path": "/api/catalog/brands",

    "form": [

        {

            "name": "name",

            "type": "text",

            "required": true
        }

    ]
}
```

Entonces el motor crea automáticamente

```
Name

[__________________]

[Execute]
```

Sin escribir Blazor.

---

# O un login

```json
{
    "form": [

        {

            "name": "email",

            "type": "email"
        },

        {

            "name": "password",

            "type": "password"
        }

    ]
}
```

La UI aparece sola.

---

# ¿Y si quiero que después del login guarde el token?

Simple.

```json
{
    "saveVariables": [

        {

            "name": "AccessToken",

            "jsonPath": "$.data.accessToken"
        },

        {

            "name": "RefreshToken",

            "jsonPath": "$.data.refreshToken"
        }

    ]
}
```

Ni una línea de código.

---

# ¿Y un escenario?

```json
{
    "steps": [

        {

            "execute": "login"

        },

        {

            "execute": "createBrand"

        },

        {

            "execute": "createCategory"

        },

        {

            "execute": "createProduct"

        }

    ]
}
```

---

# Lo que yo diseñaría (v2)

Aquí es donde creo que Backend Testing Studio puede convertirse en una herramienta realmente potente.

No haría que el plugin solo describa endpoints. Haría que describa **todo el dominio de pruebas**:

```text
Plugin
│
├── Metadata
├── Environments
├── Authentication
├── Variables
├── Modules
│
├── Endpoints
│      ├── Formularios
│      ├── Payloads
│      ├── Assertions
│      ├── Variables generadas
│      ├── Dependencias
│      └── Ejemplos
│
├── Business Scenarios
│
├── Test Suites
│
├── Data Factory
│
├── Mock Data
│
└── Reports
```

De esa forma, el motor no necesita saber absolutamente nada de eStore, GitHub o cualquier otra API. Todo el conocimiento vive en el plugin. Si mañana alguien quiere crear un plugin para Stripe o GitHub, solo tiene que describir su API con el mismo formato, y la aplicación generará automáticamente la interfaz, ejecutará los escenarios, validará las respuestas y producirá reportes sin modificar una sola línea del núcleo del sistema. Ese nivel de desacoplamiento es el que convierte una herramienta interna en un framework reutilizable.
