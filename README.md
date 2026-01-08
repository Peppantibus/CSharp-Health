# CSharp-Health

CSharp-Health is a developer tool to analyze C# repositories and detect
duplicated or highly similar code blocks.

The goal is to identify copy-paste patterns and near-duplicates
(e.g. renamed variables or changed literals) to improve code health
and reduce repetition.

## Features (MVP)
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
- CLI output (text / JSON)

## Solution Structure

CSharpHealth.sln
├─ CSharpHealth.Core # Analysis engine
├─ CSharpHealth.Cli # Command-line interface
└─ CSharpHealth.Tests # Unit tests


## Build & Test

```bash
dotnet build
dotnet test


Usage (planned)
dotnet run --project CSharpHealth.Cli -- scan <path>


Example:

dotnet run --project CSharpHealth.Cli -- scan ./MyRepo --min-lines 6

Status

Early MVP – scaffolding and core analysis in progress.