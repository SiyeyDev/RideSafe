# Qué falta — tareas de la reunión, garaje v6

> Fecha: 2026-10-12. Rama **`feat/garaje-v6`** (sale de `feat/garaje-v5` tras el merge del tutorial).
> Escena: `Assets/_Project/Scene/RideSafe_Garaje_v5.unity` — conserva el nombre, pero ya es el v6.
> Estado de pruebas al cerrar: **257 EditMode y 4 PlayMode en verde**, PlayMode dos corridas seguidas.

Este documento es la respuesta a «si falta algo en 3D o UI/UX, avisa en un documento nuevo».
Está ordenado por lo que **bloquea**: primero lo que no se puede hacer sin que llegue algo de
fuera, luego lo que se hizo con una decisión mía que conviene que confirmes, y al final lo
menor.

---

## 1. Lo que NO se pudo hacer, y por qué

### 1.1 🎙️ No hay ni un solo archivo de audio en el proyecto

`find` sobre `Assets/_Project` devuelve **0 archivos** `.wav`, `.mp3` u `.ogg`.

Esto deja incompletas dos tareas:

- **§3 · «Audio Feedback de Selección»** — la locución al tocar un objeto
  («Casco deteriorado, ¿deseas añadirlo?»).
- **§6 · «Carga de Feedback Específico»** — el audio de recomendación de cada rango.

**Lo que sí está hecho:** el enchufe. `SafetyItemSO` tiene un campo `PromptClip` y el inspector
lo dispara al sacar el objeto al pedestal; si está vacío, calla sin quejarse. Cuando lleguen las
locuciones basta con **arrastrarlas al asset del elemento**: ni una línea de código.

Hacen falta **11 clips** para empezar:

| Para | Cuántos | Qué dice |
|---|---|---|
| Los 7 EPP | 7 | La pregunta al inspeccionar cada uno |
| Los 4 rangos | 4 | La recomendación de `Module1/Rank_*` (el texto ya está escrito, en los dos idiomas) |

### 1.2 🎬 Los dos videos de revisión no existen

Solo hay `Assets/_Project/Video/VideoTest.mp4`, que es un marcador de posición.

**Lo que sí está hecho, y probado con el video de prueba:** el modo teatro (el garaje se apaga
alrededor de la pantalla), el bloqueo de «Continuar» hasta que el video termina al 100 %, y
pausa/repetir. No hay botón de silencio, como se pidió.

Hacen falta **2 videos**: el de la revisión de protección personal y el de la del vehículo.

### 1.3 🚲 El highlight por componente es imposible con los vehículos actuales

La reunión pide, en §4, «delineado naranja neón con resplandor únicamente en la **sub-malla del
componente activo**».

**El `ebike` y el `scooter` son una malla única sin hijos.** Medido: `ebike` tiene 25.570
triángulos en un solo `MeshFilter` llamado `e_bike`, y `scooter` 25.856 en `Scooter_2:Mesh`. No
existe «el freno», ni «la rueda delantera», ni «el manubrio» como objeto al que ponerle un
contorno.

**Lo que sí está hecho:** el resplandor va en el **marcador** del punto de inspección, que sí es
un objeto propio: naranja neón y latiendo cuando es el punto en revisión, verde al resolverlo,
y tenue cuando es de otra zona.

**Lo que hace falta del equipo de arte:** los dos vehículos **despiezados**, con un objeto hijo
por componente evaluable — frenos, luces, llantas, manubrio y batería. En cuanto existan, el
contorno se mueve del marcador a la sub-malla sin cambiar nada más de la lógica.

### 1.4 🎨 Los dos vehículos no tienen materiales

Los dos usan un material llamado **`New Material`**: URP/Lit, sin textura base, color gris
`(0.509, 0.509, 0.509)`. En la escena se ven como una maqueta blanca.

Es el objeto central del módulo 1.2 y ahora mismo es lo peor acabado de todo el garaje.

### 1.5 📄 Los dos PDF del GDD siguen fuera del repo

Siguen sin estar en el proyecto, así que no se puede consultar nada que dependa de ellos —por
ejemplo, si `luz grontal` y `luz roja 1` deben ser EPP recogibles o quedarse como parte del
punto `point_lights`.

**Están en tu carpeta de Descargas** (`RideSafe VR GDD VF1.pdf` y
`RideSafe_VR_Module_Narratives_v2.0.pdf`). Si los metes en `Docs/`, dejan de ser un punto ciego.

### 1.6 📊 La hoja de I2 del Drive sigue por detrás

Sin cambios respecto a lo que ya decía `memory.md`: la hoja original no está en este Drive y por
el relay no hay API de celdas. **Y ahora la distancia es mayor:** la categoría `Module1` del
asset pasó de 61 a **77 claves** (16 nuevas, en español e inglés, sin celdas vacías).

Si alguien reimporta la hoja vieja encima, se cargaría 77 términos traducidos. Las trampas del
prefijo duplicado y del inglés retraducido siguen vigentes y están en `memory.md` §9.

---

## 2. Decisiones que tomé y conviene que confirmes

### 2.1 Los tres botones de decisión ahora son de colores — y eso revierte una decisión tuya

La reunión pide 🟩 «¿Listo para circular?» / 🟨 «Necesita servicio» / 🟥 «No circular».
**Está hecho.**

Pero `memory.md` registra la decisión contraria, del 2026-10-07: *«los tres botones del go/no-go
quedaron secundarios: en una evaluación, un pill coral en RoadReady sugiere la respuesta antes de
que el aprendiz lea el texto»*.

La reunión es posterior, así que manda la reunión. **Pero el motivo de entonces sigue siendo
válido**, y en un visor el verde tira mucho. Vale la pena mirarlo en el Quest y decidir.

### 2.2 «Mi Elección» es un panel con dos columnas, no una sola

Se pidió «eliminar la tabla duplicada de *Referencia para este recorrido*» y «unificar en un
único panel centrado». La tabla duplicada **está eliminada** —y era literalmente la misma lista
con los estados cambiados, o sea cero información nueva—.

Lo que conservé son las **dos columnas**, repartiendo una sola lista entre ellas. Motivo: el
reporte final junta los 7 elementos de protección con los 5 puntos del vehículo, y doce filas en
una columna se salen del panel por abajo. Si prefieres una sola columna con desplazamiento, se
cambia en un rato.

### 2.3 El cuarto estado: «en riesgo» vs «incorrecto»

La reunión pidió cuatro iconos, y el enum solo distinguía tres casos útiles. Añadí un quinto
estado, `AtRisk`, y una marca «deteriorado» en los datos del elemento, porque **el casco dañado y
el móvil en la mano son los dos "inapropiado" y restan igual, pero enseñan cosas distintas**: uno
es revisar el estado del equipo y el otro no llevar carga suelta.

Hoy solo `helmet_damaged` está marcado como deteriorado. Si hay más elementos que deban llevar el
⚠️ en vez del 🔴, es una casilla en su asset.

Lo «condicional» (el chaleco en un día claro) **no entra en la leyenda**: no es ni acierto ni
fallo, y meterlo obligaba a explicar un quinto estado que no se evalúa.

### 2.4 Cómo se calcula la nota

- Se juzgan **el núcleo** (tomarlo acierta, omitirlo falla) y **los inapropiados** (no tomarlos
  acierta, tomarlos falla).
- **Lo opcional y lo condicional no entran.** Penalizarlos sería la penalización falsa que la
  narrativa prohíbe, y premiarlos convertiría «llévatelo todo» en la estrategia ganadora.
- Las dos secciones se suman por **aciertos y juzgables**, no promediando porcentajes: la de
  protección tiene 7 elementos y la del vehículo 5, así que promediar le daría a cada acierto del
  vehículo más peso sin que nadie lo haya decidido.
- La nota se **trunca**, nunca se redondea: un 74,6 % no puede convertirse en aprobado por un
  artefacto de coma flotante.
- **La disposición final (circular / servicio / no circular) NO entra en la nota.** Se evalúa
  aparte y se le dice al aprendiz si acertó. Son dos aprendizajes distintos y mezclarlos esconde
  uno de los dos. **Si prefieres que cuente, dímelo: es un cambio pequeño.**

### 2.5 Dónde quedó cada punto del vehículo

Los 5 puntos cuelgan ahora de la malla y viajan con el giro, pero **sus sitios son aproximados**:
salen de fracciones de la caja del vehículo, no de huesos ni de sub-objetos, porque no los hay
(ver 1.3). Manubrio, freno y faro están «donde deberían», pero conviene repasarlos a ojo en la
escena. Moverlos a mano es seguro: el comando que los monta no los vuelve a tocar si ya existen.

Lo mismo con los giros: `cockpit` 90°, `wheels` 180°, `visibility` −90°. **Cuál extremo de la
malla es el morro no se puede saber desde el código.** Si en la zona de manubrio el vehículo sale
de culo, se intercambian los 90 por −90 en `Module01VehicleStaging.Yaws`.

### 2.6 Moví cosas del paquete de arte

Dos cambios sobre lo que entregó el equipo de arte, los dos con motivo:

- **Un maniquí estaba plantado delante del mostrador**, tapando las gafas y el calzado. Lo aparto
  al extremo libre. El comando lo vuelve a apartar solo si el v7 lo coloca otra vez ahí.
- **Los 7 EPP estaban desparramados** a lo largo de 5 metros de mostrador (la zona `head` ocupaba
  4,99 m), así que desde un punto de vista se veían diminutos. Los agrupé por zona. Además tres
  estaban **hundidos en la tapa** —`casco 1` tenía la base 16 cm por debajo—; ahora los siete
  apoyan exactamente.

---

## 3. Rendimiento: el problema más gordo que queda

### 670.903 triángulos. El presupuesto de Quest es 150.000–350.000.

Casi el doble, y ha subido desde los ~370.000 que tenía v5. Medido sobre la escena real, contando
solo renderers activos.

| Triángulos | Qué | % |
|---:|---|---:|
| **149.601** | Los **3 maniquíes** `Ch20_nonPBR` (49.867 cada uno) | 22 % |
| **112.726** | **22 troncos de árbol** `Tree_Trunk` (16 skinned + 6 estáticos) | 17 % |
| ~95.000 | Las casas `House01`…`House06` del exterior | 14 % |
| 51.426 | `scooter` + `ebike` | 8 % |
| 26.881 | Los 7 EPP | 4 % |
| 11.228 | `botas` — usa la malla `Ch20`, la del personaje | 2 % |

**Las dos primeras filas son el 39 % del presupuesto y ninguna de las dos es el contenido del
módulo.** Tres cosas que darían margen rápido, por orden de ganancia:

1. **Los maniquíes.** 50.000 triángulos cada uno es coste de personaje protagonista, y además
   están **en pose T**, que es como se ve un modelo sin animar. O se cambian por versiones de
   bajo polígono con una pose, o se quitan. Solo esto son 150.000.
2. **Los árboles.** Un tronco *skinned* de 5.283 triángulos repetido 22 veces, para decorado que
   se ve a través de la ventana. Con un modelo de bajo polígono o un billboard bajan a casi nada.
3. **Las casas.** 12.768 triángulos por casa para fachadas de fondo.

También hay dos detalles técnicos:

- `botas` usa la malla del personaje (`Ch20`), 11.228 triángulos para unas botas.
- **Ninguna malla del garaje tiene collider.** Ni los armarios ni los maniquíes. No rompe nada
  hoy, pero hace que toda comprobación de oclusión por raycast mienta: los 7 rayos hacia los EPP
  dicen «despejado» mientras un maniquí los tapa. Lo descubrí por una captura, no por los números.

### Lo que falta para el hito del Quest 3

La plataforma de build **ya está en Android** y hay 2 escenas en la lista de build. **Ojo con el
orden:** la primera es `RideSafe_Garaje.unity`, la vieja, así que un build arrancaría en esa y no
en la del garaje v6. Hay que subir `RideSafe_Garaje_v5.unity` al primer puesto antes de compilar.

No he compilado el APK. Con 670.000 triángulos el build saldría, pero la experiencia en el visor
no sería representativa hasta bajar el presupuesto.

---

## 4. Lo menor, por si hay tiempo

- **Rehornear luz y occlusion** en la escena. v6 trae su propio horneado, pero el horneado es por
  escena y no viaja con el trasplante del mundo.
- **El casco `df_g_helmet_01` que no se dibujaba en Game y sí en Scene** ya no da problema: se ve
  correctamente en las capturas de PlayMode. Lo dejo como resuelto salvo que reaparezca.
- **El logo en el encabezado** está puesto pero se queda pequeño y cae cerca de una lámpara del
  techo, así que casi no se lee. Falta decidir tamaño y sitio.
- **Los marcadores de los puntos siguen verdes en la pantalla final**, sobre el vehículo, detrás
  del panel. No molesta, pero quizá deban apagarse al salir de la sección del vehículo.
- **`MenuRuntimeTextLocalizer` sigue sin unificar** con `UIText`.
- **Las 9 `TaskSequenceSO` siguen sin pasos** y nadie las ejecuta; el módulo lo mueve
  `Module01Director`. Es deuda vieja, no de esta tanda.
- **Ningún nivel de calidad asigna RP asset**; `Mobile_RPAsset` sigue sin usar. Para Quest esto
  importa.
- **Integrar `feat/garaje-v6` en `dev`** — y avisar a Cindy antes, que trabaja sobre
  `RideSafe_Garaje.unity`.

---

## 5. Lo que sí quedó hecho, para que no lo busques

Resumen rápido contra el documento de la reunión:

| § | Tarea | Estado |
|---|---|---|
| 1 | Plantilla de guion | Ya estaba entregada: `Docs/Guion_Modulo1_RideSafe_PLANTILLA.docx` |
| 1 | Sincronizar Drive | **Bloqueado** (1.6) |
| 2 | Bug de texto solapado en el checklist | Hecho: autoajuste con suelo de 12 y corte con puntos suspensivos |
| 2 | Canvas a la derecha y abajo + billboard | Hecho, con desplazamiento lateral propio |
| 2 | Contraste: tarjeta blanca, negrilla H1/H2 | Hecho |
| 2 | Branding: logo y relieve | Hecho, pero el logo se lee mal (§4) |
| 2 | «Mi Elección» y los 4 estados | Hecho (ver 2.2 y 2.3) |
| 3 | Escala 1:1, objetos apoyados | Hecho: se acabó la normalización a 32 cm |
| 3 | Ocultar duplicado al inspeccionar | Hecho |
| 3 | Efecto «puesto» y reaparecer al quitarlo | Hecho |
| 3 | Audio al tocar | **Bloqueado** (1.1) — enchufe listo |
| 4 | Rotación automática del vehículo | Hecho |
| 4 | Zoom al inclinar la cabeza | **No hecho.** En VR el cabeceo lo pone el visor; lo intenté con el ancla y rompió la geometría del popup. Hay que hacerlo con la cámara, en el visor |
| 4 | Highlight neón en sub-malla | **Parcial** (1.3): va en el marcador |
| 4 | Puntos RPG `!` | Hecho |
| 4 | Tres botones de decisión | Hecho, en color (ver 2.1) |
| 5 | Fade antimareo entre módulos | Hecho |
| 5 | Efecto cine | Hecho |
| 5 | Bloquear «Continuar» hasta el 100 % | Hecho |
| 5 | Sin mute, con pausa y repetir | Verificado: no hay botón de silencio |
| 5 | Componente modular para módulos 2–5 | Hecho: `TheaterMode`, sin nada del módulo 1 |
| 6 | Evaluación diferida al checkpoint final | Hecho |
| 6 | Umbral del 75 % y los 4 rangos | Hecho (ver 2.4) |
| 6 | Feedback por rango | Texto hecho en los dos idiomas; **audio bloqueado** (1.1) |
| 7 | Build en Quest 3 | **No hecho** (§3) |
