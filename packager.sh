#!/usr/bin/env bash

SCRIPT="$(readlink -f "$BASH_SOURCE")"
BASEDIR="$(dirname "$SCRIPT")"

cd "$BASEDIR/scripts/packager"
npm run start -- "${@:1}"
