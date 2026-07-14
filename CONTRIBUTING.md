# Backend Testing Studio - Contributing Guide

## 1. Before You Start

1. Read `ARCHITECTURE.md`.
2. Read `PROJECT_RULES.md`.
3. Read `PLUGIN_SPEC.md`.
4. Do not introduce behavior that conflicts with those documents.
5. If a change requires an architectural exception, propose the document change first.

## 2. How to Create a Feature

1. Define the user outcome.
2. Identify the Core contract needed for that outcome.
3. Add or update the service orchestration in Core.
4. Add the required infrastructure implementation.
5. Update UI to call the service.
6. Add tests for the use case, infrastructure, and UI behavior as appropriate.
7. Update documentation if the feature changes a rule or contract.

Feature rule:

1. Do not start in the UI if the behavior belongs in Core.
2. Do not start in infrastructure if the behavior changes a contract.

## 3. How to Create a Plugin

1. Create a new plugin folder following `PLUGIN_SPEC.md`.
2. Add the required manifest and content files.
3. Define environments, variables, modules, endpoints, payloads, assertions, and scenarios.
4. Validate the plugin against the spec before trying to execute it.
5. Add sample data and clear metadata.
6. Confirm compatibility with the engine version.

Plugin rule:

1. Plugins are declarative.
2. Do not add plugin-specific C# code to the plugin content.
3. Do not depend on undocumented fields.

## 4. How to Create a Scenario

1. Identify the plugin and environment the scenario belongs to.
2. Define the scenario goal in one sentence.
3. Break the flow into ordered steps.
4. Reference endpoints or reusable scenarios by their identifiers.
5. Define inputs, outputs, and variable effects.
6. Define expected failure behavior.
7. Add assertions where the scenario must validate results.

Scenario rule:

1. A scenario must be reproducible from its definition and inputs.
2. A scenario must not depend on hidden manual steps.

## 5. How to Create an Assertion

1. Use an existing assertion type if the need is already covered.
2. If a new assertion type is necessary, define its contract clearly.
3. Keep the assertion deterministic.
4. Define its failure message and diagnostic data.
5. Add tests for success and failure cases.

Assertion rule:

1. Assertions validate behavior; they do not change state.

## 6. How to Make a Pull Request

1. Keep the change focused.
2. Include tests when the behavior is executable.
3. Update documentation when any contract or rule changes.
4. Verify that the solution still respects the architecture and project rules.
5. Describe the behavior change and the affected layer in the PR description.
6. Call out any compatibility impact on plugins, scenarios, or saved data.

PR checklist:

1. No direct UI access to infrastructure.
2. No Core dependency on infrastructure.
3. No undocumented plugin fields.
4. No duplicated logic.
5. Tests included or an explicit justification for why they are not needed.

## 7. Review Expectations

1. Reviewers should check architecture compliance first.
2. Reviewers should check contract compatibility second.
3. Reviewers should check naming, clarity, and testability after that.
4. Any rule conflict must be resolved before merge.

