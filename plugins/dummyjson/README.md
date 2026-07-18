# DummyJSON Plugin

Declarative plugin for DummyJSON.

## Environment

- Base URL: `https://dummyjson.com`
- Auth: bearer token captured from `POST /auth/login`
- Default login: `emilys` / `emilyspass`

## Modules

- `auth`: login and current authenticated user.
- `products`: list, get, search and simulated add.
- `carts`: list, get, user carts, simulated add, update and delete.
- `users`: list, get, search, simulated add, update and delete.

## Scenarios

- `login-and-auth-user`
- `products-catalog-flow`
- `cart-management-flow`
- `users-management-flow`
- `full-commerce-smoke`

DummyJSON mutating endpoints simulate writes and return the expected response without persisting the changes.
