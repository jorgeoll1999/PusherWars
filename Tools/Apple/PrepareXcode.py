"""Validate the iOS export and reconcile bundled framework minimum OS metadata."""
import json,plistlib,re,subprocess,sys
from pathlib import Path
project=Path(__file__).resolve().parents[2]
export=project/'Builds/iOS/Xcode'
for framework in (export/'Frameworks').glob('*.framework'):
 info=framework/'Info.plist'
 if not info.exists():continue
 data=plistlib.loads(info.read_bytes());binary=framework/data.get('CFBundleExecutable',framework.stem)
 if not binary.exists():continue
 output=subprocess.check_output(['xcrun','vtool','-show-build',str(binary)],text=True)
 minimum=re.findall(r'\bminos\s+([0-9.]+)',output)
 if not minimum:continue
 key=lambda v:tuple(map(int,v.split('.')))
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
