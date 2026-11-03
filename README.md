# PusherWars · Guerra de empujes

Prototipo de arena de magos para móvil, desarrollado con Unity **6000.6.7f1** (revisión `b22767f04f04`) y Universal Render Pipeline. Versión del juego: **0.8.4**.

Cuatro magos, bots y salas online de hasta ocho jugadores. El anfitrión simula el combate; conviene que todos usen la misma versión.

En el arsenal, el primer toque muestra la ficha y el segundo compra o mejora. Se empieza con Fuego y Campo de fuerza, con tres ranuras libres. Rayo, Teleport, Ariete, Ricochet, Égida, Metralla, Luz curva, Monolito y Búmeran se pueden elegir antes del combate y entre rondas. Los objetos no ocupan ranuras de hechizo: botas, capa, orbe, anillo de regeneración y talismán. Los niveles y el oro duran una partida.

## Descargar y abrir en la Mac

Instalar Git LFS antes de clonar para descargar también los archivos fuente de Blender:

```sh
git lfs install
mkdir -p "$HOME/Projects"
git clone https://github.com/jorgeoll1999/PusherWars.git "$HOME/Projects/PusherWars"
cd "$HOME/Projects/PusherWars"
git lfs pull
```

En Unity Hub, añadir la carpeta `PusherWars` como proyecto existente y abrirla con Unity **6000.6.7f1**. La primera importación reconstruye `Library` y descarga los paquetes de Unity.

Escena de prueba: `Assets/Characters/Scenes/Citadel_Playtest.unity`.

Los modelos y texturas que usa Unity están en `Assets`. Los `.blend` editables están en `ArtSource` y se almacenan con Git LFS. No se incluyen cachés de Unity, APK, exportaciones de Xcode, instaladores ni credenciales.

## Compilar para Apple

La Mac necesita una licencia de Unity activa. Para iPhone también necesita el módulo iOS Build Support de esa misma versión de Unity, Xcode y la firma de desarrollo de Apple configurada.

Desde la raíz del proyecto:

```sh
bash Tools/Apple/BuildApple.sh mac
bash Tools/Apple/BuildApple.sh ios
```

El primer comando genera `Builds/macOS/Guerra de empujes.app`. El segundo exporta el proyecto de Xcode a `Builds/iOS/Xcode`; después hay que compilarlo y firmarlo en Xcode. Los registros se escriben en `Logs`.

## Servidor multijugador

El cliente incluye el endpoint público del relay en `Assets/Characters/Resources/OnlineConfig.json`. El servicio se encuentra en `Tools/Multiplayer` y requiere Node.js:

```sh
cd Tools/Multiplayer
npm ci
npm test
npm start
```

Consultar `Tools/Multiplayer/README.md` para los detalles de salas, despliegue y las limitaciones del prototipo. Los datos de certificados y las sesiones locales de prueba quedan fuera de Git.
