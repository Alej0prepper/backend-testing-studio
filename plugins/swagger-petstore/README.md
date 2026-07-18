# Swagger PetStore Plugin

Declarative plugin for the public Swagger PetStore API.

## Environment

- Base URL: `https://petstore.swagger.io/v2`
- API key header: `api_key`
- Example API key: `special-key`

## Endpoints

- `create-pet`: `POST /pet`
- `get-pet`: `GET /pet/{{PetId}}`
- `update-pet`: `PUT /pet`
- `delete-pet`: `DELETE /pet/{{PetId}}`

## Scenarios

- `create-and-read-pet`: creates a pet and reads it back.
- `pet-crud-lifecycle`: creates, reads, updates, reads again, and deletes a pet.

## Runtime Variables

- `PetId`
- `PetName`
- `UpdatedPetName`
- `PetStatus`
- `UpdatedPetStatus`
- `ApiKey`

Swagger PetStore is a shared public sample API. Data can be changed or reset by other users.
