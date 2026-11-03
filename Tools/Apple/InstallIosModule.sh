#!/bin/bash
set -euo pipefail
source="$HOME/Downloads/GuerraSetup/IosExpanded/TargetSupport.pkg.tmp/Payload/iOSSupport"
destination='/Applications/Unity/Hub/Editor/6000.6.7f1/PlaybackEngines/iOSSupport'
test -d "$source"
test ! -e "$destination"
mv "$source" "$destination"
echo IOS_MODULE_INSTALLED
du -sh "$destination"
