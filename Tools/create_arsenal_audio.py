"""Original sound design derived from the four user-selected recordings.
Never rewrites the approved clips. Reproducible: python Tools/create_arsenal_audio.py
"""
import json, hashlib, wave, uuid
from pathlib import Path
import numpy as np
from scipy.signal import butter,sosfilt,resample_poly
from scipy.io import wavfile
SR=48000
root=Path('Assets/Characters/Resources/SpellAudio')
sources={}
for name in ['Fire','Lightning','Teleport','Bubble']:
 rate,x=wavfile.read(Path('ArtSource/Audio')/(name+'_Decoded.wav'))
 x=x.astype(np.float64)/32768
 if x.ndim>1:x=x.mean(axis=1)
 if rate!=SR:x=resample_poly(x,SR,rate)
 sources[name]=x
protected={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in root.glob('*.wav')}
report=[]
def grain(src,start,length,pitch=1,band=None,reverse=False):
 x=sources[src][int(start*SR):int((start+length)*SR)].copy()
 if reverse:x=x[::-1]
 if band:
  lo,hi=band
  if lo:x=sosfilt(butter(3,lo,fs=SR,btype='highpass',output='sos'),x)
  if hi:x=sosfilt(butter(3,hi,fs=SR,btype='lowpass',output='sos'),x)
 if pitch!=1:x=np.interp(np.arange(0,len(x),pitch),np.arange(len(x)),x)
 if len(x):
  n=min(int(.015*SR),len(x)//3);x[:n]*=np.linspace(0,1,n);x[-n:]*=np.linspace(1,0,n)
 return x
def make(name,duration,layers,echo=.06):
 out=np.zeros(round(duration*SR));provenance=[]
 for src,start,length,pitch,gain,at,band,rev in layers:
  x=grain(src,start,length,pitch,band,rev);i=round(at*SR);n=min(len(x),len(out)-i)
  if n>0:out[i:i+n]+=x[:n]*gain
  provenance.append(dict(source=src+'_Original.mp3',region=[start,start+length],pitch_rate=pitch,offset=at))
 if echo:
  original=out.copy()
  for seconds,level in [(.047,echo),(.103,echo*.6),(.171,echo*.3)]:
   n=round(seconds*SR);out[n:]+=original[:-n]*level
 rms=np.sqrt(np.mean(out*out));peak=max(abs(out))
 if peak==0:raise ValueError(name)
 out*=min(.74/peak,.14/max(rms,1e-8))
 n=min(round(.025*SR),len(out)//4);out[:192]*=np.linspace(0,1,192);out[-n:]*=np.linspace(1,0,n)
 pcm=np.round(np.clip(out,-1,1)*32767).astype('<i2')
 wavfile.write(root/(name+'.wav'),SR,pcm)
 meta=root/(name+'.wav.meta')
 if not meta.exists():meta.write_text((root/'FireCast.wav.meta').read_text().replace('5719f0b172b23ea4d8bf1eff0617810f',uuid.uuid4().hex))
 report.append(dict(clip=name,seconds=duration,peak_dbfs=round(20*np.log10(max(abs(out))),2),rms_dbfs=round(20*np.log10(np.sqrt(np.mean(out*out))),2),layers=provenance))
def L(src,start,length,pitch=1,gain=1,at=0,band=None,rev=False):return(src,start,length,pitch,gain,at,band,rev)
make('RamCast',.72,[L('Fire',0,.52,.82,1,band=(90,5200)),L('Teleport',.04,.28,.72,.3)])
make('RamImpact',.32,[L('Bubble',.97,.24,.85,.85),L('Lightning',0,.12,.65,.25,band=(70,1600))])
make('RicochetCast',.65,[L('Fire',.03,.45,1.18,.65,band=(250,7000)),L('Lightning',.1,.22,1.45,.25)])
make('RicochetBounce',.23,[L('Teleport',.16,.13,1.6,.6),L('Lightning',.03,.09,1.7,.2,band=(1400,12000))])
make('AegisCast',2.12,[L('Bubble',0,.88,.46,.75,band=(350,6500)),L('Teleport',.02,.33,1.35,.45,band=(600,11000)),L('Teleport',.15,.22,1.7,.17,.45),L('Teleport',.15,.22,1.7,.12,1.15)],.15)
make('FragmentCast',.56,[L('Fire',.02,.35,1.05,.7),L('Lightning',.16,.18,1.6,.28,band=(1600,12000))])
make('FragmentSplit',.34,[L('Lightning',.02,.08,1.65,.6),L('Lightning',.18,.09,1.9,.4,.045),L('Teleport',.1,.1,1.7,.3,.09)],.12)
make('FragmentImpact',.26,[L('Lightning',.05,.12,1.35,.45),L('Bubble',1,.12,1.3,.4)])
make('CurveCast',1.15,[L('Teleport',0,.68,.95,.65,band=(550,11000)),L('Fire',.08,.4,1.05,.28,band=(1200,8500)),L('Teleport',.22,.12,1.9,.16,.4),L('Teleport',.22,.12,2,.12,.7)],.18)
make('CurveImpact',.4,[L('Bubble',1.01,.18,1.5,.45),L('Teleport',.11,.2,1.8,.55)],.2)
make('MonolithRise',.83,[L('Bubble',.95,.6,.65,.8,band=(40,2400)),L('Lightning',.05,.25,.5,.45,.05,band=(50,1500)),L('Fire',.1,.32,.65,.3,.15,band=(60,1100))])
make('BoomerangCast',.68,[L('Teleport',.03,.43,.9,.6),L('Fire',.1,.35,1.2,.55,band=(350,8000))])
make('BoomerangReturn',.54,[L('Teleport',.12,.4,1.1,.7,rev=True),L('Fire',.12,.25,1.15,.35,band=(500,8500))])
make('BoomerangHit',.29,[L('Bubble',1.02,.15,1.15,.5),L('Teleport',.12,.15,1.4,.4)])
make('BoomerangCatch',.32,[L('Teleport',.15,.19,1.8,.6),L('Bubble',.97,.1,1.6,.15)],.12)
for name,digest in protected.items():
 if name not in {r['clip']+'.wav' for r in report}:assert hashlib.sha256((root/name).read_bytes()).hexdigest()==digest,name
(root/'ArsenalAudioManifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
Path('ArtSource/Audio/ArsenalAudioManifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
# Short review reel, one representative sound per power, in catalog order.
clips=[]
for name in ['RamCast','RicochetCast','AegisCast','FragmentCast','CurveCast','MonolithRise','BoomerangCast']:
 _,x=wavfile.read(root/(name+'.wav'));clips.extend([x,np.zeros(int(.35*SR),dtype=np.int16)])
wavfile.write(Path('ArtSource/Audio/ArsenalPreview.wav'),SR,np.concatenate(clips))
print(json.dumps([dict(clip=r['clip'],seconds=r['seconds'],peak_dbfs=r['peak_dbfs']) for r in report],indent=2))
