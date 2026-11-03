import http from 'node:http';
import {randomBytes} from 'node:crypto';
import {WebSocketServer,WebSocket} from 'ws';
const rooms=new Map();
const server=http.createServer((req,res)=>{res.setHeader('Content-Type','text/plain; charset=utf-8');res.end('Guerra de empujes · servidor de pruebas online. Abre el juego y pega la invitación para entrar.');});
const wss=new WebSocketServer({server,maxPayload:65536,perMessageDeflate:false});
function send(s,m){if(s.readyState===WebSocket.OPEN&&s.bufferedAmount<262144)s.send(JSON.stringify(m));}
function roster(r){const m={type:'roster',code:r.code,players:r.players.map(p=>({id:p.id,hero:p.hero,ready:p.ready,connected:p.readyState===WebSocket.OPEN}))};r.players.forEach(p=>send(p,m));}
function error(s,message){send(s,{type:'error',message});}
function closeRoom(r,message){rooms.delete(r.code);r.players.forEach(p=>{send(p,{type:'closed',message});p.room=null;p.close(1000);});}
wss.on('connection',s=>{
 s.alive=true;s.started=Date.now();s.bucket=0;s.window=Date.now();s.on('pong',()=>s.alive=true);
 s.on('error',()=>{});
 s.on('message',raw=>{
  if(Date.now()-s.window>1000){s.window=Date.now();s.bucket=0;}if(++s.bucket>100){s.close(1008,'Rate limit');return;}
  let m;try{m=JSON.parse(raw);}catch{return error(s,'Mensaje inválido');}if(!m||typeof m!=='object')return;
  if(m.type==='ping')return send(s,{type:'pong',stamp:m.stamp});
  if(!s.room){
   if(m.version!==2)return error(s,'Actualiza el APK: versión de red incompatible.');
   if(!['create','join'].includes(m.type))return;
   const hero=Number.isInteger(m.hero)&&m.hero>=0&&m.hero<4?m.hero:0;
   let r;if(m.type==='create'){if(rooms.size>=50)return error(s,'Servidor lleno.');const code=randomBytes(4).toString('hex').toUpperCase();r={code,players:[],playing:false,touched:Date.now()};rooms.set(code,r);}
   else{r=rooms.get(String(m.code).toUpperCase());if(!r)return error(s,'La sala no existe o ya cerró.');if(r.playing)return error(s,'La partida ya comenzó.');if(r.players.length>=8)return error(s,'La sala está llena (8 magos).');}
   s.room=r;s.id=r.players.length;s.hero=hero;s.ready=false;r.players.push(s);send(s,{type:'welcome',id:s.id,code:r.code});roster(r);return;
  }
  const r=s.room;r.touched=Date.now();
  if(m.type==='ready'&&!r.playing){s.ready=!!m.ready;roster(r);return;}
  if(m.type==='start'&&s===r.players[0]&&!r.playing){if(r.players.length<2||!r.players.every(p=>p.ready))return error(s,'Se necesitan al menos dos magos, todos listos.');r.playing=true;r.players.forEach(p=>send(p,{type:'start'}));return;}
  if(!r.playing)return;
  if(['snapshot','event'].includes(m.type)){if(s!==r.players[0])return;delete m.from;r.players.forEach(p=>{if(p!==s)send(p,m);});return;}
  if(['input','cast','buy','roundReady'].includes(m.type)){m.from=s.id;send(r.players[0],m);}
 });
 s.on('close',()=>{const r=s.room;if(!r)return;s.room=null;if(s===r.players[0])return closeRoom(r,'El anfitrión salió. Vuelve a crear una sala.');if(!r.playing){r.players=r.players.filter(p=>p!==s);r.players.forEach((p,i)=>{p.id=i;send(p,{type:'welcome',id:i,code:r.code});});roster(r);}else{send(r.players[0],{type:'left',from:s.id});roster(r);}});
});
const heartbeat=setInterval(()=>{for(const s of wss.clients){if(!s.alive){s.terminate();continue;}s.alive=false;s.ping();if(!s.room&&Date.now()-s.started>30000)s.close(1008);}for(const r of rooms.values())if(Date.now()-r.touched>1800000)closeRoom(r,'Sala caducada.');},15000);
server.listen(Number(process.env.PORT||9006),'127.0.0.1',()=>console.log('Guerra relay ready on 127.0.0.1:'+server.address().port));
function shutdown(){clearInterval(heartbeat);for(const r of rooms.values())closeRoom(r,'Servidor cerrado.');wss.close();server.close();}
process.on('SIGTERM',shutdown);process.on('SIGINT',shutdown);
