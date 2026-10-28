#!/usr/bin/env bash
set -euo pipefail

dotnet restore src/Api

dotnet tool restore

dotnet ef database update --project src/Api
