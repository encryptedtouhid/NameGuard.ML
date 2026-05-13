# Contributing to NameGuard.ML

Issues and pull requests welcome. This doc covers how to build, test, retrain, and cut releases.

## Governance

NameGuard.ML is maintained solo by [@encryptedtouhid](https://github.com/encryptedtouhid).

- All changes to `main` go through a pull request reviewed and approved by the owner. Direct pushes to `main` are blocked.
- Release tags (`v*`) can only be created by the owner. CI does not publish on any other ref.
- Publishing to nuget.org runs in a protected deployment environment; the owner must approve each release before the package is pushed.
- Security issues must be reported privately via [GitHub Security Advisories](https://github.com/encryptedtouhid/NameGuard.ML/security/advisories/new), not as public issues.

## Build & test

Requires .NET 8 SDK.

```bash
dotnet build NameGuard.ML.sln -c Release
dotnet test  NameGuard.ML.sln -c Release
```

## Repository layout

```
NameGuard.ML.sln
├── NameGuard.ML.Core       Public API + embedded model (the published NuGet package)
├── NameGuard.ML.Trainer    Console — retrains the model from Data/world-names.json
├── NameGuard.ML.Example    Console — CLI / REPL / --benchmark
└── NameGuard.ML.Test       xUnit — 43 tests
```

## Retrain the model

After changing `NameGuard.ML.Trainer/Data/world-names.json`:

```bash
dotnet run --project NameGuard.ML.Trainer -c Release
```

Writes a fresh `model.zip` (~1 MB) into `NameGuard.ML.Core/Resources/`. The next build embeds it automatically. Training is deterministic (seed 42) and takes ~10 seconds.

## Benchmark

```bash
dotnet run --project NameGuard.ML.Example -c Release -- --benchmark
```

## Pack the NuGet locally

```bash
dotnet pack NameGuard.ML.Core/NameGuard.ML.Core.csproj -c Release -o ./nupkg
```

## Versioning & releases

Version is derived automatically from git tags via [MinVer](https://github.com/adamralph/minver) — no `<Version>` is hardcoded in any csproj.

| Git state | Package version |
|---|---|
| Tag `v1.2.3` at HEAD | `1.2.3` |
| `N` commits past `v1.2.3` | `1.2.4-alpha.0.N` |
| No tags | `0.0.0-alpha.0.<total commits>` |
| Tag `v1.2.3-rc.1` | `1.2.3-rc.1` (pre-release on nuget.org) |

### Cutting a release

1. Move items from `[Unreleased]` to a new `[X.Y.Z]` section in `CHANGELOG.md`.
2. Commit: `git commit -m "Release vX.Y.Z"`.
3. Tag and push:
   ```bash
   git tag vX.Y.Z
   git push origin main --tags
   ```
4. CI detects the `v*` tag, packs, and publishes to nuget.org via the `NUGET_API_KEY` repository secret.

## Where help is especially useful

- **Extending `world-names.json`** — native speakers / regional knowledge for under-represented cultures (especially African, Pacific-Islander, microstate naming).
- **Reducing false positives** for dictionary-word inputs like `Lorem Ipsum` or `Test Test`.
- **Single-token recall** — names passed without a surname currently score low.
- **Non-Latin script handling** — pipeline currently strips diacritics; richer Unicode could be valuable.

## PR guidelines

1. Open an issue first for non-trivial changes so we can agree on the approach.
2. Keep the test suite green (`dotnet test NameGuard.ML.sln -c Release`).
3. If you change the training pipeline or dataset, regenerate `model.zip` and include it in the PR.
