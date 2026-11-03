import sys,json,subprocess,wave
from pathlib import Path
sys.path.insert(0,'Tools/AudioDeps');import imageio_ffmpeg
ff=imageio_ffmpeg.get_ffmpeg_exe();src=Path('ArtSource/Audio');dst=Path('Assets/Characters/Resources/SpellAudio');dst.mkdir(parents=True,exist_ok=True)
analysis=json.loads((src/'Analysis.json').read_text())
segments={'FireCast':('Fire',0,analysis['Fire']['duration'],None),'LightningCast':('Lightning',0,1.8,None),'TeleportArrival':('Teleport',0,analysis['Teleport']['duration'],None),'BubbleCast':('Bubble',0,.88,1.2),'BubblePop':('Bubble',.94,2.25,None)}
only=set(sys.argv[1:])
for tenths in range(12,17):segments['BubbleCast'+str(tenths)]=('Bubble',0,.88,tenths/10)
report=[]
for name,(source,start,end,target) in segments.items():
 length=end-start;tempo=length/target if target else 1;duration=target or length
 filters=[]
 if target:filters.append('atempo='+str(tempo))
 filters+=['afade=t=in:d=0.004','afade=t=out:st='+str(max(.01,duration-.04))+':d=0.04','alimiter=limit=0.92:level=false']
 if target:filters+=['apad=pad_dur=0.1','atrim=duration='+str(target)]
 if not only or name in only:subprocess.run([ff,'-y','-ss',str(start),'-t',str(length),'-i',str(src/(source+'_Original.mp3')),'-af',','.join(filters),'-ar','48000','-ac','1','-c:a','pcm_s16le',str(dst/(name+'.wav'))],stdout=subprocess.DEVNULL,stderr=subprocess.PIPE,check=True)
 with wave.open(str(dst/(name+'.wav'))) as w:actual=w.getnframes()/w.getframerate()
 report.append({'clip':name,'user_source':source+'_Original.mp3','source_region_seconds':[start,end],'result_seconds':actual,'tempo_preserves_pitch':tempo,'trigger':'OnSpellRelease animation event; complete source plays uninterrupted through impact' if name=='FireCast' else 'successful teleport arrival only; complete source; no launch sound' if name=='TeleportArrival' else 'OnSpellRelease animation event' if name.endswith('Cast') and name!='BubbleCast' else 'bubble activation' if name.startswith('BubbleCast') else 'actual collision/arrival/detonation'})
(src/'UserAudioTiming.json').write_text(json.dumps(report,indent=2));(dst/'AudioManifest.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
