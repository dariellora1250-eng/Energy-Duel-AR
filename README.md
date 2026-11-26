# Energy Duel AR

## Etapa final · audio y armas

La escena mantiene la integración de MediaPipe ya instalada y usa los recursos locales de `Assets/musica`. Los clips seleccionados para reproducción se encuentran en `Assets/Resources/EnergyDuelAudio`: `title.mp3` (menú), `urgent.mp3` (combate), `victory.mp3`, activación de espada y escudo, `impact.ogg`, `break.ogg` y `click.ogg`. Los ZIP originales permanecen intactos. Los tres efectos OGG se extrajeron temporalmente y proceden de las bibliotecas Kenney Impact Sounds, Sci-Fi Sounds e Interface Sounds. Cada ZIP incluye `License.txt`; conserva esos avisos junto a la copia original de las bibliotecas. No se añadieron recursos externos.

El audio se controla mediante PlayerPrefs (`ED.vol`, `ED.music`, `ED.sfx`). La música cambia entre menú, combate y victoria. Los parámetros de desgaste y las penalizaciones se exponen en `Reglas.asset`: espada rota, 3 s; escudo roto, 2 s; los valores de penalización admiten cero. Un cambio de gesto confirmado selecciona el arma opuesta. Los choques entre espadas desgastan ambas y el contacto sostenido desgasta la hoja ofensiva según el tiempo transcurrido. Un bloqueo desgasta más la espada que el escudo. Un choque entre escudos desgasta ambos sin reducir vidas.

El objetivo del renderizado permanece en 60 FPS y la frecuencia de inferencia se limita de forma independiente. La solicitud de resolución vuelve a abrir secuencialmente la fuente de cámara y verifica el tamaño negociado, con fallback cuando no coincide. Una webcam que solo informa 640 × 480 seguirá usando ese tamaño aunque se solicite 1280 × 720.

**Pendiente de verificación en Unity:** la compilación, el constructor de la escena, los niveles de volumen, los choques de escudos y las penalizaciones requieren ejecutar **Herramientas > Construir Energy Duel AR** y las pruebas presenciales en esta máquina. La configuración de volumen se guarda, pero esta versión aún requiere controles visibles en el menú. Los tooltips y los indicadores gráficos de durabilidad también requieren completar su integración de interfaz.

Duelo local de realidad aumentada para dos personas y una webcam. Cada jugador controla una espada o un escudo con una mano. La cámara permanece visible y los núcleos de color son las zonas vulnerables. Los impactos ocurren en la imagen. Basta con mover la mano en el propio espacio para proyectar la hoja sobre el núcleo rival.

## Probar en Unity

1. Abre la carpeta `realidad aumentada` desde Unity Hub con **Unity 2022.3.62f3**. Espera a que termine la importación.
2. Fuera de Play, selecciona **Herramientas > Construir Energy Duel AR**. El menú prepara los recursos locales, abre y guarda la escena, comprueba los componentes y la coloca primera en Build Settings. Si Unity pregunta por cambios en la escena anterior, puedes guardarlos. También puedes abrir directamente `Assets/EnergyDuel/Generado/Energy Duel AR.unity`.
3. Selecciona `Assets/EnergyDuel/Generado/Reglas.asset`. Escribe **Nombre** y **Matricula** en el Inspector para mostrar tus datos arriba a la derecha.
4. Conecta la webcam, deja libre su captura y pulsa **Play**. Usa la pestaña **Game**. Como primer formato de prueba, selecciona **16:9**.
5. Muestren una sola mano por persona. J1 empieza a la izquierda de la imagen y J2 a la derecha. Mantengan las manos quietas en sus lados durante medio segundo. Cuando ambas estén reconocidas, comenzará una cuenta de tres segundos.
6. Abre la mano para defender. Flexiona los dedos parcialmente para atacar. Haz pasar la hoja por el núcleo rival y retírala antes del siguiente golpe. Para bloquear, interpón el hexágono en el recorrido de la hoja.
7. Tras una derrota, cualquiera de los dos puede mantener su mano reconocida sobre **REINICIAR** durante 1,2 segundos. Vuelvan a sus lados para calibrar la nueva partida.

## Cámara y MediaPipe

La integración reutiliza `VisionTaskApiRunner`, `Bootstrap`, `WebCamSource`, `Screen.Initialize`, `TextureFramePool` y `HandLandmarkDetectionConfig` de **MediaPipe Unity Plugin 0.16.3 / Official Solutions**. La escena de referencia sigue en `Assets/Samples/MediaPipe Unity Plugin/0.16.3/Official Solutions/Scenes/Hand Landmark Detection/`.

Selecciona **Energy Duel AR** en la jerarquía y edita **Duel Capture** antes de pulsar Play.

| Campo | Valor inicial |
|---|---:|
| Inference / Delegate | CPU |
| Lectura | CPU Async |
| Modo | LIVE_STREAM |
| Manos | 2 |
| Detección | 0.01 |
| Presencia | 0.1 |
| Seguimiento | 0.9 |

CPU Async usa lectura asíncrona de la textura y ejecuta la inferencia en CPU. Si el dispositivo gráfico carece de lectura asíncrona, la captura usa lectura CPU. También puedes seleccionar CPU para diagnosticar la lectura, o IMAGE y VIDEO para comprobar los modos síncronos del plugin. La partida requiere dos manos. Los cambios del detector se aplican al volver a entrar en Play.

`Generado/Webcam.asset` es la configuración propia de arranque. Solicita la webcam predeterminada y un ancho de 640 píxeles para el N150. Puedes ajustar **Preferred Default Web Cam Width** en ese recurso. El generador toma el archivo `hand_landmarker.bytes` del paquete instalado y coloca una copia en `Assets/StreamingAssets` para que el mismo cargador funcione en el editor y en el ejecutable.

## Gestos, seguimiento y combate

- El clasificador combina los ángulos de dos articulaciones en índice, medio, anular y meñique con el alcance de las puntas respecto al tamaño de la palma. Utiliza los puntos 3D relativos de MediaPipe y dispone de una alternativa con coordenadas de imagen corregidas por su relación de aspecto. No hace falta cerrar el puño.
- En `Reglas.asset`, **Espada Hasta** y **Escudo Desde** definen dos umbrales de extensión entre 0 y 1. La banda intermedia conserva el gesto anterior. **Confirmar Gesto** exige que un nuevo gesto se mantenga, inicialmente durante 110 ms. Si la espada cuesta demasiado, sube ligeramente **Espada Hasta** manteniéndolo por debajo de **Escudo Desde**.
- El sistema asigna las dos detecciones conjuntamente por continuidad espacial, velocidad reciente y lateralidad. Puede suspender una muestra ambigua durante un solapamiento. Después de cruzar el centro, cada mano conserva su propietario mientras exista continuidad suficiente. Dos manos idénticas ocultas entre sí pueden requerir recalibración.
- La posición, la orientación y la escala siguen la palma con suavizado exponencial. **Suavizado** más alto responde antes. Una pérdida explícita corta el daño en la siguiente actualización del juego. **Vigencia** limita la antigüedad de una observación si dejan de llegar callbacks. **Retención Visual** mantiene la última pose 0,30 s desde la última muestra válida. Tras **Olvido** (1,4 s), la mano se recupera manteniéndola otra vez en su lado.
- Cada jugador empieza con 100 puntos. Una entrada válida resta 10. El juego proyecta la hoja, el contorno hexagonal y los núcleos al mismo plano ortográfico. Comprueba el espacio barrido entre muestras, da prioridad al escudo y exige movimiento, separación y un intervalo antes de volver a puntuar. Si falta una mano, ambos ataques quedan suspendidos.
- **Zona J1/J2** usa coordenadas de imagen entre 0 y 1 con origen abajo a la izquierda. **Radio Zona**, **Velocidad Mínima** y **Recorrido Mínimo** usan la altura visible de la imagen como unidad (por segundo para la velocidad). **Intervalo** controla el tiempo mínimo entre ataques. **Estela** activa la estela ligera.
- El cronómetro mide tiempo transcurrido. La partida termina al llegar una vida a cero. Dos impactos finales en la misma muestra dan empate.

## Organización

| Ruta en Assets/EnergyDuel | Responsabilidad |
|---|---|
| Scripts/DuelCapture.cs | Webcam, captura de muestras, vida de la textura e intercambio seguro entre hilos |
| Scripts/HandTracking.cs | Clasificación, confirmación y asignación de manos |
| Scripts/DuelWeapon.cs y DuelShape.cs | Pose, mallas 3D compartidas, escudo y estela |
| Scripts/DuelCombat.cs | Barrido de hoja, bloqueo, movimiento y rearme |
| Scripts/DuelMatch.cs y DuelRules.cs | Vida, temporizador, calibración, victoria y reinicio |
| Scripts/DuelHud.cs | Interfaz y zona de reinicio |
| Editor/ | Menú, receta de escena, prefabs, interfaz y recursos |
| Generado/ | Escena jugable, configuración editable, materiales y prefabs |

El generador utiliza rutas e identificadores estables. Completa los recursos ausentes y conserva los recursos existentes, incluidos los campos de autor y los ajustes del Inspector. Las mallas procedurales se generan al activar cada forma y se comparten por topología. El shader pertenece al **Built-In Render Pipeline**.

## Reproducir la dependencia local en otro equipo

Esta instalación ya resuelve el plugin mediante el tarball indicado en `Packages/manifest.json`. Conserva el archivo **com.github.homuler.mediapipe-0.16.3.tgz** con tu copia de respaldo. La distribución original es el paquete para Unity de homuler. [Distribución 0.16.3](https://github.com/homuler/MediaPipeUnityPlugin/releases/tag/v0.16.3)

Para trasladarlo a otra máquina sin conservar la ruta de Descargas de este equipo, cierra Unity, coloca ese mismo tarball en `Packages/Local/` dentro del proyecto y usa esta entrada en `Packages/manifest.json`.

```json
"com.github.homuler.mediapipe": "file:Local/com.github.homuler.mediapipe-0.16.3.tgz"
```

En `Packages/packages-lock.json`, cambia el valor `version` de esa misma dependencia a la misma cadena `file:Local/…`, con el nombre completo. Abre después el proyecto. La ruta se resuelve desde `Packages`. Conserva `Assets/Samples`, `Assets/EnergyDuel`, `Assets/StreamingAssets`, los archivos `.meta`, `Packages` y `ProjectSettings`. El tarball local y las carpetas regenerables de Unity están excluidos por `.gitignore`.

## Compilar para Windows

1. Ejecuta **Herramientas > Construir Energy Duel AR**.
2. Abre **File > Build Settings**. Comprueba que **Energy Duel AR** esté marcada y primera en **Scenes In Build**.
3. Selecciona **PC, Mac & Linux Standalone**, **Target Platform Windows** y **Architecture Intel 64-bit / x86_64**. Si hace falta, pulsa **Switch Platform**.
4. Pulsa **Build** y elige una carpeta como `Builds/Windows` dentro del proyecto. Ejecuta el `.exe` manteniendo a su lado la carpeta `_Data`, `UnityPlayer.dll` y los demás archivos emitidos. El modelo debe estar en `_Data/StreamingAssets/hand_landmarker.bytes`.

## Verificación realizada

El compilador de Unity 2022.3.62f3 compiló los scripts del juego y del editor. También compiló el código del jugador con las definiciones de Windows Standalone y sin referencias al editor. El modelo empaquetado coincide por SHA-256 con el instalado. El plugin nativo procesó una imagen sintética en CPU en modo IMAGE y devolvió cero manos.

Pasaron 16 pruebas sintéticas de clasificación, rotación y escala, asignación al cruzarse, confirmación de gestos, pérdida y recuperación, impactos, intervalo, permanencia dentro del núcleo y prioridad del escudo. Se validaron los documentos serializados, las referencias locales y los GUID externos. Una segunda ejecución de la receta produjo los mismos archivos de recursos.

En el editor de Unity 2022.3.62f3 se ejecutó el constructor dos veces y se validaron las referencias al reabrir la escena. Ambas ejecuciones conservaron ocho objetos raíz. En Play, la webcam entregó vídeo de 640 × 480 al fondo y Hand Landmarker devolvió un resultado asíncrono en CPU. Se comprobó el render comparando los píxeles de la cámara virtual con el fondo activado y desactivado. Quedan pendientes la alineación de las armas con dos personas, la fluidez durante una partida presencial y la ejecución de la compilación de Windows.

## Prueba presencial

Comprueba que la imagen respete el encuadre, que el mango siga la palma en posición frontal y semilateral y que los colores sigan a sus dueños al cruzar las manos. Prueba un bloqueo encima del núcleo, un golpe mantenido, una pérdida breve y una pérdida de más de 1,4 s. Termina una partida y mantén una mano sobre el botón de reinicio. Repite en el ejecutable x64 para valorar latencia y fluidez en el N150.

## Licencias y atribución

**MediaPipe Unity Plugin y Official Solutions** son obra de homuler bajo licencia MIT. **MediaPipe**, de The MediaPipe Authors, se distribuye bajo Apache License 2.0. La copia del paquete instalado incluye los avisos de sus componentes. El generador conserva la licencia y esos avisos junto al modelo en StreamingAssets para su inclusión en Windows. [Licencia del plugin](Assets/StreamingAssets/MediaPipe-LICENSE.txt) · [Avisos de terceros](<Assets/StreamingAssets/MediaPipe-Third Party Notices.txt>).

