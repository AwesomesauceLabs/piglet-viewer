#!/bin/bash
set -eu -o pipefail

unity='/mnt/c/Program Files/Unity-2018.3.0f2/Editor/Unity.exe'

tmpdir='/mnt/c/Users/Ben/tmp'
mkdir -p "$tmpdir"

src='/mnt/c/Users/Ben/git/bitbucket/piglet/'
dest=$(mktemp -d -p "$tmpdir" piglet.XXXXXXX)

http_port=8000

echo "cloning $src to $dest..."
rsync -a --exclude-from - "$src" "$dest" <<EOF
- /[Ll]ibrary/
- /[Ll]ogs/
- /[Tt]emp/
- /[Oo]bj/
- /[Bb]uild/
- /[Bb]uilds/
- /[Pp]ackages/
- /[Pp]lugins/
EOF


echo "running WebGL build in $dest..."
cd "$dest"
"$unity" -quit -batchmode -projectPath $(win-path "$dest") -executeMethod WebGLBuilder.Build

echo "serving WebGL app at http://localhost:8000/index.html..."
cd WebGL-Dist
webfsd -F -p $http_port
