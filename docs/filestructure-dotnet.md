# SquareBuddy WebApi File Structure (Feature-First)

This document describes the current .NET `SquareBuddy.WebApi` folder structure used in SquareBuddy. The goal is to keep files close to the feature they belong to.

## Principles

- Code is grouped by feature or usage, not by technical type.
- Feature folders should contain endpoints, services, and related models for that domain.
- Cross-cutting setup code lives in focused infrastructure/extension folders.
- Root files are limited to host composition and project-level config.

## Current Structure

```text
SquareBuddy.WebApi/
|- AppJsonContext.cs
|- Program.cs
|- README.md
|- SquareBuddy.WebApi.csproj
|- config.yaml
|- test.http
|- Dockerfile
|- Dockerfile.AOT
|- .gitignore
|- Properties/
|  |- launchSettings.json
|- Core/
tbd
|- Identity/
|  |- IdentityBuilderExtensions.cs
|  |- IdentityEndpoints.cs
|  |- LoggingEmailSender.cs
|- Infrastructure/
|  |- AiBuilderExtensions.cs
|  |- EntityFrameworkBuilderExtensions.cs
|  |- MinioBuilderExtensions.cs
|  |- SwaggerBuilderExtensions.cs
|- Extensions/
|  |- IHostExtensions.cs
|- wwwroot/
```

## Folder Responsibilities

- `Core/`: Story and board domain behavior, streaming workflow, scene graph generation, and related API endpoints.
- `Identity/`: Auth endpoint mapping and identity-specific setup/services.
- `Infrastructure/`: External integrations and app wiring (AI, EF Core, MinIO, Swagger).
- `Extensions/`: host-level extension helpers that are not feature-specific.

## Guidance for New Work

- Add new feature files under the feature folder that owns the behavior.
- Keep builder/service-registration extensions with the feature unless they are broad infrastructure concerns.
- Avoid per-type folders (`Controllers`, `Services`, `Models`) unless size forces a split.
