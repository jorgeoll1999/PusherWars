# Multijugador de prueba 0.4.0

APK: Builds/Android/GuerraDeEmpujes-0.4.0-Multijugador.apk (73,568,272 bytes, versionCode 6).
SHA-256: 8345F5D63CEC3FE06E240FF0664CDF6B5BF6E3F9F80306463E85F77D507734FB.
Export Unity: Succeeded, errors=0. Gradle release: BUILD SUCCESSFUL. Firma verificada. Instalado en HONOR CRT-LX3.

Implementado: salas por código/invitación WSS para 2–4 jugadores, mago elegido antes de entrar, todos listos, simulación del anfitrión sin bots online. Movimiento, poses interpoladas, vida, muertes, recargas, animaciones, proyectiles, efectos y audio. Cuatro poderes; oro y mejoras individuales; tienda y espera de todos; cinco rondas. Salir el anfitrión cierra la sala. El menú mantiene la simulación. Sockets recibidos y enviados sin depender del ritmo del hilo de dibujado.

## Validación

- Relay público: crear/unirse, listo, bloqueo tras inicio, identidad asignada por servidor, autoridad del anfitrión, comandos y estado, desconexiones. Sala de cuatro clientes de protocolo; quinto rechazado; cierre de todos al salir el anfitrión.
- Logs/OnlineIntegration.txt: COMPLETE, sin fallos; Unity anfitrión y cliente de protocolo.
- Logs/OnlineReplicaChecks.txt: siete PASS; Unity cliente y anfitrión de protocolo (identidad, mago local, física desactivada, interpolación, compra, vida y cooldown).
- Logs/HonorOnlineChecks.txt: teléfono real y Editor conectados por el servicio público. Teléfono probado como anfitrión y como invitado. Compras separadas, cuenta atrás, fuego/rayo con daño, intercambio TP, explosión y empuje de burbuja, joystick y lanzamiento táctiles. Ronda 2 en teléfono: fuego nivel 2 conservado, vida restaurada, 750 oro y tienda disponible.
- Consola Unity sin errores y logcat revisado sin excepciones de red/referencias durante la prueba final. HUD del teléfono mostraba 30 FPS en Equilibrado; no garantiza rendimiento en todos los dispositivos.
- ArtSource/Characters/Review/MobilePlaytestChecks.txt: ALL CHECKS PASSED, regresión local.
- Capturas: Previews/HONOR_Online_*.png.

## Límites

Se probó un teléfono físico y el Editor; cuatro plazas mediante clientes de protocolo. No hay matchmaking, cuentas, migración del anfitrión ni reconexión a partida iniciada. Prototipo para pruebas de confianza: un anfitrión modificado puede alterar su simulación.

El escritorio debe seguir encendido con Node y cloudflared ejecutándose. El anfitrión debe mantener su juego en primer plano. El túnel temporal cambia de URL al reiniciarse. Reinicio e invitaciones: Tools/Multiplayer/README.md. El servicio temporal se describe en https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/.
