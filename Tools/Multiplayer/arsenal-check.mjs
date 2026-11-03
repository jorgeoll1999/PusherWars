// Local integration peer. The isolated Unity project runs ArsenalRoomCheck.Run.
import {spawn} from 'node:child_process';
import {once} from 'node:events';
import {readFile,writeFile,mkdir,rm} from 'node:fs/promises';
import {join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {setTimeout as delay} from 'node:timers/promises';
import {WebSocket} from 'ws';
import assert from 'node:assert/strict';
const dir=join(process.argv[2],'Logs');
await mkdir(dir,{recursive:true});
for(const file of ["arsenal-code.txt","arsenal-peer-pass.txt","arsenal-local.txt"])await rm(join(dir,file),{force:true});
const relay=spawn(process.execPath,[fileURLToPath(new URL('server.mjs',import.meta.url))],{cwd:new URL('.',import.meta.url),env:{...process.env,PORT:'0'},windowsHide:true});
let ws;
const timeout=setTimeout(()=>{console.error('Arsenal peer timed out');ws?.terminate();relay.kill();process.exit(1);},120000);
try{
 const [line]=await once(relay.stdout,'data');
 const addr='ws://127.0.0.1:'+String(line).match(/:(\d+)/)[1];
 await writeFile(join(dir,'arsenal-local.txt'),addr);
 let code;while(!code){try{code=(await readFile(join(dir,'arsenal-code.txt'),'utf8')).trim();}catch{await delay(120);}}
 ws=new WebSocket(addr+'/socket');await once(ws,'open');
 const send=m=>ws.send(JSON.stringify(m));let bought=false,ready=false,fired=false,barrier=false,ward=false,boomerang=false,next=0;
 const done=new Promise((resolve,reject)=>ws.on('message',data=>{try{
  const m=JSON.parse(data);
  if(m.type==='error')throw Error(m.message);
  if(m.type==='welcome')send({type:'ready',ready:true});
  if(m.type==='start'&&!bought){bought=true;for(const id of [10,6,9,1])send({type:'buy',index:id});for(const id of [3,4])send({type:'buy',item:true,index:id});}
  if(m.type!=='snapshot')return;const a=m.actors[1];
  if(!ready&&a.progress.items[4]===1){
   assert.equal(a.progress.gold,100);assert.equal(a.progress.spells.length,11);assert.equal(a.progress.spells[1],0);assert.equal(a.progress.spells.filter(Boolean).length,5);assert.equal(a.cooldown.length,11);ready=true;send({type:'roundReady'});
  }
  if(m.phase!==2)return;
  ward ||= a.ward>0; barrier ||= a.coverLife>0; boomerang ||= m.shots.some(x=>x.kind===10);
  if(next<=2){const id=[6,9,10][next];if(!fired||Date.now()-fired>650){send({type:'cast',index:id,position:{x:0,y:0,z:0}});fired=Date.now();}}
  if(next===0&&ward)next++;if(next===1&&barrier)next++;
  if(ward&&barrier&&boomerang)resolve();
 }catch(e){reject(e);}}));
 send({type:'join',version:2,hero:1,code});await done;
 await writeFile(join(dir,'arsenal-peer-pass.txt'),'PASS: cap, gold, cooldowns, ward, temporary cover, boomerang snapshots');console.log('PASS: host accepted remote purchases and casts; rejected sixth spell; ward, cover and boomerang visible to peer.');
}finally{clearTimeout(timeout);ws?.terminate();relay.kill();}
