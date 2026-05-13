# NameGuard.ML.Core

Detect whether a name input is a real human name or fake / junk. Hybrid pipeline — a heuristic fast-path catches obvious junk in microseconds, then an ML.NET FastTree binary classifier over character n-grams (1–4, TF-IDF) handles the rest. **The trained model is embedded in the assembly** — no runtime downloads, no Python, no extra files.

Targets `net8.0`. Licensed under [MIT](LICENSE) — free for any use, including commercial.

[![NuGet](https://img.shields.io/nuget/v/NameGuard.ML.Core.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/NameGuard.ML.Core/)
[![Downloads](https://img.shields.io/nuget/dt/NameGuard.ML.Core.svg?label=Downloads&logo=nuget)](https://www.nuget.org/packages/NameGuard.ML.Core/)
[![CI](https://img.shields.io/github/actions/workflow/status/encryptedtouhid/NameGuard.ML/ci.yml?branch=main&label=CI&logo=github)](https://github.com/encryptedtouhid/NameGuard.ML/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

---

## Why use this?

Catch fake or low-effort name input before it touches your database. Common use cases:

- **Signup / signin forms** — reject `asdfgh`, `qwerty`, `aaaaa` at the input layer
- **Lead-capture / marketing forms** — improve data quality, reduce spam leads
- **CRM data quality / dedup pipelines** — flag obviously-bad rows for human review
- **E-commerce checkout** — guest-checkout names that are clearly junk
- **Survey responses** — filter joke entries before analysis
- **Fraud / abuse signals** — one feature in a larger anti-abuse pipeline

NameGuard runs in microseconds per call, ships with the trained model embedded in the assembly, and operates entirely offline — no API calls, no external services, no Python runtime.

---

## Install

```bash
dotnet add package NameGuard.ML.Core
```

Or in your csproj:

```xml
<PackageReference Include="NameGuard.ML.Core" Version="0.1.0" />
```

---

## Quick start

```csharp
using NameGuard.ML.Core;

var guard = new NameGuard();              // loads the embedded model once
var result = guard.Check("Mary Johnson");

Console.WriteLine(result.IsReal);         // True
Console.WriteLine(result.Score);          // 1.00
Console.WriteLine(result.Reason);         // "ML model"
```

That's the whole API for the typical case. One instance, one method.

---

## Validation framework integration

### ASP.NET Core DataAnnotations

Drop-in `[RealName]` attribute that plays nicely with `[Required]`, `ModelState`, and any DataAnnotations-aware UI binder:

```csharp
using System.ComponentModel.DataAnnotations;
using NameGuard.ML.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RealNameAttribute : ValidationAttribute
{
    // One shared instance — model load is amortized across all validations.
    private static readonly NameGuard Guard = new();

    public float MinScore { get; set; } = 0.5f;

    public override bool IsValid(object? value)
    {
        if (value is not string name) return true; // let [Required] handle nulls
        var result = Guard.Check(name);
        return result.IsReal && result.Score >= MinScore;
    }
}

// Apply it
public sealed class SignupRequest
{
    [Required, RealName(MinScore = 0.7f, ErrorMessage = "Please enter a valid name.")]
    public string FullName { get; set; } = "";
}
```

`ModelState.IsValid` will now be `false` for `asdfgh`, `qwerty`, `aaaa`, and similar inputs — with no extra wiring in your controllers.

### FluentValidation

```csharp
using FluentValidation;
using NameGuard.ML.Core;

public sealed class SignupValidator : AbstractValidator<SignupRequest>
{
    private static readonly NameGuard Guard = new();

    public SignupValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .Must(name => Guard.Check(name).IsReal)
            .WithMessage("Please enter a valid name.");
    }
}
```

> **Thread safety:** the `Guard` field is shared across requests — that's safe for construction but the underlying `Check()` is not concurrent. For a high-RPS web app, either wrap `Check()` in a `lock`, or use one instance per request (cheap after the first construction, since the model itself is JIT-cached after first load).

---

## Sample predictions

```
Khaled Md Tuhidul Hossain      -> REAL (score=1.00) — ML model
Mary Johnson                   -> REAL (score=1.00) — ML model
Yuki Tanaka                    -> REAL (score=1.00) — ML model
Nikolai Lobachevsky            -> REAL (score=1.00) — ML model
Mohamed Ben Ali                -> REAL (score=1.00) — ML model
Joao Silva                     -> REAL (score=0.98) — ML model
asdfgh                         -> FAKE (score=0.00) — Keyboard roll detected
qwerty                         -> FAKE (score=0.00) — Keyboard roll detected
xkqzpw                         -> FAKE (score=0.00) — No vowels
aaaaaaa                        -> FAKE (score=0.00) — Repeating character
bcdfgh                         -> FAKE (score=0.00) — No vowels
12345                          -> FAKE (score=0.00) — No letters
dfsd sdfsdf                    -> FAKE (score=0.00) — No vowels
```

---

## API

```csharp
namespace NameGuard.ML.Core;

public interface INameGuard
{
    NamePrediction Check(string name);
}

public sealed class NameGuard : INameGuard
{
    public const float DefaultThreshold = 0.5f;

    // Loads the embedded model.
    public NameGuard(float threshold = DefaultThreshold);

    // Loads a custom model from a stream (advanced — usually unnecessary).
    public NameGuard(Stream modelStream, float threshold = DefaultThreshold);

    public NamePrediction Check(string name);
}

public sealed class NamePrediction
{
    public bool   IsReal { get; init; }   // Score >= threshold
    public float  Score  { get; init; }   // 0..1 (higher = more likely real)
    public string Reason { get; init; }   // Why this verdict was returned
}
```

### `Reason` values

| Reason | When |
|---|---|
| `"ML model"` | Classifier returned a probability >= threshold |
| `"ML model: low score"` | Probability < threshold |
| `"Keyboard roll detected"` | `qwerty`, `asdfgh`, `zxcvbn`, etc. |
| `"No vowels"` | `bcdfgh`, `xkqzpw` |
| `"Repeating character"` | `aaaaaa` |
| `"Long repeating run"` | 4+ identical letters in a row |
| `"Too short"` | Length < 2 (after trimming) |
| `"Too long"` | Length > 60 |
| `"No letters"` | `12345`, `!!!!!` |
| `"Mostly digits"` | `a1234567` |

---

## Decision pipeline

```
input ──▶ trim ──▶ heuristic filter ──┬─▶ reject (with human-readable reason)
                                      └─▶ ML.NET FastTree classifier ──▶ Score 0..1
```

1. **Heuristic fast-path** — length bounds, all-digits, no-vowel, repeating-char, keyboard-walk detection. Catches obvious junk in microseconds with interpretable reasons.
2. **ML model** — only invoked if heuristics don't reject. Character n-gram (1–4) TF-IDF features → FastTree binary classifier.

Two-stage design keeps obvious bad inputs cheap and gives you a useful `Reason` for every rejection.

---

## Performance

### Model quality

The bundled model was trained on names from 175 countries (≈7,000 unique authored tokens combined into 17,500 real samples, balanced with 17,500 synthesized junk samples).

| Metric | Holdout (20%) | 5-fold CV |
|---|---|---|
| AUC | 0.9997 | 0.9996 |
| Accuracy | 0.9942 | 0.9919 |
| F1 | 0.9942 | 0.9919 |

**External verification:** a probe set of 197 representative names spanning every UN member state plus observers (Vatican, Taiwan, Kosovo, Palestine) was classified **197/197 as REAL** at score ≥ 0.98.

### Inference speed

Single-threaded, `dotnet run -c Release`, Apple Silicon (M-series), .NET 8 (measure on your hardware with `dotnet run --project NameGuard.ML.Example -c Release -- --benchmark`):

| Code path | Per call | Throughput |
|---|---:|---:|
| Heuristic fast-path (`asdfgh`, `aaaa`, `12345`, etc.) | **0.17 µs** | ~6.0 M ops/sec |
| ML model (real-looking inputs) | **22 µs** | ~45 K ops/sec |
| Cold start (constructor + first inference) | ~200 ms | — |

The heuristic fast-path is ~130× faster than the ML path — and catches a meaningful share of bad input — which is the whole point of the two-stage design.

### Footprint

- `NameGuard.ML.Core.dll`: **~976 KB** (includes the embedded model)
- Embedded `model.zip`: **~965 KB**
- No runtime allocations beyond the prediction engine (initialized once per `NameGuard` instance)
- No network, no filesystem, no external dependencies at runtime

---

## Thread safety

- Constructor: thread-safe.
- `Check()`: **not** thread-safe under the hood (ML.NET `PredictionEngine` is not). For concurrent use, either pool one `NameGuard` instance per worker or guard `Check()` with a lock.

---

## Known limitations

- **Single-token names** classify with lower confidence — ~95% of training data is `Given Surname` pairs, so isolated tokens like `Akihito` or `Pyotr` can fall below threshold. Pass a full name where possible.
- **Dictionary-word combos** like `Lorem Ipsum` or `Test Test` statistically look like names and pass. If you need to reject these, layer a stop-word / dictionary check above NameGuard.
- **Latin-script only** — the pipeline strips diacritics and lowercases at training and inference. Romanize Cyrillic / CJK / Arabic input before passing in.
- **Smaller-country coverage** in the seed dataset is weaker for some African, Pacific-Islander, and microstate naming patterns. Scores still typically > 0.95 in those regions, but with slightly lower confidence.

---

## Building from source

Requires .NET 8 SDK.

```bash
dotnet build NameGuard.ML.sln -c Release
dotnet test  NameGuard.ML.sln -c Release
```

### Retrain the embedded model

When you change `NameGuard.ML.Trainer/Data/world-names.json`:

```bash
dotnet run --project NameGuard.ML.Trainer -c Release
```

Output: a fresh `model.zip` (~1 MB) written into `NameGuard.ML.Core/Resources/`. The next `dotnet build` of `NameGuard.ML.Core` embeds it automatically. Training is deterministic (seed 42) and takes ~10 seconds.

### Pack the NuGet locally

```bash
dotnet pack NameGuard.ML.Core/NameGuard.ML.Core.csproj -c Release -o ./nupkg
```

CI runs the same on every push to `main` and every PR — see `.github/workflows/ci.yml`.

---

## Versioning & releases

This project follows [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`). The version is **derived automatically from git tags** via [MinVer](https://github.com/adamralph/minver) — there is no `<Version>` hardcoded in any `.csproj`.

| Git state | Resulting package version |
|---|---|
| Tag `v1.2.3` at HEAD | `1.2.3` (clean release) |
| `N` commits past tag `v1.2.3` | `1.2.4-alpha.0.N` (dev preview) |
| No tags yet | `0.0.0-alpha.0.<total commits>` |
| Tag `v1.2.3-rc.1` | `1.2.3-rc.1` (pre-release) |

### Cutting a release

1. Update `CHANGELOG.md` — move items from `[Unreleased]` into a new `[X.Y.Z]` section.
2. Commit with a message like `Release v1.2.3`.
3. Tag and push:
   ```bash
   git tag v1.2.3
   git push origin main --tags
   ```
4. The CI publish job triggers automatically on the `v*` tag and:
   - Packs `NameGuard.ML.Core.1.2.3.nupkg` and `.snupkg` (symbols)
   - Pushes both to nuget.org using the `NUGET_API_KEY` secret

### One-time setup before the first release

1. Create a NuGet API key at https://www.nuget.org/account/apikeys (scope: push `NameGuard.ML.Core`).
2. Add it to the GitHub repo: **Settings → Secrets and variables → Actions → New repository secret**, name = `NUGET_API_KEY`.

### Local dev builds

Untagged commits produce `0.0.0-alpha.0.<n>` versions when you run `dotnet pack` — these are intended for local testing only, not for publishing. CI also produces these as build artifacts on every push.

See [CHANGELOG.md](CHANGELOG.md) for the release history.

---

## Repository layout

```
NameGuard.ML.sln
├── NameGuard.ML.Core       Public API + embedded model (this package)
├── NameGuard.ML.Trainer    Console — retrains the model from world-names.json
├── NameGuard.ML.Example    Console — CLI / REPL demo
└── NameGuard.ML.Test       xUnit — 43 tests
```

---

## Contributing

Issues and pull requests welcome. Areas where outside help is especially useful:

- **Extending `world-names.json`** — add or refine per-country given-name / surname pools. Native speakers and people with regional knowledge can dramatically improve coverage for under-represented cultures.
- **Reducing false positives** for dictionary-word inputs like `Lorem Ipsum` or `Test Test`.
- **Single-token support** — improving recall for names passed without a surname.
- **Non-Latin script handling** — currently the pipeline strips diacritics and lowercases; richer Unicode handling could be valuable.

If you're contributing a code change, please:

1. Open an issue first for non-trivial changes so we can agree on the approach.
2. Keep the test suite green (`dotnet test NameGuard.ML.sln -c Release`).
3. If you change the training pipeline or dataset, regenerate `model.zip` (`dotnet run --project NameGuard.ML.Trainer -c Release`) and include it in your PR.

## License

Released under the [MIT License](LICENSE).

Copyright (c) 2026 Khaled Md Tuhidul Hossain

---

## If you find this useful

⭐ **[Star NameGuard on GitHub](https://github.com/encryptedtouhid/NameGuard.ML)** — it helps others discover the project and lets me know the work is appreciated.

Spotted a bug or want a feature? [Open an issue](https://github.com/encryptedtouhid/NameGuard.ML/issues).

Want to support continued development? [GitHub Sponsors](https://github.com/sponsors/encryptedtouhid) is the easiest way.
