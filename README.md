# NameGuard.ML.Core

Detect whether a name input is a real human name or fake / junk. Fast (microseconds per call), self-contained (embedded model), runs entirely offline. Targets `net8.0`.

[![NuGet](https://img.shields.io/nuget/v/NameGuard.ML.Core.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/NameGuard.ML.Core/)
[![Downloads](https://img.shields.io/nuget/dt/NameGuard.ML.Core.svg?label=Downloads&logo=nuget)](https://www.nuget.org/packages/NameGuard.ML.Core/)
[![CI](https://img.shields.io/github/actions/workflow/status/encryptedtouhid/NameGuard.ML/ci.yml?branch=main&label=CI&logo=github)](https://github.com/encryptedtouhid/NameGuard.ML/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Install

```bash
dotnet add package NameGuard.ML.Core
```

## Use

```csharp
using NameGuard.ML.Core;

var guard = new NameGuard();
var result = guard.Check("Mary Johnson");

result.IsReal   // true
result.Score    // 1.00 (0..1, higher = more likely real)
result.Reason   // "ML model"
```

## ASP.NET Core integration

Drop-in `[RealName]` attribute:

```csharp
using System.ComponentModel.DataAnnotations;
using NameGuard.ML.Core;

public sealed class RealNameAttribute : ValidationAttribute
{
    private static readonly NameGuard Guard = new();
    public float MinScore { get; set; } = 0.5f;

    public override bool IsValid(object? value)
    {
        if (value is not string name) return true;
        var result = Guard.Check(name);
        return result.IsReal && result.Score >= MinScore;
    }
}

public sealed class SignupRequest
{
    [Required, RealName(MinScore = 0.7f, ErrorMessage = "Please enter a valid name.")]
    public string FullName { get; set; } = "";
}
```

`ModelState.IsValid` is now false for `asdfgh`, `qwerty`, `aaaa`, etc.

## How it works

Two-stage pipeline:

1. **Heuristic fast-path** (~0.2 µs) — catches obvious junk: keyboard rolls (`qwerty`), no vowels (`xkqzpw`), repeating chars (`aaaa`), all-digits, length bounds.
2. **ML.NET FastTree classifier** (~22 µs) — character n-grams (1–4, TF-IDF) trained on 175 countries × 40 tokens = 17,500 real samples.

The trained model (~1 MB) is embedded in the assembly. No runtime downloads, no Python, no external services.

## Sample predictions

```
Mary Johnson          -> REAL (1.00)  ML model
Khaled Hossain        -> REAL (1.00)  ML model
Yuki Tanaka           -> REAL (1.00)  ML model
Nikolai Lobachevsky   -> REAL (1.00)  ML model
asdfgh                -> FAKE (0.00)  Keyboard roll detected
xkqzpw                -> FAKE (0.00)  No vowels
aaaaaaa               -> FAKE (0.00)  Repeating character
12345                 -> FAKE (0.00)  No letters
```

## Quality

| | Holdout (20%) | 5-fold CV |
|---|---:|---:|
| AUC      | 0.9997 | 0.9996 |
| Accuracy | 0.9942 | 0.9919 |
| F1       | 0.9942 | 0.9919 |

Verified on 197 names from every UN member state + observers: **197/197 REAL at score ≥ 0.98**.

## API

```csharp
namespace NameGuard.ML.Core;

public interface INameGuard
{
    NamePrediction Check(string name);
}

public sealed class NameGuard : INameGuard
{
    public NameGuard(float threshold = 0.5f);
    public NameGuard(Stream modelStream, float threshold = 0.5f);
    public NamePrediction Check(string name);
}

public sealed class NamePrediction
{
    public bool   IsReal { get; }   // Score >= threshold
    public float  Score  { get; }   // 0..1
    public string Reason { get; }   // Why this verdict was returned
}
```

## Limitations

- **Single-token names** (`Akihito`, `Pyotr`) score lower — pass full names where possible.
- **Dictionary-word combos** (`Lorem Ipsum`, `Test Test`) pass — layer a stop-word check above if needed.
- **Latin-script only** — diacritics are stripped; romanize Cyrillic / CJK / Arabic input.
- **`Check()` is not thread-safe** (ML.NET `PredictionEngine` limitation) — pool one instance per worker, or guard with a lock.

## Links

- **Source & issues** — https://github.com/encryptedtouhid/NameGuard.ML
- **Changelog** — [CHANGELOG.md](https://github.com/encryptedtouhid/NameGuard.ML/blob/main/CHANGELOG.md)
- **Contributing & releasing** — [CONTRIBUTING.md](https://github.com/encryptedtouhid/NameGuard.ML/blob/main/CONTRIBUTING.md)
- **License** — [MIT](LICENSE)

If you find this useful, ⭐ [the repo on GitHub](https://github.com/encryptedtouhid/NameGuard.ML).
