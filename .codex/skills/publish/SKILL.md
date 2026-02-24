---
name: publish
description: Executes publish.ps1 to build images, push to the registry, and trigger a Coolify service redeployment.
---

# Execution Directives

## Prerequisites
1. Set execution context to the repository root.
2. Validate the existence of `VERSION.txt`.
3. Validate the `.env` file contains required keys: `DOCKER_REGISTRY`, `COOLIFY_API_TOKEN`, `COOLIFY_BASE_URL`, `COOLIFY_SERVICE_UUID`.

## Execution
Run the PowerShell script:
```powershell
& ./publish.ps1