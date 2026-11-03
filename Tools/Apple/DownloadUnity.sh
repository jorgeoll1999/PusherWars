#!/bin/bash
set -euo pipefail
cd "$HOME/Downloads/GuerraSetup"
curl --fail --location --retry 3 --connect-timeout 20 --output Unity-6000.6.7f1.pkg.part 'https://download.unity3d.com/download_unity/b22767f04f04/MacEditorInstallerArm64/Unity-6000.6.7f1.pkg'
test "$(md5 -q Unity-6000.6.7f1.pkg.part)" = '74fd87d79282a3c1d0f2ecea8821c4a5'
mv Unity-6000.6.7f1.pkg.part Unity-6000.6.7f1.pkg
pkgutil --check-signature Unity-6000.6.7f1.pkg
curl --fail --location --retry 3 --connect-timeout 20 --output Unity-iOS-6000.6.7f1.pkg.part 'https://download.unity3d.com/download_unity/b22767f04f04/MacEditorTargetInstaller/UnitySetup-iOS-Support-for-Editor-6000.6.7f1.pkg'
test "$(md5 -q Unity-iOS-6000.6.7f1.pkg.part)" = 'dca114bbde2f1a5840f23d34017453ff'
mv Unity-iOS-6000.6.7f1.pkg.part Unity-iOS-6000.6.7f1.pkg
pkgutil --check-signature Unity-iOS-6000.6.7f1.pkg
echo DOWNLOADS_COMPLETE
