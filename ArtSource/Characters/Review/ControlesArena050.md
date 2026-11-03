# Controles y arena 0.5.0

Versión Android 0.5.0, código 8. APK: Builds/Android/GuerraDeEmpujes-0.5.0-ControlesArena.apk.

## Cambios

- Base 1.6 → 2.72 (+70%). Botas conservan +12% por nivel; anfitrión y réplicas comparten el cálculo. Ciclo de locomoción acompaña la velocidad; animaciones de cast conservan su sincronización.
- Fuego/rayo eligen dirección con flecha corta, sin círculo de destino ni indicador de distancia. Teleport conserva dirección/distancia y círculo de destino.
- Ajustes → Editar HUD. Se mueven poderes, zona de joystick, vida, reloj, oro, menú, inventario, cancelación y gráficos. También se puede mover la barra del editor. Guardar local, cancelar y restablecer; posiciones normalizadas dentro de la zona segura. El combate online sigue mientras se edita.
- Colisión barrida entre proyectiles físicos, incluyendo alcance desde atrás y cruces frontales. Ambos se anulan, sin daño ni teleport provocado por la colisión. Los proyectiles paralelos separados continúan.
- Terreno y collider seguro se contraen juntos; la lava sólida permanece. Reducción tras 15 segundos durante la ronda. Radio inicial 12 m, baja 0.6 m por ronda hasta un mínimo inicial de 7 m; radio final no menor de 4.5 m. El anfitrión transmite el radio a los clientes.

## Comprobación

- Logs/Mobile050Checks.txt: 24 PASS y COMPLETE. Velocidad/bonos, dirección versus destino, arrastre, guardado y recarga de diseño, cancelar/restablecer, geometría/collider, daño de lava y tres casos de colisión de proyectiles.
- Logs/OnlineIntegration.txt: COMPLETE. Regresión por WSS, movimiento, cast, compras, recompensas, continuidad de sala hasta ronda 6.
- Logs/OnlineArena050.txt: PASS. Cliente Unity conectado por WSS recibe radio 6 m y replica también el collider físico.
- Capturas: Previews/Mobile_050_ShrunkArena.png y Previews/HONOR_050_*.png. Arrastre por toque probado en HONOR; se canceló el diseño de prueba.
- Unity export Succeeded; errors=0. Compilación Gradle release y verificación de firma antes de distribuir.

Todos los participantes deben actualizar. El servidor temporal conserva las limitaciones de Tools/Multiplayer/README.md.

Validación final HONOR: actualización instalada con firma verificada; editor dentro de zona segura; arrastre táctil y guardado comprobados tras force-stop y relanzamiento. Se restablecieron y guardaron posiciones predeterminadas al terminar. Logcat revisado sin excepciones de referencias, red o ejecución. Gradle final BUILD SUCCESSFUL en 28 s.
