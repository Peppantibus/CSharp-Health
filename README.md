# CSharp-Health

[![CI](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions/workflows/ci.yml)

CSharp-Health is a developer tool to analyze C# repositories and detect
duplicated or highly similar code blocks.

The goal is to identify copy-paste patterns and near-duplicates (e.g. renamed
variables or changed literals) to improve code health and reduce repetition.

## Features
- Scan a local C# repository
- Detect duplicated code blocks:
  - Methods
  - Lambdas
  - Control blocks (if / try / loops)
- Normalization to detect near-duplicates:
  - Ignore whitespace and comments
  - Normalize identifiers
  - Normalize literals
- Group duplicates with similarity score
- CLI output in text, JSON, or Markdown

## Solution Structure

```
CSharpHealth.sln
├─ CSharpHealth.Core  # Analysis engine
├─ CSharpHealth.Cli   # Command-line interface
└─ CSharpHealth.Tests # Unit tests
```

## Build & Test

```bash
dotnet build
```

```bash
dotnet test
```

## Usage

```bash
dotnet run --project CSharpHealth.Cli -- scan <path> \
  --top 10 \
  --min-group-size 2 \
  --min-tokens 50 \
  --min-lines 6 \
  --kinds Method,Lambda,Block \
  --preview-lines 3 \
  --format text
```

### Examples

Plain text to stdout:

```bash
dotnet run --project CSharpHealth.Cli -- scan ./MyRepo --min-lines 6
```

JSON report to stdout:

```bash
dotnet run --project CSharpHealth.Cli -- scan ./MyRepo --format json
```

Markdown report to file (format inferred from extension):

```bash
dotnet run --project CSharpHealth.Cli -- scan ./MyRepo --out ./reports/dup-report.md
```

Force a specific format when writing to a file:

```bash
dotnet run --project CSharpHealth.Cli -- scan ./MyRepo --out ./reports/report.txt --format text
```

## Impact Ranking

Groups are ranked by impact, which is calculated as:

```
impact = tokenCount * (groupSize - 1)
```

This favors larger duplicate groups with more tokens, helping highlight the
most significant duplication first.

## Publishing Binaries

The CLI can be published as framework-dependent binaries:

```bash
dotnet publish CSharpHealth.Cli -c Release -r win-x64 --self-contained false
```

```bash
dotnet publish CSharpHealth.Cli -c Release -r linux-x64 --self-contained false
```

If you prefer, use the helper scripts in `scripts/`.
