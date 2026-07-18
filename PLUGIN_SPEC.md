# Backend Testing Studio - Plugin Specification

## 1. Purpose

A plugin describes a backend API test domain in a fully declarative way. The engine loads the plugin, validates it, indexes it, and then uses it to generate UI, execute requests, evaluate assertions, and produce reports.

## 2. Core Principles

1. A plugin is a folder, not compiled plugin code.
2. A plugin is the source of truth for the API testing domain it describes.
3. The engine must not require API-specific code to understand a plugin.
4. A plugin must be readable, portable, and versioned.
5. Every field used by the engine must be defined in this document.

## 3. Canonical Format

1. The canonical format is JSON.
2. JSON is the contract format used by the engine.
3. File names use kebab-case.
4. Folder names use kebab-case.
5. YAML is not part of the canonical v1 contract.

## 4. Plugin Folder Layout

Required content:

1. `plugin.json`
2. `variables.json`
3. `environments/`
4. `modules/`
5. `scenarios/`
6. `payloads/`
7. `assertions/`
8. `README.md`

Optional content:

1. `assets/`
2. `data/`
3. `docs/`
4. `reports/`

## 5. plugin.json

The plugin manifest defines the identity and compatibility of the plugin.

Required fields:

1. `id`
2. `name`
3. `version`
4. `schemaVersion`
5. `engineVersion`
6. `author`
7. `description`
8. `defaultEnvironment`
9. `modules`

Recommended fields:

1. `tags`
2. `license`
3. `repositoryUrl`
4. `supportUrl`
5. `entryPoint` is not allowed because the format is declarative

Field meaning:

1. `id` is the stable plugin identifier.
2. `name` is the human-friendly name.
3. `version` is the plugin semver version.
4. `schemaVersion` is the contract version used by the engine parser.
5. `engineVersion` is the minimum engine version required.
6. `author` is the owner or maintainer name.
7. `description` explains the purpose of the plugin.
8. `defaultEnvironment` is the environment loaded when no explicit choice is made.
9. `modules` lists the module identifiers exposed by the plugin.

## 6. Versioning and Compatibility

1. Plugin versioning uses semantic versioning.
2. Schema versioning uses semantic versioning or a compatible integer-based contract version chosen by the engine, but the meaning must remain explicit.
3. A breaking plugin contract change requires a major version bump.
4. The engine must reject incompatible schema versions.
5. The engine must warn when the plugin version is outside the supported range but the schema is still compatible, if the policy allows warning-based compatibility.

Compatibility model:

1. `engineVersion` declares the minimum engine version required.
2. `schemaVersion` declares the parsing contract version.
3. A plugin is loadable only when both checks pass.

## 7. variables.json

Variables define reusable values for environments, payloads, scenarios, and runtime outputs.

Each variable definition may contain:

1. `name`
2. `type`
3. `defaultValue`
4. `required`
5. `sensitive`
6. `description`
7. `scope`
8. `exportable`
9. `validators`
10. `computed`

Rules:

1. `name` is the stable variable key.
2. `type` must be explicit.
3. `defaultValue` may be empty.
4. `required` indicates whether the user must provide a value before execution.
5. `sensitive` marks secrets that must not be logged in plain text.
6. `scope` defines where the variable is valid.
7. `exportable` indicates whether runtime values may be persisted or surfaced.
8. `validators` define input constraints.
9. `computed` indicates values produced by the engine rather than typed by the user.

Variable scopes:

1. Global plugin scope.
2. Environment scope.
3. Scenario scope.
4. Step scope.
5. Runtime response scope.

Variable precedence:

1. Step values override scenario values.
2. Scenario values override environment values.
3. Environment values override plugin defaults.
4. Runtime response values override earlier defaults when explicitly mapped.

Interpolation syntax:

1. `{{VariableName}}` resolves a variable value.
2. `{{Environment.Name}}` resolves an environment property when allowed by the engine.
3. Unresolved placeholders must fail validation or execution depending on where they are required.

## 8. environments/

Each environment file defines a deploy target and runtime context.

Required environment fields:

1. `id`
2. `name`
3. `baseUrl`
4. `authentication`

Recommended environment fields:

1. `headers`
2. `timeout`
3. `retryPolicy`
4. `variables`
5. `notes`

Authentication model:

1. Authentication is described declaratively.
2. The engine supports environment-level auth configuration.
3. Auth config may reference a login scenario or a token source.
4. Secrets are resolved from variables, not hard-coded.

## 9. modules/

A module groups related endpoints under a functional area.

Required module fields:

1. `id`
2. `name`
3. `endpoints`

Recommended module fields:

1. `description`
2. `basePath`
3. `tags`
4. `defaultHeaders`
5. `authentication`

Rules:

1. A module must be a stable grouping boundary.
2. A module can share defaults across endpoints.
3. A module must not hide endpoint identities.

## 10. Endpoints

An endpoint describes one executable API operation and the UI form needed to run it.

Required endpoint fields:

1. `id`
2. `method`
3. `path`
4. `name`

Recommended endpoint fields:

1. `description`
2. `module`
3. `tags`
4. `headers`
5. `query`
6. `pathParameters`
7. `authentication`
8. `request`
9. `form`
10. `payload`
11. `assertions`
12. `saveVariables`
13. `examples`
14. `dependencies`
15. `expectedStatusCodes`
16. `timeout`
17. `retryPolicy`

### Endpoint request model

An endpoint may define request input using one of three modes:

1. Payload reference.
2. Generated form input.
3. Inline body definition.

Rules:

1. The engine must know which mode applies.
2. The endpoint must not rely on ambiguous request sources.
3. The same endpoint should not define conflicting request modes.

### Form definition

The form definition drives UI generation.

Each form field may contain:

1. `name`
2. `label`
3. `type`
4. `required`
5. `defaultValue`
6. `placeholder`
7. `description`
8. `options`
9. `min`
10. `max`
11. `regex`
12. `multiline`
13. `secret`
14. `source`

Supported field types:

1. Text.
2. Email.
3. Password.
4. Number.
5. Boolean.
6. Date.
7. DateTime.
8. Select.
9. MultiSelect.
10. Json.
11. File reference.

Rules:

1. Form fields must be enough for the engine to render the UI without custom UI code.
2. The form is a contract, not an implementation detail.

## 11. payloads/

Payload files define reusable request bodies.

Required payload fields:

1. `id`
2. `content`

Recommended payload fields:

1. `description`
2. `contentType`
3. `variables`
4. `examples`

Rules:

1. Payloads are templates, not compiled code.
2. Payload templates may contain variable placeholders.
3. A payload must be resolvable before execution.
4. Payloads must not contain engine logic.

## 12. assertions/

Assertions define reusable validation rules.

Required assertion fields:

1. `id`
2. `type`
3. `target`
4. `expected`

Recommended assertion fields:

1. `description`
2. `severity`
3. `message`
4. `caseSensitive`
5. `tolerance`
6. `jsonPath`
7. `headers`
8. `statusCode`

Supported assertion types:

1. Status code equality.
2. Status code range.
3. Header equality.
4. Header contains.
5. Body contains.
6. Body equality.
7. JSONPath equality.
8. JSONPath contains.
9. JSONPath not empty.
10. Regex match.
11. Schema validation when a schema is explicitly declared.

Rules:

1. Assertions are deterministic.
2. Assertions must return structured diagnostics on failure.
3. Assertions do not mutate response data.

## 13. scenarios/

Scenarios define ordered execution flows.

Required scenario fields:

1. `id`
2. `name`
3. `steps`

Recommended scenario fields:

1. `description`
2. `tags`
3. `inputs`
4. `outputs`
5. `preconditions`
6. `postconditions`
7. `variables`
8. `onFailure`

### Step model

Each step may contain:

1. `execute`
2. `with`
3. `saveVariables`
4. `assertions`
5. `description`
6. `enabled`

Rules:

1. `execute` references an endpoint or another scenario by id.
2. Steps are executed in order unless the engine explicitly supports a documented branching rule.
3. A step may save variables from the execution result.
4. A step may attach additional assertions.
5. A scenario must remain readable as a business flow.

### Scenario execution rules

1. The engine resolves the scenario graph before execution.
2. Circular references must fail validation.
3. Missing dependencies must fail validation.
4. Scenario inputs must be mapped before runtime execution starts.

## 14. Save Variables

An endpoint or scenario step may declare variables to capture from responses.

Each mapping may contain:

1. `name`
2. `jsonPath`
3. `source`
4. `transform`
5. `required`

Rules:

1. `name` is the destination variable name.
2. `jsonPath` defines where the value is read from.
3. `transform` is a declarative transformation when supported.
4. Missing required values must fail execution.

## 15. Metadata

Plugin metadata is used for discovery, UI display, and filtering.

Metadata fields may include:

1. `id`
2. `name`
3. `version`
4. `author`
5. `description`
6. `tags`
7. `defaultEnvironment`
8. `modules`
9. `supportedFeatures`

Rules:

1. Metadata must be stable and human-friendly.
2. Metadata is for catalog and UI purposes.
3. Metadata must not be used to infer hidden behavior that is not declared elsewhere.

## 16. Validation Rules

1. Every required field must exist.
2. Every reference must resolve.
3. Every plugin id must be unique.
4. Every module id must be unique within the plugin.
5. Every endpoint id must be unique within the plugin.
6. Every scenario id must be unique within the plugin.
7. Every payload id must be unique within the plugin.
8. Every assertion id must be unique within the plugin.
9. Every circular reference must be rejected.
10. Every unsupported field in a required area must be reported according to the validation policy.

## 17. Engine Responsibilities

The engine must:

1. Load the plugin folder.
2. Parse all manifests.
3. Validate all references.
4. Build the catalog.
5. Generate UI metadata.
6. Execute endpoints and scenarios.
7. Evaluate assertions.
8. Capture runtime variables.
9. Produce traceable results.

The engine must not:

1. Assume plugin-specific code exists.
2. Ignore invalid manifests.
3. Guess missing required values.
4. Hide compatibility errors.

## 18. UI Generation Rules

1. The UI may be generated from endpoint form definitions.
2. The UI may list modules, endpoints, scenarios, payloads, and assertions from plugin metadata.
3. The UI must not require hand-written screens for each plugin.
4. Any custom UI behavior must still be driven by the same declarative contract.

## 19. Reporting Linkage

1. Every execution must store the plugin id and version used.
2. Every report must keep the environment and scenario identity.
3. Every failed assertion must be traceable back to the endpoint or scenario step that caused it.

## 20. Non-Goals for v1

1. No plugin-specific code execution.
2. No arbitrary scripting inside plugins.
3. No hidden reflection-based extension model.
4. No ambiguous contract fields.
5. No undocumented runtime behavior.

