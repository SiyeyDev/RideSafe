# Qué falta — tareas de la reunión, garaje v7

> Fecha: 2026-10-12 (actualizado por la noche, con el garaje **v7** ya dentro).
> Escena: `Assets/_Project/Scene/RideSafe_Garaje_v5.unity` — conserva el nombre, pero ya es el v7.
> Estado de pruebas al cerrar: **4/4 PlayMode y 286/288 EditMode**. Los dos que fallan son del
> tutorial del módulo 0 (`PF_Module00_UI` trae `Localize` con `LocalizeOnAwake` y reescribe el
> título bilingüe del panel de idioma); vienen de esa rama y no se tocaron.

---

Este documento es la respuesta a «si falta algo en 3D o UI/UX, avisa en un documento nuevo».
Está ordenado por lo que **bloquea**: primero lo que no se puede hacer sin que llegue algo de
fuera, luego lo que se hizo con una decisión mía que conviene que confirmes, y al final lo
menor.

---

## 0. Lo que cambia con el v7

**Lo que resuelve.** El v7 es, sobre todo, **el horneado de luz**: 4 lightmaps, 2 sondas de
reflexión y el `LightingData`. De los 596 GUID del paquete, 434 ya estaban y solo **34 ficheros
cambian de verdad**; casi todos son el bake. Y nos entra gratis: nuestra escena de trabajo ya
referenciaba el mismo `LightingDataAsset`, así que **330 de 423 renderers** quedan iluminados por
el bake de Daniel sin hornear nada aquí.

También se lleva los **tres maniquíes en pose T** que eran el 22 % del presupuesto de triángulos,
y reemplaza la raíz `enviroment` por tres prefabs (`interior garage`, `barrio_exterior`,
`enviroment_decoration`).

**Lo que trae y hay que decidir.** 162 ficheros nuevos: una mesa de herramientas y **seis prendas
que parecen pensadas como EPP**: bufanda, jean ajustado, pantalón ancho, «pant low», tapabocas y
mascada, más dos pares de guantes y unos de látex. Ninguna está en el catálogo, así que hoy son
solo decorado. **Esto necesita decisión del cliente**, porque añadir una prenda al catálogo cambia
las respuestas correctas del ejercicio: hasta ahora la zona de ropa tiene dos elementos (chaleco y
calzado) y ningún distractor de vestuario.

**Lo que hubo que corregir.** El v7 planta esas seis prendas **de pie, en el pasillo, delante de
la estación de ropa** (x de −0,01 a 1,85, z ≈ −3,0). Desde ese punto llenaban el encuadre y
tapaban el mostrador. Se apartan solas a la pared de al lado, a un hueco que se busca. Y una regla
nuestra, `RemoveStrayCopies`, **borraba las dos e-bikes de adorno** que el v7 cuelga de una
estantería, porque se llamaban `ebike (1)` y `ebike (2)`; ahora solo borra lo que está encima del
punto de exhibición.


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

### 2.2 «Mi Elección» es un panel con UNA sola columna — resuelto

Se pidió «eliminar la tabla duplicada de *Referencia para este recorrido*» y «unificar en un
único panel centrado». La tabla duplicada está eliminada —era literalmente la misma lista con
los estados cambiados, o sea cero información nueva— y **la lista va en una sola columna**, como
pediste después.

Caben las doce filas que puede llegar a tener porque cada una es de una sola línea: el estado va
como palabra a su derecha (CORRECTA, INCORRECTA, OMISIÓN, EN RIESGO, CONDICIONAL) en vez de en
una leyenda al pie. Eso resolvió tres cosas de golpe: se lee sin bajar la vista a descifrar
iconos, desaparece el solape de las etiquetas largas en español, y la fila deja de necesitar una
segunda línea.

De paso: **cada fila mostraba el id crudo** (`helmet_ok`, `point_brakes`) debajo del título. Era
texto de depuración a la vista del aprendiz; ya no se pinta, pero sigue en los datos para que los
tests lo puedan afirmar.

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

- **Lo que se planta delante del mostrador se aparta.** En el v6 eran tres maniquíes; en el v7
  son seis prendas de pie que tapaban la estación de ropa entera. Van a la pared de al lado, a un
  hueco libre. Si el v8 las vuelve a poner en medio, se vuelven a apartar solas.
- **Los 7 EPP estaban desparramados** a lo largo de 5 metros de mostrador (la zona `head` ocupaba
  4,99 m), así que desde un punto de vista se veían diminutos. Los agrupé por zona. Además tres
  estaban **hundidos en la tapa** —`casco 1` tenía la base 16 cm por debajo—; ahora los siete
  apoyan exactamente.

---

## 3. Rendimiento: el problema más gordo que queda

### 611.411 triángulos con el v7. El presupuesto de Quest es 150.000–350.000.

Sigue siendo casi el doble. El v7 **no baja el conteo**: se lleva los maniquíes (−150.000) pero
mete más decorado. Medido sobre la escena real, por raíz:

| Triángulos | Qué | % |
|---:|---|---:|
| **239.170** | `barrio_exterior` — el vecindario que se ve por la ventana | 39 % |
| **115.042** | `interior garage` | 19 % |
| 66.156 | `enviroment_decoration` | 11 % |
| 64.601 | `epps` — los 7 del ejercicio **y** las prendas nuevas | 11 % |
| 76.710 | Las **3** e-bikes: la del ejercicio y 2 de adorno en una estantería | 13 % |
| 25.856 | `scooter` | 4 % |
| 14.928 | `House04` | 2 % |

**El 39 % se va en el exterior, que solo se ve por una ventana.** Por orden de ganancia:

1. **`barrio_exterior`.** 239.000 triángulos de fachadas y árboles para un fondo de ventana.
   Las mallas más caras siguen siendo las mismas: `House03` (34.377), `Tree_Trunk` (28.182
   sumando repeticiones), `House01` (25.536). Un *impostor* o un recorte del vecindario a lo que
   de verdad entra por el hueco de la ventana es, solo, la mitad del exceso.
2. **Las dos e-bikes de adorno**, 51.140 triángulos de la malla completa del vehículo para dos
   bicis colgadas de una balda. Una versión de bajo polígono las deja en nada.
3. **`df_g_container_01` (36.528) y `df_g_canister_01` (25.494)**, sumando repeticiones: cajas y
   bidones de almacén.

También hay dos detalles técnicos:

- `botas` usa la malla del personaje (`Ch20`), 11.228 triángulos para unas botas.
- **Ninguna malla del garaje tiene collider.** Ni los armarios ni los maniquíes. No rompe nada
  hoy, pero hace que toda comprobación de oclusión por raycast mienta: los 7 rayos hacia los EPP
  dicen «despejado» mientras un maniquí los tapa. Lo descubrí por una captura, no por los números.

### Lo que falta para el hito del Quest 3

La plataforma de build **ya está en Android**. La lista de build tenía primero
`RideSafe_Garaje.unity`, la vieja, así que un build habría arrancado en esa y no se habría visto
nada de esto; **ya está corregido**, con la del v6 en primer lugar. Es un cambio en
`ProjectSettings`, así que conviene mirarlo al integrar con Cindy.

No he compilado el APK. Con 670.000 triángulos el build saldría, pero la experiencia en el visor
no sería representativa hasta bajar el presupuesto.

---

## 4. Lo menor, por si hay tiempo

- **Rehornear luz y occlusion** en la escena. v6 trae su propio horneado, pero el horneado es por
  escena y no viaja con el trasplante del mundo.
- **El casco `df_g_helmet_01` que no se dibujaba en Game y sí en Scene** ya no da problema: se ve
  correctamente en las capturas de PlayMode. Lo dejo como resuelto salvo que reaparezca.
- **El logo en el encabezado** ya se lee: el título y el logo van sobre una barra oscura, que de
  paso cierra el viejo «fondo para el título del reporte». Sigue siendo pequeño; si lo quieres
  más grande o en otro sitio, es un número.
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
