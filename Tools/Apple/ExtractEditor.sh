#!/bin/bash
set -euo pipefail
cd "$HOME/Downloads/GuerraSetup"
test "$(md5 -q Unity-6000.6.7f1.pkg)" = '74fd87d79282a3c1d0f2ecea8821c4a5'
pkgutil --check-signature Unity-6000.6.7f1.pkg
destination='/Applications/Unity/Hub/Editor/6000.6.7f1'
test ! -e "$destination"
pkgutil --expand-full Unity-6000.6.7f1.pkg EditorExpanded
test -d EditorExpanded/Unity.pkg.tmp/Payload/Unity/Unity.app
mv EditorExpanded/Unity.pkg.tmp/Payload/Unity "$destination"
"$destination/Unity.app/Contents/MacOS/Unity" -version
echo EDITOR_INSTALLED
