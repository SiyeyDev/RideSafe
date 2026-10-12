# Garaje v6 + las tareas de la reunión — diseño

> Fecha: 2026-10-12. Rama: `feat/garaje-v6` (sale de `feat/garaje-v5` tras el merge del tutorial).
> Fuente de las tareas: `carlos-rey-tareas-uiux-v3.md`, acuerdos de la reunión del equipo.

---

## 1. Qué se persigue

Dos cosas a la vez, y en este orden porque la segunda se apoya en la primera:

1. **Sustituir el garaje v5 por el v6** en `RideSafe_Garaje_v5.unity`, conservando la lógica y la
   presentación del módulo 1 que ya están verificadas.
2. **Aplicar las ~40 tareas de la reunión** sobre esa escena.

El orden importa porque v6 resuelve por sí solo dos pendientes que la reunión también pide
(objetos apoyados en la mesa, colocación a mano de los EPP). Hacer primero las tareas y luego
el cambio de garaje habría tirado ese trabajo.

**Criterio de éxito:** Charlie vuelve, abre la escena, entra pulsando Begin y recorre el módulo
con el garaje nuevo, los EPP sobre la mesa a escala real, el canvas desplazado y legible, el
vehículo girando por zona, el video que no deja continuar hasta el final, y una nota con rango.
Lo que no se pueda construir por falta de assets queda listado en un documento aparte, no
silenciado ni fingido.

---

## 2. Hallazgos que condicionan el diseño

Medidos, no supuestos. Todos verificados antes de escribir esto.

### 2.1 El paquete v6

942 MB, 436 assets, rutas `Assets/CITY/…` (layout de antes de la reestructura a `_Project`).

- **365 de 436 GUID ya existen** en `_Project/Models/CITY` → Unity sobrescribe **en sitio**,
  igual que pasó con v5. Entre ellos `scenas/Garage.unity`, que es exactamente de donde lee
  `Module01SceneBuilder.ArtScene`.
- **71 GUID son nuevos** → esos crean un `Assets/CITY/` duplicado fuera de `_Project`. Son
  árboles, `Carretera`, `anden`, `basura`, `metal table`, `soporte metal`, `audifonos`,
  materiales y los lightmaps/probes nuevos. Hay que moverlos a `_Project/Models/CITY/` **desde
  Unity** (mover dentro del editor conserva GUID, así que las referencias de la escena aguantan).

### 2.2 El constructor de escena quedó obsoleto

`Module01SceneBuilder.WorldRoots` lista 19 nombres de raíz. v6 solo conserva 5
(`enviroment`, `luces`, `FX`, `Box Volume`, `Directional Light`). Lo demás cambió:

| En v5 | En v6 |
|---|---|
| 7 EPP como raíces sueltas | agrupados bajo una raíz nueva **`epps`** |
| `ebike`, `ebike (1)` | `ebike`, `ebike (1)`, `ebike (2)` — las dos extra sobre un `soporte metal` nuevo |
| `Ch20_nonPBR` | tres instancias |
| — | `Camera` y `Main Camera` (no deben pasar: la escena ya tuvo el bug de dos cámaras) |

Correr `Montar escena jugable` tal cual haría dos daños: regenera la escena y se lleva la
presentación (trampa ya documentada en `Docs/memory.md` §9), **y** dejaría la escena sin EPP
porque ya no están donde los busca.

→ **Hace falta un comando nuevo, no destructivo, que cambie el mundo sobre la escena abierta.**

### 2.3 v6 trae resuelto el bug de los objetos flotando

Comparación de posiciones:

| | v5 (hoy) | v6 |
|---|---|---|
| z de los 7 EPP | **−3.20 los siete** | −4.2 a −4.6, repartidos |
| y de los 7 EPP | **1.30 los siete** | 1.15 / 1.16 / 1.17 / 1.18 / 1.19 / 1.30 / 1.44 |

Una `y` distinta por objeto significa **apoyado en una superficie**. La fila de v5 la monta
`Module01PresentationBuilder.ArrangeStations`, que reparenta todo a `Module01_Spawned`, lo
coloca flotando a 1.5 m del ancla y lo normaliza a 32 cm en su lado mayor. Eso es a la vez:

- el pendiente «los objetos no se apoyan en la mesa» de `memory.md` §8,
- la tarea §3 «garantizar que todos los objetos descansen físicamente sobre la superficie»,
- y la tarea §3 «escala 1:1, la linterna no parezca enana» (la normalización a 32 cm es
  exactamente lo que hace enana a una linterna y gigante a un tornillo).

→ **`ArrangeStations` deja de colocar y escalar los EPP.** Las entidades se etiquetan encima de
los objetos donde el arte los puso, que es para lo que existe `Module01SceneEntities`.

### 2.4 El vehículo es una malla única — bloquea el highlight por componente

`memory.md` lo midió y lo confirmé: `ebike` y `scooter` son **malla única sin hijos**
(bounds 0.66×1.11×1.97 y 0.55×1.35×1.27). La tarea §4 pide «delineado naranja neón únicamente
en la sub-malla del componente activo».

**No hay sub-mallas que delinear.** Esto no se arregla en código: el equipo de arte tiene que
entregar los vehículos separados en sub-objetos (frenos, luces, llantas, manubrio, batería).
Va al documento de faltantes. Lo que sí se puede hacer, y se hace, es el highlight sobre el
**marcador** del punto de inspección, que sí es un objeto propio.

### 2.5 Cero audio en el proyecto

`find` sobre `Assets/_Project` da **0 archivos .wav/.mp3/.ogg**. Las tareas §3 (locución al
tocar un objeto) y §6 (audio de feedback por rango) no se pueden completar.

→ Se construye **el enchufe**, no el contenido: campo de `AudioClip` en los datos, un
reproductor que lo dispara, y silencio limpio cuando el clip es null. Cuando lleguen las voces
se arrastran al asset y funciona sin tocar código. Los clips faltantes van al documento.

### 2.6 Video: solo `VideoTest.mp4`

Los dos videos de revisión no existen (ya estaba en `memory.md`, bloqueado a propósito). El
modo teatro y el bloqueo de «Continuar» **sí** se pueden construir y probar con `VideoTest.mp4`.

### 2.7 Cosas que ya estaban hechas y la reunión volvió a pedir

Antes de construir nada conviene no construirlo dos veces:

- **Los 3 botones de decisión** (`RoadReadyButton` / `NeedsServiceButton` / `DoNotRideButton`)
  ya existen y están cableados. Lo que falta es el color 🟩🟨🟥.
- **El billboard de §2** ya existe: `RideSafe.UI.GazeFollower`, con zona muerta y suavizado.
  Solo hay que colgarlo del canvas del checklist.
- **La plantilla de guion de §1** ya está entregada en
  `Docs/Guion_Modulo1_RideSafe_PLANTILLA.docx` (commit `3f5e9ea`).
- **El fade** de §5 ya existe como `CanvasGroup` en `GuidedTour._fade`; falta usarlo en la
  transición módulo 0 → módulo 1.
- `_videoComplete` ya se calcula en `Module01Experience` — **y no se usa para nada**. El
  bloqueo de «Continuar» de §5 es conectar una variable que ya está ahí.

### 2.8 Un conflicto con una decisión anterior

`memory.md` registra: «los tres botones del go/no-go quedaron **secundarios** a propósito: en
una evaluación, un pill coral en `RoadReady` sugiere la respuesta antes de que el aprendiz lea
el texto». La reunión pide ahora 🟩🟨🟥.

**La reunión es posterior, así que manda la reunión.** Se implementan los colores y el conflicto
queda anotado en el documento de faltantes para que Charlie lo confirme o lo revierta.

---

## 3. Diseño

### Fase A — el garaje v6

**A1. Importar.** Por `AssetDatabase.ImportPackage(path, false)` desde el relay de Unity, sin
diálogo. Red de seguridad: rama `feat/garaje-v6` y el import en su propio commit, para poder
volver con un `git checkout`.

**A2. Recolocar los 71 nuevos.** `AssetDatabase.MoveAsset` de `Assets/CITY/…` a
`Assets/_Project/Models/CITY/…`, creando las carpetas que falten. Mover desde Unity conserva
GUID, así que la `Garage.unity` recién importada sigue encontrándolos. Al final `Assets/CITY`
tiene que quedar vacío y se borra.

**A3. Comando nuevo: `Module01WorldSwap`.** No destructivo, sobre la escena abierta. En
`Module01/Editor/`, al lado de `Module01VehicleWiring`, siguiendo ese patrón.

Qué hace, en orden:
1. Verifica que no está en Play y que la escena abierta es la de trabajo.
2. Abre la escena de arte en modo aditivo.
3. Borra de la escena de trabajo las raíces que son mundo, **por lista explícita de lo que es
   mundo**, nunca «todo lo que no reconozco»: así un objeto de lógica nuevo no desaparece en
   silencio.
4. Trae de la escena de arte todas sus raíces **menos** `Camera` y `Main Camera`, que las pone
   la presentación.
5. Descarga la escena de arte y deja la de trabajo marcada como sucia.

La lista de lo que es mundo sale de la escena de arte, no está escrita a mano: lo que es raíz
allí es mundo aquí. Eso lo hace sobrevivir a un garaje v7 que vuelva a reagrupar.

**A4. Re-etiquetar entidades.** `Module01SceneEntities` sobre los hijos de `epps` y los objetos
de `enviroment` que son del catálogo (`botas`, `df_g_helmet_01`). Las entidades se quedan
**donde el arte las puso**.

**A5. Colliders y bake.** Reponer los MeshCollider del suelo/paredes y rehornear luz y
occlusion. v6 trae sus lightmaps, pero son de `Garage.unity`: el bake es por escena y no viaja
con el trasplante.

### Fase B — §3, los EPP sobre la mesa

**B1. `ArrangeStations` deja de tocar los EPP.** Pierde el reparent, el reposicionado y el
`localScale *= .32f/size`. Conserva **solo** el ajuste del `BoxCollider` a los bounds reales,
porque sin eso «se hace clic en la nada» sigue pasando — y ahí hay que añadir un **volumen
mínimo de clic**, que es lo que faltaba para la guaya y el teléfono (láminas de 2 cm,
invisibles de canto pero con collider).

Las anclas de zona sí se siguen colocando: son el punto de vista, no los objetos.

**B2. Ocultar el duplicado al inspeccionar.** `ItemInspector` instancia una copia del
`DisplayPrefab` sobre el pedestal y el original se queda en la mesa: se ven dos. Al presentar
se apaga el objeto de mundo del item, y al terminar se vuelve a encender.

**B3. Efecto «puesto».** Al aceptar un EPP desaparece de la mesa; al quitarlo del checklist
reaparece. Vive donde ya está el estado: `ZoneRunner` conoce el libro y la lista lateral.

**B4. Escala real.** Los `DisplayPrefab` del inspector siguen normalizándose a `DisplaySize`
—eso es correcto, es una vitrina, todo se mira al mismo tamaño— pero los objetos **del mundo**
no. Si alguno queda desproporcionado en la mesa es del FBX y va al documento.

### Fase C — §4, el vehículo

**C1. El ancla `vehicle`.** Punto único de exhibición en el centro del `piso`. El código ya
está y está guardado tras `_tour.HasAnchor(VehicleAnchorId)`; crear el ancla lo activa.

**C2. Autorar los yaw.** Hoy los 6 (3 por perfil) están en 0, así que el vehículo «gira» a la
misma posición siempre. `cockpit` de frente, `wheels` de costado, `visibility` por detrás.
Derivados de los bounds, **aproximados: necesitan ojo humano**, y así se dice.

**C3. Marcadores `!` estilo RPG.** Los 5 puntos dejan de ser esferas en fila bajo
`Module01_Spawned` y pasan a colgar de la malla del vehículo, viajando con el giro. Cada uno
lleva un `!` que mira a la cámara. Clic valida; no hacer clic cuenta como omisión, que es
exactamente lo que ya hace el grader con `CoreOmitted`.

**C4. Highlight neón naranja.** Sobre el marcador del punto activo, no sobre la sub-malla
(ver 2.4). Shader de URP propio, porque el `Outline.shader` de AutoHand es del pipeline viejo.

**C5. Los 3 botones a color.** 🟩 verde / 🟨 ámbar / 🟥 rojo. Ver el conflicto de 2.8.

### Fase D — §2 y §6, la UI

**D1. Solapamiento del texto español.** Medido en **unidades de canvas**, nunca en píxeles de
pantalla (volver a equivocarse en esto costó un arreglo correcto revertido). Autosize y wrap en
las filas del checklist, y la fila crece en alto en vez de pisarse.

**D2. Canvas a la derecha y abajo** + `GazeFollower` colgado del checklist.

**D3. Contraste.** La línea gris descriptiva pasa a tarjeta de fondo blanco puro con texto
negro. H1 y H2 en negrilla. Iconos de leyenda más brillantes.

**D4. Branding.** `Assets/_Project/UI/ridesafe-logo.png` en los encabezados de panel, y relieve
3D en las tarjetas con los panel 9-sliced del atlas.

**D5. «Mi Elección».** Fuera la tabla de referencia duplicada (hoy `ShowReport` pinta las dos:
`entries` y `Reference(entries)`). Un único panel centrado. Los 4 estados con icono y color.

Sobre el cuarto estado: el enum `ItemStatus` tiene hoy `Selected`, `CoreOmitted`,
`ConditionDependent`, `NotSuited`. La reunión pide correcto / incorrecto / omisión / **en riesgo
o deteriorado**. `ConditionDependent` es el chaleco —aceptable pero no exigido—, que no es «en
riesgo». Se añade `AtRisk` y el grader marca así el elemento deteriorado elegido
(`helmet_damaged`), separándolo de `NotSuited`, que queda para la elección equivocada de otra
naturaleza (el teléfono en la mano). La leyenda enseña los 4 que pidió la reunión;
`ConditionDependent` se pinta neutro. **Va al documento para que Charlie lo confirme.**

**D6. Nota con rango (§6).** `SelectionGrader` no calcula porcentaje hoy, solo listas. Se añade
una capa de puntuación pura (sin Unity, como el resto de `Scoring/`), con los cortes de la
reunión: ≥75 aprobado y desbloquea «Continuar al Módulo 2»; 50–74, 25–49 y <25 desbloquean solo
«Repetir Módulo». La nota se retiene hasta la pantalla de confirmación final, que es punto de no
retorno. Esto va con TDD: es lógica pura y es donde un error se paga caro.

### Fase E — §5, el teatro

**E1. Bloquear «Continuar» hasta el 100%.** Conectar `_videoComplete`, que ya se calcula.
Botón no interactuable hasta entonces, para que no parezca roto.

**E2. Atenuar el entorno** durante el video: un panel negro a pantalla completa en la capa UI,
por debajo del panel del video y por encima del 3D.

**E3. Fade módulo 0 → módulo 1** y en los reposicionamientos, con el `CanvasGroup` que ya existe.

**E4. Sin mute.** Verificar que no hay botón de silencio; pausa y repetir se quedan.

**E5. Componente reutilizable** para los módulos 2–5: el teatro sale de `Module01Experience` a
una clase propia.

### Fase F — §7, Quest 3

Medir triángulos después de v6 (v5 ya iba en 370k con presupuesto de 150–350k, y v6 añade
árboles, carretera y andén: va a subir). Configurar el build de Android y reportar.
**No prometo APK instalado**: el build de Android no es el objetivo inmediato según `memory.md`
y cualquier bloqueo de toolchain se reporta en vez de fingirse.

---

## 4. Pruebas

El proyecto ya tiene 145 EditMode + 3 PlayMode en verde, y la regla del repo es que los tests
toquen la UI, no solo la lógica, y que PlayMode entre **pulsando Begin**, no por atajo.

- **EditMode/TDD** para todo lo que es lógica pura: la puntuación y los rangos (D6), el estado
  `AtRisk` (D5), la visibilidad por libro (B3). Test primero.
- **EditMode sobre la escena** para lo estructural: que tras el swap existan las 12 entidades,
  que ninguna esté en `Module01_Spawned`, que las `y` de los EPP sigan siendo distintas (si
  vuelven a ser todas iguales, alguien volvió a colocarlos en fila), que no haya dos cámaras.
- **PlayMode** para el recorrido: los 3 tests que ya existen tienen que seguir verdes, y se
  añade que «Continuar» no responde antes de que el video acabe.
- **Capturas reales** para lo visual, exigiendo marca de escritura nueva (una captura que solo
  comprueba que el archivo existe miente).

Antes de lanzar PlayMode, comprobar `EditorApplication.isPlaying` y salir si está en true: es
la causa de todos los «fallos intermitentes» del 2026-10-07.

---

## 5. Lo que no entra

- Las voces y los dos videos: no existen los archivos (2.5, 2.6).
- El highlight por sub-malla del vehículo: no existen las sub-mallas (2.4).
- Alinear la hoja de I2 del Drive: la hoja original no está en este Drive, y por el relay no hay
  API de celdas. Se exporta lo que el asset tenga para que alguien la alinee a mano.
- APK instalado en el visor: se deja configurado y medido, no prometido (Fase F).

Todo esto va, con su motivo, a `Docs/Pendientes_Reunion_2026-10-12.md`.
