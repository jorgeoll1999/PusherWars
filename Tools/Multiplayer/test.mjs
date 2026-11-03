import assert from 'node:assert/strict';
import WebSocket from 'ws';
const url=process.env.RELAY_URL||'ws://127.0.0.1:9006/socket';
async function peer(){const ws=new WebSocket(url);const messages=[];ws.on('message',x=>messages.push(JSON.parse(x)));await new Promise((ok,no)=>{ws.once('open',ok);ws.once('error',no);});return {ws,messages,send:m=>ws.send(JSON.stringify(m)),async wait(type){const until=Date.now()+10000;while(Date.now()<until){const i=messages.findIndex(m=>m.type===type);if(i>=0)return messages.splice(i,1)[0];await new Promise(r=>setTimeout(r,15));}throw Error('Timed out: '+type);}};}
const a=await peer(),b=await peer(),c=await peer();
try{
 a.send({type:'create',version:2,hero:2});const room=await a.wait('welcome');assert.match(room.code,/^[A-F0-9]{8}$/);assert.equal(room.id,0);
 b.send({type:'join',version:2,code:room.code,hero:3});assert.equal((await b.wait('welcome')).id,1);
 a.send({type:'start'});assert.match((await a.wait('error')).message,/dos magos/);
 a.send({type:'ready',ready:true});b.send({type:'ready',ready:true});await new Promise(r=>setTimeout(r,100));a.send({type:'start'});await a.wait('start');await b.wait('start');
 c.send({type:'join',version:2,code:room.code});assert.match((await c.wait('error')).message,/comenzó/);
 b.send({type:'input',from:0,position:{x:1,y:0,z:0}});assert.equal((await a.wait('input')).from,1);
 b.send({type:'snapshot',message:'forged'});await new Promise(r=>setTimeout(r,150));assert.ok(!a.messages.some(m=>m.type==='snapshot'));
 a.send({type:'snapshot',round:1});assert.equal((await b.wait('snapshot')).round,1);
 b.send({type:'cast',index:0});assert.equal((await a.wait('cast')).index,0);
 b.ws.close();assert.equal((await a.wait('left')).from,1);
 a.ws.close();console.log('PASS: public rooms, ready/start, locked match, sender identity, host authority, state/cast relay, disconnect.');
}finally{for(const p of[a,b,c])p.ws.terminate();}
const eight=await Promise.all(Array.from({length:9},peer));
try{
 eight[0].send({type:'create',version:2,hero:0});const room=await eight[0].wait('welcome');
 for(let i=1;i<8;i++){eight[i].send({type:'join',version:2,code:room.code,hero:i%4});assert.equal((await eight[i].wait('welcome')).id,i);}
 eight[8].send({type:'join',version:2,code:room.code});assert.match((await eight[8].wait('error')).message,/llena/);
 for(let i=0;i<8;i++)eight[i].send({type:'ready',ready:true});await new Promise(r=>setTimeout(r,150));eight[0].send({type:'start'});
 for(let i=0;i<8;i++)await eight[i].wait('start');eight[0].ws.close();for(let i=1;i<8;i++)assert.match((await eight[i].wait('closed')).message,/anfitrión/);
 console.log('PASS: eight-player room, ninth player rejected, all-ready start, host disconnect closes all clients.');
}finally{eight.forEach(p=>p.ws.terminate());}
