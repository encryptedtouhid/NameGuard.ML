# Changelog

All notable changes to this project are documented here. This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html) and the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

Versions are derived automatically from git tags via [MinVer](https://github.com/adamralph/minver). See [CONTRIBUTING.md](CONTRIBUTING.md#versioning--releases) for the release workflow.

## [Unreleased]

### Fixed
- Mixed-junk inputs like `"Khaled asd asd"` or `"zephy xyz"` no longer score REAL. Previously the max-score aggregation across tokens let one strong token rescue obvious junk siblings. `NameGuard.Check` now runs a strict per-token reject (keyboard / alphabet rolls, long repeating runs) before falling through to ML.
- Short keyboard-row fragments (`asd`, `sdf`, `dfg`, `qwe`, …) and alphabet-adjacent rolls (`abc`, `xyz`, …) are now caught by the heuristic. Keyboard-roll min length lowered from 4 to 3; the alphabet itself was added as a roll row.

### Added
- `JunkDetector.TryRejectToken` — strict per-token reject helper that skips the looser whole-string checks (length, no-vowel, digits) so short particles like `Mr`/`Jr` and initials aren't false-rejected.

## [0.1.1] — 2026-05-13

### Changed
- Simplified `README.md` from 361 to 130 lines — now consumer-focused: install, use, API, sample predictions, limitations.
- Moved build, retrain, benchmark, repo-layout, versioning, and contribution guidance to a new `CONTRIBUTING.md`.

## [0.1.0] — 2026-05-13

Initial release.

### Added
- `NameGuard.ML.Core` class library with the public `INameGuard` / `NameGuard` API.
- Embedded FastTree binary classifier (character n-grams 1–4, TF-IDF) trained on 17,500 real samples drawn from 175 countries.
- Heuristic fast-path (`JunkDetector`) for obvious junk: keyboard rolls, no-vowel, repeating chars, length bounds, all-digits, mostly-digits.
- `NameGuard.ML.Trainer` console app that retrains the model from `Data/world-names.json`.
- `NameGuard.ML.Example` console app with CLI / REPL / `--benchmark` modes.
- `NameGuard.ML.Test` xUnit suite (43 tests across heuristics, predictor golden cases, and edge cases).
- GitHub Actions CI: build, test, pack, upload artifacts on every push and PR; auto-publish to nuget.org on `v*` tags.
- Automated versioning via MinVer (derives `PackageVersion` from git tags).
- `Directory.Build.props` centralizing shared NuGet metadata across all projects.
- SourceLink (Microsoft.SourceLink.GitHub) so package consumers can step into NameGuard source from their debugger.
- Deterministic builds in CI (`ContinuousIntegrationBuild=true` when `GITHUB_ACTIONS=true`).

### Model quality
- Holdout AUC `0.9997`, Accuracy `0.9942`, F1 `0.9942`.
- 5-fold CV AUC `0.9996`, Accuracy `0.9919`, F1 `0.9919`.
- External verification: 197/197 representative names from every UN member state + observers classified as REAL at score ≥ 0.98.

[Unreleased]: https://github.com/encryptedtouhid/NameGuard.ML/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/encryptedtouhid/NameGuard.ML/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/encryptedtouhid/NameGuard.ML/releases/tag/v0.1.0
