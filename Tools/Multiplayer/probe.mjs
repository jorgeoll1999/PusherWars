import fs from 'node:fs';
import WebSocket from 'ws';
let info;for(let n=0;n<120;n++){try{info=JSON.parse(fs.readFileSync(new URL('./probe-room.json',import.meta.url)));break;}catch{}await new Promise(r=>setTimeout(r,500));}if(!info)throw Error('No probe room');
const s=new WebSocket(info.message+'/socket');let playing=false,fighting=false,casts=0,snapshots=0,events=0,phase=-1,lastReadyRound=1;const started=Date.now();
s.on('open',()=>s.send(JSON.stringify({type:'join',version:2,code:info.code,hero:1})));
const send=m=>{if(s.readyState===1)s.send(JSON.stringify(m));};
s.on('message',raw=>{const m=JSON.parse(raw);if(m.type==='welcome')send({type:'ready',ready:true});if(m.type==='start'){playing=true;send({type:'buy',index:0,item:false});send({type:'roundReady'});}if(m.type==='snapshot'){snapshots++;if(m.phase===0&&m.round>lastReadyRound){lastReadyRound=m.round;send({type:"roundReady"});}if(m.actors?.length!==2)throw Error('Actor count');fighting=m.phase===2;if(phase!==m.phase){phase=m.phase;console.log('PHASE',phase,'round',m.round,'hp',m.actors.map(a=>a.hp));}}if(m.type==='event')events++;if(m.type==='closed'){console.log('PROBE COMPLETE',JSON.stringify({snapshots,events,casts}));clearInterval(tick);s.close();}});
const tick=setInterval(()=>{if(Date.now()-started>90000){s.terminate();clearInterval(tick);return;}if(fighting){send({type:'input',position:{x:0,y:0,z:1}});if(++casts%40===0)send({type:'cast',index:0,position:{x:0,y:0,z:-5}});}},50);
s.on('error',e=>{console.error(e.message);clearInterval(tick);});s.on('close',()=>clearInterval(tick));
