#!/usr/bin/env bash
set -euo pipefail

SCRIPT="$(readlink -f "$BASH_SOURCE")"
BASEDIR="$(dirname "$SCRIPT")"

case "$(uname -s)" in
  CYGWIN*) isCygwin=1;;
  MINGW*)  isCygwin=1;;
  *)       isCygwin=0;;
esac

function join_by { local d=${1-} f=${2-}; if shift 2; then printf %s "$f" "${@/#/$d}"; fi; }

function checkBin() {
  local bin="$1"
  local binpath exitcode

  binpath="$(command -v "$bin")"
  exitcode="$?"

  if [[ "$exitcode" -ne "0" ]]; then
    echo "Unable to find binary '$bin' in your PATH"
    return 1
  fi

  [[ -x "$binpath" ]] && return 0

  echo "Binary '$bin' located at '$binpath' is not executable"
  return 1
}

checkBin dotnet || exit 1
checkBin unzip || exit 1
checkBin jq || exit 1

tmp_build_dir="$(mktemp -d)"
if [[ "$isCygwin" -eq 1 ]]; then
  tmp_build_dir="$(cygpath -w "$tmp_build_dir")"
fi

trap "{ exitcode=\$?; rm -rf \"$tmp_build_dir\"; exit \$exitcode; }" EXIT

packager_dll="$tmp_build_dir/packager/Packager.dll"
modpack_readme="$tmp_build_dir/packager/README.md"

MODPACK_NAME="QoLFix"
MODPACK_AUTHOR="notpeelz"
MODPACK_VERSION="1.0.0"
MODPACK_DESCRIPTION="A general GTFO improvement mod that aims to fix various quality of life issues."
MODPACK_WEBSITE="https://github.com/notpeelz/GTFO-QoLFix"
MODPACK_DEPENDENCY_BLACKLIST=()
README_PROJECT_REFERENCE=("$BASEDIR/QoL."*)

# Only keep the Qol.* folder names
for i in "${!README_PROJECT_REFERENCE[@]}"; do
  README_PROJECT_REFERENCE[$i]="$(basename ${README_PROJECT_REFERENCE[$i]})"
done

rm -rf "$BASEDIR/build"

# Build mod packages
dotnet build -c Release-Thunderstore -o "$tmp_build_dir/pkgs"
mkdir -p "$BASEDIR/build/pkgs"
cp "$tmp_build_dir/pkgs/thunderstore/"* "$BASEDIR/build/pkgs"

# Aggregate dependency strings from from manifests
MODPACK_DEPENDENCY=()
for zip in "$BASEDIR/build/pkgs/"*.zip; do
  [[ -z "$zip" ]] && continue

  name="$(unzip -p "$zip" manifest.json | jq -r '.name')"

  # Skip if the mod is blacklisted from the dependency list
  if [[ " ${MODPACK_DEPENDENCY_BLACKLIST[@]} " =~ " $name " ]]; then
    echo "Skipping dependency: $name"
    continue
  fi

  echo "Adding dependency: $name"

  depStr="$(unzip -p "$zip" manifest.json | jq -r '"\(.namespace)-\(.name)-\(.version_number)"')"
  MODPACK_DEPENDENCY+=("$depStr")
done

# Build modpack package
dotnet msbuild -restore "$BASEDIR/Packager" \
  -p:"Configuration=Release-Thunderstore" \
  -p:"OutputPath=\"$tmp_build_dir/packager\"" \
  -p:"RanFromBuildScript=true" \
  -p:"ReadmeProjectReferences=\"$(join_by ';' "${README_PROJECT_REFERENCE[@]}")\""
dotnet "$packager_dll" \
  --output "$BASEDIR/build" \
  --icon "$BASEDIR/img/logo.png" \
  --readme "$modpack_readme" \
  --name "$MODPACK_NAME" \
  --author "$MODPACK_AUTHOR" \
  --description "$MODPACK_DESCRIPTION" \
  --website "$MODPACK_WEBSITE" \
  --version "$MODPACK_VERSION" \
  --dependency "${MODPACK_DEPENDENCY[@]}"
