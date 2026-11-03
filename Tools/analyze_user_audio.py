import sys,os,subprocess,wave,json,shutil
from pathlib import Path
import numpy as np
sys.path.insert(0,'Tools/AudioDeps');import imageio_ffmpeg
ff=imageio_ffmpeg.get_ffmpeg_exe();base=Path('ArtSource/Audio');base.mkdir(parents=True,exist_ok=True)
names={'Fire':'audio-fire-ball-magic (1).mp3','Lightning':'audio-swift-thunderous-lightning-flash (1).mp3','Teleport':'audio-naruto-sound-style-teleportation.mp3','Bubble':'audio-spellcasting-with-a-magical-explosion.mp3'}
report={}
for name,file in names.items():
 src=Path(r'C:\Users\Jorge Ortega\Downloads')/file;shutil.copy2(src,base/(name+'_Original.mp3'))
 dest=base/(name+'_Decoded.wav');subprocess.run([ff,'-y','-i',str(src),'-ac','1','-ar','48000',str(dest)],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,check=True)
 with wave.open(str(dest)) as w:a=np.frombuffer(w.readframes(w.getnframes()),dtype=np.int16).astype(float)/32768
 n=len(a)//480;rms=np.sqrt(np.mean(a[:n*480].reshape(n,480)**2,axis=1));active=np.where(rms>max(rms)*.025)[0];peaks=sorted(range(n),key=lambda k:rms[k],reverse=True);chosen=[]
 for i in peaks:
  if all(abs(i-j)>.15*100 for j in chosen):chosen.append(i)
  if len(chosen)==6:break
 report[name]={'duration':len(a)/48000,'onset':float(active[0]/100),'end':float(active[-1]/100),'peak_windows':[(i/100,round(float(rms[i]),3)) for i in chosen]}
print(json.dumps(report,indent=2));(base/'Analysis.json').write_text(json.dumps(report,indent=2))
