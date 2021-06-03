#!/usr/bin/env bash

SCRIPT="$(readlink -f "$BASH_SOURCE")"
BASEDIR="$(dirname "$SCRIPT")"

dotnet run --project "$BASEDIR/Packager" --configuration Debug
dotnet run --project "$BASEDIR/Packager" --configuration Release-Standalone
dotnet run --project "$BASEDIR/Packager" --configuration Release-Thunderstore
