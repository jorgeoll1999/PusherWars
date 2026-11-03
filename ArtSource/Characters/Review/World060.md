# Coberturas y estética 0.6.0

Android 0.6.0, código 10. Ocho coberturas de roca y obelisco; desaparecen los grupos decorativos de ruinas, plantas, pedestales y rocas de fondo. Cada cobertura tiene 3, 4 o 5 impactos, indicador de vida al recibir daño y collider sólido. Fuego/rayo se frenan incluso en el golpe que destruye el objeto. La burbuja no empuja a través de cobertura. Teleport atraviesa sin dañarla y rechaza terminar dentro o encima de un obstáculo. La salud de las coberturas se reinicia por ronda y se sincroniza mediante snapshots.

Escudo bloqueado durante el vuelo de teleport, validado por anfitrión y por bandera replicada; TP también rechaza un escudo activo. Flecha de malla simétrica rellena con contorno oscuro y oro apagado. Marcos de habilidades y joystick con acabado de metal oscuro y bronce.

Lava: textura original generada con imagegen, guardada en Assets/Characters/Resources/Battlefield/LavaBasalt060.png; shader con desplazamiento, distorsión suave y variación de luminosidad en canales de magma. Conserva el suelo físico sólido y el daño existente.

Capas: se han estabilizado pesos de 2249/1531/2388/2158 vértices de ropa en Arcana/Fuego/Hielo/Planta, respectivamente, sin modificar FBX fuente ni clips. Se generan mallas derivadas en Assets/Characters/ClothRepair. El conector de Blender no estaba disponible; la reparación se realizó con las mallas y pesos desde Unity Editor. No se ha sustituido el diseño de los bordes originales por un modelo nuevo.

Pruebas: Logs/World060Checks.txt COMPLETE (15 PASS): coberturas, colisiones de personaje, fuego y rayo, tercer/quinto impacto, teleport atravesando sin dañar, bloqueo de escudo, carga de textura y nueva flecha. Logs/World060Online.txt: PASS vida replicada y rechazo de escudo mediante estado de teleport recibido por WSS.

Las primeras pruebas detectaron un inicializador de MaterialPropertyBlock fuera de Awake y una ejecución duplicada de fixtures; ambos corregidos antes de la validación final. Se retiraron los fixtures de la escena de edición antes de exportar.

Todos los participantes deben actualizar a 0.6.0. Servidor temporal y límites de pruebas: Tools/Multiplayer/README.md.

Validación nativa final: exportación Unity sin errores, Gradle BUILD SUCCESSFUL y firma APK verificada. Instalación -r confirmada en HONOR CRT-LX3 (versionName 0.6.0, versionCode 10). Captura Previews/HONOR_060_Joystick.png: combate, joystick y lava visibles, indicador alrededor de 30 FPS; logcat Unity/AndroidRuntime sin excepciones en la muestra final. Esta cifra es una observación de la prueba, no un perfil sostenido. La validación online de esta revisión fue mediante cliente Unity y réplica WSS; falta una sesión con dos teléfonos físicos.
