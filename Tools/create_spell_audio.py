import math,random,wave,struct,os,json
root=r'C:\Users\Jorge Ortega\Desktop\Guerra de empujes\Assets\Characters\Resources\SpellAudio'
os.makedirs(root,exist_ok=True);sr=44100
specs={'FireCast':(.45,1),'FireImpact':(.7,2),'LightningCast':(.65,3),'TeleportCast':(.55,4),'TeleportArrival':(.55,5),'BubbleCast':(.65,6),'BubblePop':(.75,7)}
report=[]
for name,(duration,kind) in specs.items():
 rng=random.Random(kind*147);data=[];low=0;phase=0
 for i in range(int(sr*duration)):
  t=i/sr;u=t/duration;noise=rng.uniform(-1,1);low=low*.92+noise*.08
  if kind==1:
   env=math.sin(math.pi*u)**.6;x=(noise*.3+low*2)*env*.6+math.sin(math.tau*(230*t-120*t*t))*math.exp(-8*t)*.2
  elif kind==2:
   env=math.exp(-7*t);x=(noise*.45+low*3)*env+math.sin(math.tau*(110*t-45*t*t))*math.exp(-9*t)*.6
  elif kind==3:
   crack=math.exp(-70*t);x=noise*(crack*.9+math.exp(-10*t)*.18)+(math.sin(math.tau*60*t)+low*5)*math.exp(-7*t)*.2
  elif kind in (4,5):
   freq=(300+u*1100) if kind==4 else (1300-u*900);phase+=math.tau*freq/sr
   env=math.sin(math.pi*u)**.7;x=(math.sin(phase)*.25+math.sin(phase*1.502)*.18+math.sin(phase*2.003)*.08+low*.8)*env
  elif kind==6:
   phase+=math.tau*(180+u*180)/sr;x=(math.sin(phase)*.3+math.sin(phase*1.5)*.15+low*.7)*math.sin(math.pi*u)**.8
  else:
   phase+=math.tau*(80+260*math.exp(-30*t))/sr;x=math.sin(phase)*math.exp(-12*t)*.65+noise*math.exp(-24*t)*.55
   for delay,freq in [(0.04,1200),(.09,1800),(.16,2500),(.23,1400)]:
    if t>delay:x+=math.sin(math.tau*freq*(t-delay))*math.exp(-22*(t-delay))*.14
  fade=min(1,t/.006,(duration-t)/.035);data.append(x*max(0,fade))
 peak=max(abs(v) for v in data);gain=.82/max(peak,.001)
 # A short stereo-like echo is folded to mono so Unity can spatialize the clip.
 delay=int(.055*sr);samples=[v+(.14*data[i-delay] if i>=delay else 0) for i,v in enumerate(data)]
 scale=.82/max(max(abs(v) for v in samples),.001)
 with wave.open(root+'/'+name+'.wav','wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes(b''.join(struct.pack('<h',int(max(-1,min(1,v*scale))*32767)) for v in samples))
 report.append({'clip':name,'seconds':duration,'sample_rate':sr,'peak':.82,'source':'original procedural synthesis'})
with open(root+'/AudioManifest.json','w') as f:json.dump(report,f,indent=2)
print('Created 7 original, peak-normalized spell sounds')
