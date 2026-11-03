# Lanzamiento ágil y flecha 0.5.1

Android 0.5.1, código 9. APK: Builds/Android/GuerraDeEmpujes-0.5.1-CasteoAgil.apk.

El bloqueo termina después del evento de lanzamiento + 0.05 s para fuego/rayo/teleport; se omite la cola del clip y se funde a locomoción. Burbuja conserva apertura/explosión, seguida de 0.08 s de recuperación. No se cambia el evento de salida ni los audios. La muerte cancela callbacks pendientes. Se conserva una vía de recuperación si falta un evento de clip.

Medición en Editor a unos 30 FPS (Logs/CastFeel051Checks.txt, COMPLETE): fuego 0.432 s, rayo 0.666 s, teleport 0.633 s; burbuja 1.400 s en nivel 1 y 1.800 s en nivel 5. Hay cuantización de fotogramas y actualización de física. Se verifica que el cuerpo realmente vuelva a moverse, que salga un único hechizo y que la muerte no deje una acción pendiente.

Flecha de fuego/rayo: 5.5 m de longitud desde el personaje, trazo de 0.30 m, cabeza de 1.7 m de ancho. Material AimArrowYellow en Resources/Interface, URP Unlit amarillo opaco sin textura/degradado. Teleport conserva su guía anterior y círculo de destino.

Regresión online: Logs/OnlineIntegration.txt, COMPLETE (lanzamiento remoto, movimiento, compras, recompensas y seis rondas en la misma sala). Revisión visual: Previews/Mobile_051_YellowArrow.png.

Todos los jugadores deben actualizar a esta versión para usar la misma recuperación de cast.

APK con firma verificada, instalado sobre 0.5.0 en HONOR. Flecha amarilla opaca comprobada por arrastre táctil durante combate: Previews/HONOR_051_YellowArrow.png. Se conservaron las posiciones personalizadas del HUD. Sin excepciones de referencias/red/ejecución en el logcat revisado. Exportación Unity sin errores; Gradle BUILD SUCCESSFUL (40 s). Se dejó el juego relanzado en inicio tras la prueba local.
