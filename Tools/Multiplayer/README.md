# Prueba online · Guerra de empujes

Prototipo para 2–8 teléfonos Android por Internet. Cada teléfono instala el mismo APK 0.7.0. El anfitrión simula la partida; el relay de este escritorio reenvía mensajes por WSS, sin abrir puertos entrantes.

## Jugar

1. Elegir mago en MAGOS y volver al inicio.
2. MULTIJUGADOR → CREAR SALA → COPIAR INVITACIÓN. Compartir la invitación con los demás.
3. Los demás pegan el enlace completo o el código de ocho caracteres y pulsan UNIRME.
4. Todos pulsan LISTO. El anfitrión pulsa INICIAR PARTIDA.
5. Cada uno compra sus mejoras con su oro y pulsa ENTRAR A LA RONDA. La cuenta atrás comienza cuando todos están listos.

Las rondas continúan en la misma sala y abren la tienda al terminar, sin bots online. El anfitrión y el escritorio del servicio deben permanecer conectados. Si el anfitrión sale o pone su app en segundo plano, se cierra la sala. No hay migración de anfitrión, reconexión a partidas en marcha, matchmaking ni cuentas. No hay salida automática al lobby después de la quinta ronda. El oro y las mejoras se conservan mientras siga la sesión.

## Reiniciar el servicio

Desde esta carpeta, `npm ci` (solo instalación inicial), luego `npm start`.
En otra terminal, desde la raíz del proyecto:

`Tools/Runtime/cloudflared.exe tunnel --no-autoupdate --url http://127.0.0.1:9006`

Copiar la URL HTTPS impresa por cloudflared. Cambia cuando se reinicia el túnel. En el juego, pegar esa URL en el campo de invitación antes de pulsar CREAR SALA. Los invitados deben pegar el enlace completo que copia el nuevo anfitrión. El APK sigue funcionando así aunque su servidor predeterminado haya caducado.

Quick Tunnel es un servicio temporal de desarrollo, sin garantía de disponibilidad. Un servicio estable con dominio y hosting es el siguiente paso para distribuir pruebas permanentes.

## Validación

`npm test` prueba el relay local. Para probar el servicio público, definir `RELAY_URL=wss://…/socket` antes de ejecutarlo. Cubre creación, unión, límite de entrada a partidas iniciadas, estado listo, identidad asignada por servidor, autoridad del anfitrión y desconexiones.

`OnlineIntegrationChecks` es una comprobación exclusiva del Editor; se ejecuta con `node Tools/Multiplayer/probe.mjs`. No se incluye en el APK. Resultados en `Logs/OnlineIntegration.txt`.

Solo se publican el endpoint de salas y una página de texto. El relay escucha en 127.0.0.1; no publica el proyecto ni los archivos del escritorio. No se guardan conversaciones ni datos personales. Es un prototipo con anfitrión de confianza; no incluye protección competitiva contra un anfitrión modificado.


## Controles y arena (0.5.0)

Ajustes → EDITAR HUD permite arrastrar poderes, zona del joystick, vida, reloj, oro, inventario, menú, cancelación y gráficos. La barra del editor también se puede mover. GUARDAR conserva posiciones en ese dispositivo; CANCELAR recupera el diseño previo; RESTABLECER recupera las posiciones iniciales (pulsar GUARDAR para conservarlas). La partida online continúa mientras se edita.

Fuego y rayo se apuntan por dirección, con flecha corta; solo teleport muestra alcance y destino seleccionable. Velocidad base 2.72, un 70% sobre 1.6. Las botas conservan su incremento porcentual.

Los proyectiles físicos chocan entre sí y ambos se anulan, incluso por alcance desde atrás. Un teleport anulado no traslada al lanzador. El rayo sigue siendo un hechizo instantáneo.

La arena empieza a contraerse tras 15 segundos de combate, hasta un radio mínimo de 4.5 metros. Cada ronda comienza 0.6 metros más pequeña, con un mínimo inicial de 7 metros. Se conserva el suelo sólido de lava. El anfitrión sincroniza el radio con los clientes; todos deben instalar 0.5.0.


## Lanzamiento ágil (0.5.1)

Fuego, rayo y teleport terminan el bloqueo tras el evento de lanzamiento y una recuperación de 0.05 s; la vuelta visual a locomoción usa un fundido breve. Burbuja espera la apertura de brazos/explosión y recupera en 0.08 s, omitiendo el resto del clip. A 30 FPS los tiempos medidos aparecen en Logs/CastFeel051Checks.txt. Los momentos de salida y el audio de cada hechizo se conservan. La muerte sigue cancelando lanzamientos pendientes.

La flecha de dirección de fuego/rayo mide 5.5 m, ancho 0.30 m y usa un material amarillo sólido sin degradado. Teleport conserva su marcador de destino.


## Versión 0.6.0
Coberturas destructibles sincronizadas (3–5 impactos), colisiones sólidas para personajes/fuego/rayo, teleport con paso a través y destino libre, bloqueo de escudo durante vuelo de TP. Nueva lava animada de basalto, flecha geométrica y marcos de controles. Actualizar todos los clientes.

## Versión 0.7.0
Salas de dos a ocho magos, protocolo 2. Pista circular de 30 m para hasta cuatro y rectangular de 44 × 27 m para cinco a ocho. Cuatro coberturas. El proyectil del teleport atraviesa ataques y magos intermedios, y resuelve el intercambio en el destino. Gran arena permite practicar offline contra siete bots.

## Versión 0.7.1
Cámara ortográfica de seguimiento local, ángulo de elevación de 45 grados y giro de 35 grados. Zoom fijo 8.8 para todos los tamaños de pista. Seguimiento suavizado de 0.12 segundos y recolocación al reaparecer o teletransportarse. Cada cliente sigue game.Player, correspondiente a su LocalActor. El protocolo de red sigue en versión 2, compatible con 0.7.0.


## Servidor propio — 0.7.2
Instalación Windows en C:/Services/GuerraRelay del servidor 172.16.10.102.
Las tareas de inicio GuerraEmpujes-relay y GuerraEmpujes-tls ejecutan los procesos con LOCAL SERVICE, supervisión y reinicio automático.
Relay privado 127.0.0.1:9006. Caddy 2.11.7 escucha TCP 9443 y su administración solo en 127.0.0.1:20197.
La regla de Windows GuerraEmpujes-TLS permite TCP 9443 únicamente al ejecutable Caddy.
Redirección requerida en el router TCP 186.4.147.35:443 -> 172.16.10.102:9443.
URL pública del juego wss://186.4.147.35.sslip.io. Ese nombre resuelve a la IP pública usando DNS sslip.io.
El certificado requiere completar la redirección pública para validar TLS-ALPN por el puerto externo 443.
Los datos de certificados están en tls-data, accesibles a LOCAL SERVICE, SYSTEM y Administradores.
Logs en logs/relay.log.stdout y logs/tls.log.stderr. No se guardan contraseñas de SSH en el proyecto.
La versión 0.7.2 usa el nuevo servidor y permite pegar una URL actualizada antes de CREAR SALA. Guarda el servidor que aceptó la conexión.

Validación pública completada el 2 de noviembre de 2026. Certificado Let's Encrypt validado por TLS 1.3. Pruebas WSS públicas aprobadas para creación, unión, listo/inicio, ocho participantes, rechazo del noveno, mensajes de juego, autoridad del anfitrión y desconexiones. La política FortiGate exclusiva del juego permite origen all hacia el VIP y el puerto interno TCP 9443.

