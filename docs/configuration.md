# Configuration Design Decision

## Context

The service requires configuration for infrastructure dependencies (e.g. MinIO).
Historically, configuration was provided exclusively via environment variables using
flat keys (`MINIO_ENDPOINT`, `MINIO_ACCESS_KEY`, etc.) with explicit runtime validation.

During refactoring, the standard .NET *Options pattern* (`IOptions<T>`, sections,
binders, validators) was evaluated but introduced unnecessary complexity and
edge cases around naming conventions, binding rules, and provider-specific behavior
(especially when mixing environment variables, JSON/YAML, and Docker conventions).

The goal was to arrive at a configuration approach that is:
- predictable
- easy to reason about
- friendly to Docker/Kubernetes
- simple to validate
- reusable across multiple configuration blocks

---

## Decision

We use **explicit configuration access via `IConfiguration` extensions**, combined with
**construction-time validation**, instead of the .NET Options pattern.

Each infrastructure dependency exposes a method of the form:

```csharp
builder.Configuration.GetXxxOptions()
```

This method:

- Reads configuration values directly from IConfiguration
- Applies defaults if appropriate
- Validates required and derived values
- Throws clear, fail-fast exceptions on invalid configuration
- Returns a fully valid, immutable options object

Shared helper methods are used to generate consistent configuration errors.

### Example

var options = builder.Configuration.GetMinioOptions();

Validation happens inside GetMinioOptions.

If the method returns, the options object is guaranteed to be valid.

### Configuration Sources

Configuration is merged from:

- YAML files (appsettings.yaml, appsettings.{Environment}.yaml) for defaults
- Environment variables for overrides

Flat configuration keys are used intentionally:

- MINIO_ENDPOINT: http://localhost:9000
- MINIO_ACCESS_KEY: minioadmin
- MINIO_SECRET_KEY: minioadmin
- MINIO_BUCKET: squarebuddy

Environment variables override YAML values automatically.

### Why Not IOptions<T>

The standard Options pattern was rejected for this use case because it:

- introduces implicit binder behavior
- depends on naming conventions (__, _, casing)
- obscures the true source of values
- complicates debugging in containerized environments
- adds framework coupling where it provides little value

For small, infrastructure-focused configuration blocks, explicit access is clearer
and more robust.

### Benefits

- No hidden binding rules
- No section or naming ambiguity
- Docker- and Kubernetes-friendly
- Validation is explicit and colocated
- Errors are clear and actionable
- Easy to unit test
- Minimal framework coupling

### Trade-offs

- No automatic model binding
- No attribute-based validation
- Slightly more code per configuration block

These trade-offs are accepted in favor of clarity, predictability, and operational
simplicity.

### Conclusion

This approach favors explicitness over abstraction.

Configuration is treated as an external contract that must be validated early and
fail fast. The resulting code is boring, predictable, and easy to operate — which is
the desired outcome for infrastructure configuration.
