"""Validate the iOS export and reconcile bundled framework minimum OS metadata."""
import json,plistlib,re,subprocess,sys
from pathlib import Path
project=Path(__file__).resolve().parents[2]
export=project/'Builds/iOS/Xcode'
key=lambda v:tuple(map(int,v.split('.')))
pbx=export/'Unity-iPhone.xcodeproj/project.pbxproj'
settings=pbx.read_text()
deployment=max(re.findall(r'IPHONEOS_DEPLOYMENT_TARGET = ([0-9.]+)',settings),key=key)
# Xcode wraps Unity's static runtime archive as a dynamic framework.
# Use one deployment floor for the wrapper and its copied Info.plist.
settings=re.sub(r'IPHONEOS_DEPLOYMENT_TARGET = [0-9.]+', 'IPHONEOS_DEPLOYMENT_TARGET = '+deployment,settings)
pbx.write_text(settings)
for framework in (export/'Frameworks').glob('*.framework'):
 info=framework/'Info.plist'
 if not info.exists():continue
 data=plistlib.loads(info.read_bytes());binary=framework/data.get('CFBundleExecutable',framework.stem)
 if not binary.exists():continue
 with binary.open('rb') as stream:static=stream.read(8)==b'!<arch>\n'
 if static:
  actual=deployment
 else:
  output=subprocess.check_output(['xcrun','vtool','-show-build',str(binary)],text=True)
  minimum=re.findall(r'\bminos\s+([0-9.]+)',output)
  if not minimum:continue
  actual=max(minimum,key=key)
 declared=data.get('MinimumOSVersion','0')
 if key(actual)>key(declared):
  data['MinimumOSVersion']=actual;info.write_bytes(plistlib.dumps(data,sort_keys=False))
  print(f'{framework.name}: MinimumOSVersion {declared} -> {actual}')
catalog=export/'Unity-iPhone/Images.xcassets/AppIcon.appiconset'
content=json.loads((catalog/'Contents.json').read_text())
marketing=[i for i in content['images'] if i.get('idiom')=='ios-marketing' and i.get('size')=='1024x1024']
if not marketing or not all((catalog/i.get('filename','missing')).is_file() for i in marketing):
 raise SystemExit('Missing 1024x1024 App Store icon in Unity export')
print('App Store icon and framework deployment metadata validated.')
