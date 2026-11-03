#!/bin/bash
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project="$(cd "$script_dir/../.." && pwd)"
editor='/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity'
case "${1:-}" in
    mac) target=StandaloneOSX; method=AppleBuild.ExportMac ;;
    ios) target=iOS; method=AppleBuild.ExportIos ;;
    *) echo 'Use mac or ios'; exit 2 ;;
esac
cd "$project"
mkdir -p Logs
"$editor" -batchmode -nographics -quit -projectPath "$project" -buildTarget "$target" -executeMethod "$method" -logFile "$project/Logs/Build-$1.log"

if [ "$1" = "ios" ]; then
    python3 Tools/Apple/PrepareXcode.py
fi
