// Exercises Unity's client path independently of the authoritative Unity host test.
import fs from 'node:fs';import WebSocket from 'ws';import assert from 'node:assert/strict';
const config=JSON.parse(fs.readFileSync(new URL('../../Assets/Characters/Resources/OnlineConfig.json',import.meta.url)));
const s=new WebSocket(config.url+'/socket');const send=m=>{if(s.readyState===1)s.send(JSON.stringify(m));};let active=false,ready=false,code,seq=0;const report={inputs:0,casts:0,buys:0,ready:false};
const model=()=>({gold:600,spells:[1,1,1,1],items:[0,0,0],shopOpen:true});const actors=[0,1].map(i=>({position:{x:0,y:.04,z:i?-5:5},yaw:i?0:180,hp:100,speed:0,bubble:false,teleporting:i===1,progress:model(),cooldown:[0,0,0,0],wins:0,kills:0,earned:0}));
s.on('open',()=>send({type:'create',version:2,hero:0}));
s.on('message',raw=>{const m=JSON.parse(raw);if(m.type==='welcome'){code=m.code;send({type:'ready',ready:true});fs.writeFileSync(new URL('./replica-room.json',import.meta.url),JSON.stringify({code,url:config.url}));}
 if(m.type==='roster'&&m.players.length===2&&m.players.every(p=>p.ready))send({type:'start'});
 if(m.type==='start')active=true;
 if(m.type==='buy'){assert.equal(m.from,1);actors[1].progress.gold-=100;actors[1].progress.spells[m.index]++;report.buys++;}
 if(m.type==='roundReady'){ready=true;actors.forEach(a=>a.progress.shopOpen=false);report.ready=true;}
 if(m.type==='input'&&m.position?.x>.5)report.inputs++;
 if(m.type==='cast'){assert.equal(m.from,1);report.casts++;actors[1].cooldown[m.index]=3;actors[0].hp=70;send({type:'event',generation:1,name:'cast',index:1,kind:m.index,position:m.position,value:1.2});}
 fs.writeFileSync(new URL('../../Logs/OnlineReplica.json',import.meta.url),JSON.stringify(report));
});
const tick=setInterval(()=>{if(active){seq++;actors[0].position.x=Math.sin(seq/20);actors[0].speed=1.4;for(const a of actors)for(let i=0;i<4;i++)a.cooldown[i]=Math.max(0,a.cooldown[i]-.067);send({type:'snapshot',coverHits:[0,1,2,3],arenaRadius:6,generation:1,actors,shots:[],round:1,phase:ready?2:0,countdown:0,time:85,message:'',results:[]});}},67);
const end=setTimeout(()=>{s.close();clearInterval(tick);},300000);s.on('close',()=>{clearTimeout(end);clearInterval(tick);console.log(report);});s.on('error',e=>{console.error(e.message);clearInterval(tick);clearTimeout(end);});
