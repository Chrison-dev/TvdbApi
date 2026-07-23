# CLAUDE.md — TvdbApi

C# client for **TheTVDB v4 API**, published as three NuGet packages. Read this before changing build, CI, codegen, or the public surface.

## ⚠️ CI/CD: Fallout owns the workflows — NEVER hand-edit `.github/workflows/*.yml`

This repo builds with **[Fallout](https://github.com/Fallout-build/Fallout)** (a NUKE fork). **All GitHub Actions workflows are GENERATED** from `[GitHubActions(...)]` attributes on `build/Build.cs`.

- **Do not edit `.github/workflows/*.yml` by hand.** They are generated artifacts.
- To change CI/publishing: edit the `[GitHubActions]` attribute(s) + build targets in `build/Build.cs` (use `IConfigureGitHubActions` / custom steps for things Fallout doesn't model natively, e.g. OIDC publish, GitHub Release), then **regenerate**:
  ```sh
  ./build.ps1                 # regenerates workflows as part of a build
  # or explicitly:
  dotnet run --project build/_build.csproj -- --generate-configuration GitHubActions_<name> --host GitHubActions
  ```
- Same rule for `.github/release.yml` if it can be driven from the build; otherwise treat generated files as generated.

## Build & tooling

- Run the build via `./build.ps1 <Target>` (bootstraps `dotnet run --project build/_build.csproj`). Targets: `Generate`, `Test`, `Pack`.
- **In-stack tooling only**: Fallout targets (C#) or PowerShell. **No Python/Bash** scripts for repo tooling.

## Codegen — don't hand-edit generated code

- Clients + models are generated from TheTVDB's OpenAPI spec via the Fallout **`Generate`** target (NSwag's C# API, in `build/Build.cs`). Run `./build.ps1 Generate`.
- The overlay (in `Build.cs`) fixes spec quirks in-memory: coerce integer params `number`→`long`, hoist inline param enums, and **strip the `{ data, status, links }` envelope** from responses.
- `src/TvdbClient.Models/TvdbModels.cs` and `src/TvdbClient/Clients/TvdbClient.cs` are **generated** — never hand-edit; change the overlay/settings and regenerate.

## Architecture — 3 projects (enforced by `ArchitectureSpecs`)

| Project | Namespace | Role | Versioning |
|---|---|---|---|
| `TvdbClient.Models` | `Tvdb.Models` | generated DTOs (the leaf) | tracks TheTVDB API version |
| `TvdbClient.Abstractions` | `Tvdb.Abstractions` | generic contracts/config/envelope (`Page<T>`) | generic |
| `TvdbClient` | `Tvdb.Clients` / `Tvdb.Paging` / … | generated clients + auth/DI/facade | generic |

`Models` must depend on nothing else; `Abstractions` must not depend on the core. `NetArchTest` specs enforce this — keep them green.

**Response shape:** clients return the entity `T` (single) / `ICollection<T>` (list); the `{data}` envelope is peeled at runtime by `EnvelopeUnwrappingHandler`. Paginated endpoints expose `Page<T>` via `GetPageAsync` extensions.

## Testing — Fallout spec convention

- **xUnit + FluentAssertions + Verify + NetArchTest + Mockly** (HTTP mocking). Public-API stability via **PublicApiGenerator + Verify** snapshots.
- Test packages are injected for `*.Specs` projects via `tests/Directory.Build.props`. **No Central Package Management** (per-project versions) — deliberate.
- One `<ClassUnderTest>Specs` per class; snake_case behavioural test names.

## Versioning & publishing

- **GitVersion**: `Major.Minor` = TheTVDB API version, **patch = our release counter**. Cut a release by tagging `v<Major>.<Minor>.<patch>` (e.g. `v4.7.12`).
- Publish (on tag, via the generated publish workflow): **NuGet.org Trusted Publishing (OIDC)** + **GitHub Packages** + a **GitHub Release** with the `.nupkg` files attached and label-categorized notes.
- Shared external accounts (nuget.org, GitHub org): add-only; don't disturb existing packages/policies.
