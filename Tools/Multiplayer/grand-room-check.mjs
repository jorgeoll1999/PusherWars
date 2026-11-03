// Connect seven test guests to a Unity host and verify the eighth seat through combat.
import fs from 'node:fs';
import assert from 'node:assert/strict';
import WebSocket from 'ws';
const [code]=process.argv.slice(2);
if(!/^[0-9A-F]{8}$/.test(code??''))throw Error('Pass a test room code');
const config=JSON.parse(fs.readFileSync(new URL('../../Assets/Characters/Resources/OnlineConfig.json',import.meta.url)));
const clients=[];
let completed=false;
async function guest(){
  const ws=new WebSocket(config.url+'/socket');
  const state={id:-1,combat:0,sawCooldown:false};clients.push(ws);
  return new Promise((resolve,reject)=>{
    ws.on('error',reject);
    ws.on('open',()=>ws.send(JSON.stringify({type:'join',code,version:2,hero:(clients.length-1)%4})));
    const send=data=>ws.send(JSON.stringify(data));
    ws.on('message',raw=>{
      const packet=JSON.parse(raw);
      if(packet.type==='error')throw Error(packet.message);
      if(packet.type==='welcome'){state.id=packet.id;send({type:'ready',ready:true});resolve();}
      if(packet.type==='start'){send({type:'buy',item:true,index:0});send({type:'roundReady'});}
      if(packet.type==='snapshot'){
        assert.equal(packet.actors.length,8);assert.equal(packet.coverHits.length,4);
        if(packet.phase!==2)return;
        assert.equal(packet.actors[state.id].progress.items[0],1);
        if(++state.combat===2)send({type:'cast',index:0,position:{x:0,y:0,z:0}});
        if(packet.actors[state.id].cooldown[0]>0)state.sawCooldown=true;
        if(state.id===7&&state.combat>=12&&state.sawCooldown)completed=true;
      }
    });
  });
}
try{
  for(let i=1;i<=7;i++)await guest();
  console.log('Seven guests joined, ready for host.');
  const timeout=Date.now()+120000;
  while(!completed&&Date.now()<timeout)await new Promise(r=>setTimeout(r,100));
  assert.ok(completed,'seat eight fought and cast with synchronized purchases and four covers');
  console.log('PASS eight online actors, seven remote purchases, ready/start and remote combat cast in seat eight.');
}finally{for(const ws of clients)ws.close();}
