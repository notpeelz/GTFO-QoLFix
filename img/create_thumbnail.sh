#!/usr/bin/env bash

case "$(uname -s)" in
  CYGWIN*|MINGW32*|MSYS*|MINGW*)
    is_cygwin=1
    ;;
  *)
    is_cygwin=0
    ;;
esac

[[ "$is_cygwin" -eq 1 ]] && file=$(cygpath -u "$1")
[[ -z "$file" ]] && exit 1

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

out="$(dirname "$file")"
filename="$(basename "$file")"
frame1="$out/$filename.png"
thumbnail="$out/${filename%.*}_thumbnail.jpg"

if [[ "$is_cygwin" -eq 1 ]]; then
  out="$(cygpath -w "$out")"
  frame1="$(cygpath -w "$frame1")"
  thumbnail="$(cygpath -w "$thumbnail")"
fi

ffmpeg -y -i "$file" -vframes 1 -s 852x480 -f image2 "$frame1"

magick convert "$frame1" "$DIR/playbtn.png" -gravity center -geometry 128x128+0+0 -composite -append "$thumbnail"

rm "$frame1"
