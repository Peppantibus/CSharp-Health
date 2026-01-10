#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION=${1:-Release}

dotnet publish CSharpHealth.Cli -c "$CONFIGURATION" -r linux-x64 --self-contained false
