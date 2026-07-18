# Backend Testing Studio - Coding Standards

## 1. Naming

1. Use PascalCase for types, methods, properties, and public members.
2. Use camelCase for local variables and private fields.
3. Use `Async` suffix for asynchronous methods.
4. Use descriptive names that express intent, not implementation shortcuts.
5. Avoid abbreviations unless they are widely understood in the domain.
6. Use kebab-case for plugin file names and folder names.

## 2. Folder Structure

1. Organize by feature when the feature is user-facing.
2. Organize by responsibility when the code is infrastructure.
3. Keep interfaces close to the contracts they represent.
4. Keep implementation details separate from abstractions.
5. Group files by bounded responsibility, not by technical accident.

## 3. Namespaces

1. Namespaces must match the project and feature path.
2. Namespace names must be stable and predictable.
3. Avoid deep, arbitrary namespace chains.
4. Keep namespace boundaries aligned with dependency boundaries.

## 4. Async and Await

1. Use async all the way for I/O operations.
2. Do not block on async work.
3. Use cancellation tokens when the operation can be cancelled.
4. Prefer task-based APIs for external calls.
5. Do not mix sync and async access to the same resource without a strong reason.

## 5. Dependency Injection

1. Prefer constructor injection.
2. Register dependencies in the composition root, not in leaf components.
3. Depend on abstractions, not concrete implementations.
4. Keep service lifetimes explicit.
5. Avoid service locator patterns.

## 6. Logging

1. Use structured logging.
2. Include correlation data in execution flows.
3. Keep log messages short, precise, and actionable.
4. Log at the appropriate level.
5. Never log secrets, tokens, or sensitive payload values in plain text.

## 7. Exceptions

1. Throw typed exceptions for known failure categories.
2. Translate external exceptions at the boundary.
3. Do not use exceptions for normal control flow.
4. Preserve the original exception as the inner exception when rethrowing with context.
5. Include context that helps diagnose the failing plugin, environment, scenario, or endpoint.

## 8. Comments

1. Comments must explain why, not repeat what the code already says.
2. Avoid obvious comments.
3. Use comments sparingly and only for non-trivial decisions.
4. Keep comments in sync with the code they describe.

## 9. XML Documentation

1. Public APIs must have XML docs when the contract is not self-evident.
2. Document parameters, return values, and behavior that callers depend on.
3. Public contracts in Core should be documented especially carefully.
4. Avoid duplicating obvious type names in docs.

## 10. File Organization

1. One public type per file.
2. File names must match the main type name.
3. Keep related support types near the feature they belong to.
4. Avoid giant files with mixed responsibilities.
5. Separate interfaces, models, and implementations clearly.

## 11. DTO Standards

1. DTOs are simple transport shapes.
2. DTOs should not contain behavior unless the behavior is required for validation or formatting of the contract itself.
3. DTOs should be easy to serialize and compare.
4. DTOs should not know about persistence details.

## 12. Service Standards

1. Services should express a single application use case or a closely related family of use cases.
2. Services should coordinate work rather than perform low-level plumbing.
3. Services should return explicit result models.

## 13. Repository Standards

1. Repositories should use names that describe the aggregate or read model they manage.
2. Repository methods should be intention-revealing.
3. Avoid exposing database-specific primitives to callers.

## 14. UI Standards

1. Components should be small and readable.
2. UI logic should be expressed in view models or page-level orchestration, not scattered across markup.
3. Keep styling, layout, and interaction state separated where practical.
4. Avoid direct infrastructure access from components.

## 15. Nullable and Defaults

1. Nullable intent must be explicit.
2. Do not rely on default values to hide missing contract data.
3. Required contract fields must be validated early.

## 16. Testing Standards

1. Test the public behavior of contracts, not private implementation details.
2. Keep tests deterministic.
3. Use descriptive test names that express the scenario and the expected result.
4. Cover validation, execution, failure, and compatibility paths.

