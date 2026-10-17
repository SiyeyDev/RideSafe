# RideSafe — estado del proyecto

> Documento vivo. Se actualiza a medida que avanza el trabajo.
> Última actualización: 2026-10-16 (noche, 3) — revisión de las UIs: cada pantalla en una caja,
> tutorial más pequeño y que ya no sale sobre el video, SpriteAtlas de la UI (§11). Antes, (noche,
> 2): arreglos de la prueba: el calzado se ve en el
> pedestal y toda la rueda delineada es clicable. Antes, esa noche: limpieza de assets sin uso y
> optimización para Quest (`198bb1e`). Por la tarde: la categoría de I2 pasa a `M1/`, locución de los 18
> elementos en dos idiomas, y el EPP elegido se queda en la mesa en verde
>
> **Lo que falta hoy, con nombres y números: `Docs/Pendientes_Reunion_2026-10-12.md`.**

---

## 1. Qué es

Experiencia VR de formación en movilidad (bici eléctrica / patinete) para **Meta Quest**.
Hoy se prueba en **modo PC con mouse**; el VR y el build de Android no son el objetivo inmediato.

- Unity **6000.4.9f1** (es lo que dice `ProjectVersion.txt`; Charlie abre con **6000.5.4f1**
  y Cindy con 6000.4.9f1, así que ese archivo hace ping-pong en cada commit).
- URP, AutoHand para manos e interacción, **I2 Localization** para textos.
- Escenas: `Assets/_Project/Scene/RideSafe_Garaje.unity` (la de siempre) y
  **`RideSafe_Garaje_v5.unity`** (la nueva, con el garaje del paquete de arte).
- Idiomas: **English (en)** y **Español (es-CO)**.

---

## 2. Documentos de diseño

Hay dos PDF y **se contradicen**: `RideSafe VR GDD VF1` (BETA FINAL v0.9) y
`RideSafe_VR_Module_Narratives_v2.0`.

Para el módulo 1 **manda la narrativa v2.0**: es posterior, es la única con detalle
suficiente para construirlo, y es la que el equipo siguió al generar `PF_Module01_UI`.

| Tema | GDD v0.9 | Narrativa v2.0 | Decidido |
|---|---|---|---|
| Transferencia cambiada | La exige | La elimina (§4.10) | Fuera, fase 2 |
| Duración M1 | 2:00 | 3–4 min | 3–4 min |
| Familiarización de controles | Antes del M1 | Después (§4.11) | Después |
| HUD de velocidad | Persistente | Prohibido | No aplica al M1 |

**Ojo:** la narrativa declara canónico un `RideSafe_VR_GDD_v2.0_Evidence_Based.md`
que el equipo nunca compartió. Si aparece, hay que revisar la spec.

Spec y plan del módulo 1: `docs/superpowers/specs/2026-10-05-modulo-01-ready-to-ride-design.md`
y `docs/superpowers/plans/2026-10-05-modulo-01.md`.

---

## 3. Ramas y estado de git

- **`main`** — rama compartida. Cindy trabaja aquí. Va por detrás: su punta (`ffcc058`) es
  ancestro de `dev`.
- **`dev`** — **al día desde el 2026-10-15 y pusheada** (`545dd19`). Tiene todo: el garaje
  **v9**, el módulo 1 entero con sus 4 estaciones, el patinete despiezado y los materiales e
  iluminación de Cindy.
- **`feat/garaje-v6`** — la rama de trabajo. **Apunta al mismo commit que `dev`** (`545dd19`),
  así que ya no hay nada «sin mezclar».
- **`feat/garaje-v5`** — congelada, muy por detrás. No usarla.
- **`Feature/tutorial`** — la de Cindy. Su último commit ya está integrado; lo que suba a partir
  de ahora hay que traerlo **con cuidado**, porque sus importaciones caen en `Assets/CITY` y
  duplican GUID. Ver §9.

**2026-10-16 — lo de Cindy ya está commiteado** (`7e98a68` y `e750492`): el tutorial agnóstico,
los paneles del M0 y el M1. Entró **todo en un commit**, incluido lo que su propio plan decía
dejar fuera: `packages-lock.json`, `Assets/Settings/*`, `OpenXRPackageSettings`, las fuentes SDF
y `Assets/_Recovery/0.unity` (3,4 MB de escena de recuperación que sobra en el repo).

**2026-10-16 (noche) — `198bb1e`, de Cindy: limpieza y optimización para Quest**, ya subidas
(detalle en la bitácora). Ese commit **quita `Assets/_Recovery/`**, así que lo de arriba ya no
aplica. Queda sin subir solo el borrado de `Assets/___www.unityassetcollection.com___.txt`.

**El horneado NO se commitea** hasta que el equipo acuerde una versión de Unity. El que está en
el repo es de 6000.5.4f1 y **no carga en 6000.4.9f1**: a Cindy le sale la escena negra. Ella
rehízo el suyo en su versión y se quedó sin subir, a propósito.

**Falta avisar a Cindy de que `dev` se movió**, porque ella trabaja sobre
`RideSafe_Garaje.unity` y ahora `dev` trae el mundo del **v9**, el horneado nuevo y una
estación más.

**La escena sigue llamándose `RideSafe_Garaje_v5.unity` pero su mundo ya es el v9.** El nombre
se conservó a propósito: cambiarlo habría roto las referencias de la lista de build, el montador
de presentación y los tests, a cambio de nada.

---

## 4. Qué funciona hoy

### Módulo 0 — verificado corriendo
Navegación completa, 35 textos localizados, cambio de idioma en vivo, narración con
subtítulos palabra por palabra, video final, colliders del garaje.

Desde el 2026-10-16, en la pantalla de vehículo:
- **Tarjetas ajustadas al texto** (`FitCardToContent`): cada tarjeta mide lo que su nombre en el
  idioma elegido; al seleccionarla crece lo justo para poner el check a su lado. Siempre queda
  centrada sobre su vehículo.
- **Barra de confirmación en dos líneas**: la frase del vehículo y «¿confirmar para continuar?»
  ya no se pisan.
- **Vitrina propia fuera del garaje** (`Module00 Vehicle Showroom`): copias de exhibición de la
  e-bike y la patineta, a escala 0,6 y a 2,25 m, sobre copias de la plataforma con partículas,
  bajo sus etiquetas. El vehículo elegido gira con sus partículas (`VehiclePreviewController`).
  Los vehículos reales del M1 no se tocan.

### Tutorial — agnóstico, montado en la v5 (2026-10-16)
Un apoyo que **no conoce ningún módulo** y no modifica su código. Muestra el Touch Plus de la mano
dominante con el gatillo iluminado, resalta dónde actuar y, solo la primera vez, una frase corta.

- **Cuándo aparece:**
  - la primera vez de cada tipo de interacción (interfaz u objeto 3D) en la sesión;
  - cuando el usuario se atasca: 10 s sin un clic, con todas las opciones resaltadas por igual
    y sin texto.
- **Cuándo calla:** se pausa con un video o una narración, y está en pausa en la orientación
  del M1 (`TutorialPause` dentro de `TutorialHelp`).
- **Dónde se coloca:** abajo al centro; si ahí tapa un panel, se aparta al lado que menos tape.
  Un roce de menos del 5 % de su tarjeta no cuenta (`HintSpot.Tolerance`): en Welcome el borde
  rozaba «Comenzar» y el control se iba al lado sin motivo.
- **Cómo lo llama un módulo (solo si hace falta):** `TutorialHelp.Show/Stop/Resume/Pause`, o los
  componentes `TutorialCue` / `TutorialPause`.
- **Spec y plan:** `Docs/superpowers/specs/2026-10-15-tutorial-agnostico-design.md` y
  `Docs/superpowers/plans/2026-10-15-tutorial-agnostico.md`.
- **Montaje:**
  - menú `RideSafe → Tutorial Help → Montar en RideSafe_Garaje_v5`, idempotente; quita el
    objeto `Tutorial` viejo;
  - los textos se añaden con `RideSafe → Tutorial Help → Añadir textos (I2)`.
- **El halo de los objetos 3D vive en el tutorial**, no colgado del objeto: lo sigue cada frame
  y se oculta con él. Colgado, el M1 lo copiaba al pedestal junto con el EPP. Un objeto fundido
  por el static batching no lleva halo.
- **Tests:** 70 de EditMode en `TutorialHelp.Tests`.
- **El tutorial viejo** (`Systems/Tutorial`, basado en TaskSequence) sigue en el repo porque lo
  usa `Garaje_Test`; su retirada es una limpieza pendiente.

### Módulo 1 — lógica y presentación, las dos probadas corriendo

Los **6** tests de PlayMode corren sobre la escena real y **entran pulsando Begin**, no por
atajo. Verdes dos corridas seguidas el 2026-10-14, junto con **347 de 349 de EditMode** —los 2
que fallan son los de siempre del módulo 0, que vienen de la rama del tutorial—.

Desde la tanda del 2026-10-12 el módulo además: esconde el EPP de la mesa mientras gira en el
pedestal y lo deja escondido al llevárselo puesto; atenúa el garaje durante los videos; no deja
saltarse el video; exhibe el vehículo en un punto único y lo gira por zona; y desde el v8 **delinea la
pieza real** del punto que toca (manubrio y rueda trasera), dejando el `!` solo donde no hay
pieza; y cierra con «Mi Elección», la nota con rango y los dos botones que esa
nota desbloquea. **Desde el 2026-10-14 el patinete también llega despiezado**, y delinea tres de
sus cinco puntos: dirección, freno y rueda trasera.

| Lo que se pidió | Estado |
|---|---|
| Puerta del garaje + fade + teleport | hecho y afirmado |
| Moverse entre las 6 zonas | hecho; el test exige llegar al ancla con 5 cm y 1° |
| Fade entre zonas | hecho; el test exige `alpha >= .99` en **cada** cambio |
| EPP al frente girando 360° | hecho; el test exige que el modelo gire |
| Popup con el nombre arriba | hecho; el test exige que no empiece por `Module1/` |
| Popup de confirmación con Sí/No | hecho; se pulsa por raycast, no por `Accept()` |
| Lista lateral siempre visible | hecho |
| Reporte final en pantalla | hecho; el test exige ver `point_brakes` |

Todos recorren el **módulo 0 real** (Language → Jurisdiction → Vehicle → Comfort) y ninguno
escribe en la escena: el interruptor es `Module01Experience._quickStart`, que hoy va **vacío**
para que al abrir la escena a mano se vean las cinco pantallas. Si alguien se lo asigna, los
tests se marcan *ignorados* diciendo por qué, en vez de fallar sin explicación.

- `El_modulo_recorre...` — recorrido completo con subtítulos encendidos
- `Rechazar_el_casco...` — rechaza el casco bueno y lo reporta como omisión crítica
- `Elegir_patinete...` — pulsa la tarjeta del patinete y afirma que el director usa ese perfil,
  que el patinete se ve y que la bici queda apagada
- `El_patinete_recorre_sus_zonas...` — las seis zonas enteras con el patinete, afirmando que cada
  contorno cuelga de su pieza, que el signo se calla donde hay contorno y que la esfera clicable
  se muda encima de lo que brilla. **Hasta el 2026-10-14 nadie recorría el patinete**: el test de
  arriba se paraba en el onboarding, y con el patinete en malla única eso bastaba

**Lo que ya no cubre ningún test:** la entrada rápida con `SO_Module00_TestSettings`.

Desde el 2026-10-16:
- **Los `!` de la e-bike reciben el clic.** Antes la zona clicable era la esfera del punto,
  8 cm de radio y **26 cm por debajo** del signo, invisible. Ahora cada `!` tiene un hijo
  `ClickArea`: trigger de 12 cm en capa Default, que se apaga con el signo y entrega el clic
  al punto.
- **«Revisa tus elecciones» crece con la lista** (`GrowWithContent`): con 7 elementos ya no
  pisa el aviso ni los botones.
- **«Mi Elección» va en una sola columna** (`ColumnComposition`): barra, nota y lista miden
  lo mismo, el título está centrado en las dos direcciones y la nota va bajo la barra.
- **Los botones finales siguen a la lista** (`FollowBelow`).
- **Arreglos sobre el prefab sin regenerarlo:** `RideSafe → Módulo 1 → Arreglar composición de
  paneles`. En la escena hay otro menú: `Quitar sobrescritura de la nota de Mi Elección`.
- **Pendiente de comprobar:** los 6 tests de PlayMode del M1 tras estos cambios.

---

## 5. Arquitectura del módulo 1

### El motor ya existía

`TaskSequence` (de Cindy) es el motor: `TaskSequenceSO` como dato, `TaskStepData` con
clave de localización y de audio, y el runner con fases
`Prepare → Present → WaitForInteraction → Validate → Feedback → Complete → Cleanup`.

### Modularidad entre vehículos: por contexto, no por código

`ContextRequirement` compara la clave `vehicle`. Hay 6 secuencias; las 3 del vehículo
exigen `vehicle == ebike`. Esta regla aplica a todos los módulos.

**Resuelto el 2026-10-07 (tramo 1).** El director tiene `List<VehicleProfileSO> _vehicles` y
`SelectVehicle(string)`, que llama `Module01Experience.StartZones` con el valor del contexto,
justo antes de `Begin()`. Sin elección usa el primero de la lista; con un vehículo sin perfil
avisa por consola y conserva el anterior, en vez de dejar la sección de vehículo vacía.

Queda desmentido el «añadir el scooter es cero líneas de código» de la spec §3.2. Hoy los dos
perfiles comparten las mismas 3 `ZoneSO`, así que el recorrido coincide por contenido; la lista
es lo que guarda que siga valiendo si algún día divergen.

### Enlace por nombre, nunca overrides en el prefab

Patrón de Cindy (`Module00TutorialBinding`): los componentes se localizan en runtime por
id de panel y nombre de objeto. Cada binding expone `FindMissing()` que un test ejecuta
contra el prefab. **Esto resolvió la vieja trampa de "regenerar prefabs borra el cableado".**

### Localización

`Cachacos.ILocalizationProvider` + `I2LocalizationProvider`, resuelto por `ServiceLocator`.
Evita que `RideSafe.UI` dependa de I2, que no tiene asmdef. `MenuRuntimeTextLocalizer`
quedó obsoleto frente a `UIText`; falta unificar.

### Código del módulo 1 — `Assets/_Project/Module01/`

| Pieza | Qué hace |
|---|---|
| `SafetyItemSO` / `ZoneSO` / `Module01CatalogSO` | catálogo con las 4 categorías |
| `VehicleProfileSO` | vehículo, su modelo, sus zonas y **qué pieza delinea cada punto** |
| `SelectionLedger` / `SelectionReport` / `SelectionGrader` | puntuación pura, sin Unity |
| `ReadinessDecision` | go/no-go contra lo encontrado |
| `WorldObjectTaskEntity` | entidad 3D vía EventSystem; sirve con mouse y con AutoHand |
| `Module01Binding` | enlaza `PF_Module01_UI` por nombre, con `FindMissing` |
| `ReportPresenter` | pinta el reporte, repinta al cambiar idioma |
| `ItemInspector` | presenta el objeto y confirma |
| `ZoneRunner` | conecta entidad, inspector y libro por zona; avisa a los `IInspectionCue` |
| `IInspectionCue` | cómo señala un punto: el signo `!` o el contorno. El recorrido no sabe cuál |
| `InspectionMarker` | el signo `!` flotante. Se **silencia** si su punto tiene contorno |
| `InspectionHighlight` | delinea la pieza real: hijo con su misma malla y casco invertido |
| `GuidedTour` | fade y reposición del rig |
| `VehicleStage` | enciende la malla del vehículo elegido, apaga la otra y la gira por zona |
| `Module01Director` | encadena las seis zonas y los dos reportes; elige perfil de vehículo |
| `Module01Context` | publica `vehicle` / `language` |

### Herramientas de editor (menú `RideSafe → Módulo 1`)

- `Crear datos de la primera pasada` — catálogo, zonas, greybox y **los 2 perfiles de vehículo**
- `Crear secuencias de zona` — las **9** `TaskSequenceSO` (3 personales + 3 por vehículo)
- `Usar modelos reales en vez del greybox` — apunta los 2 perfiles a `ebike.fbx` y `scooter.fbx`
- `Cablear los vehículos en la escena abierta` — perfiles y `VehicleStage` sobre la escena viva
- `Etiquetar entidades en la escena abierta` — pone las entidades sobre los objetos de arte,
  crea las que falten y borra las huérfanas. **Es la vía para cambiar un modelo, añadir un EPP
  o recibir un garaje nuevo sin regenerar la escena.**
- `Montar escena jugable con el garaje nuevo` — **PELIGRO: regenera, no edita.** Ver §9
- `Quitar Batching Static a los objetos que se inspeccionan (Garaje_v5)` — idempotente; deja los
  demás flags (GI, oclusión) y guarda la escena. Correrlo si llega un garaje nuevo (§9)

`Assets/_Project/Tools/Editor/HeadlessTestRunner.cs` corre los tests sin abrir la ventana;
deja el resultado en `Temp/ridesafe-tests.txt`.

---

## 6. Contenido

### Catálogo de EPP — 13 elementos en 4 zonas

| Zona | Elemento | Categoría | Modelo |
|---|---|---|---|
| head | Casco apto | Núcleo | `df_g_helmet_01.fbx` |
| head | Casco no apto | Inapropiado | `casco 1.fbx` |
| head | Gafas | Opcional | `gafas.fbx` |
| clothing | Calzado | Núcleo | `botas.fbx` |
| clothing | Chaleco reflectivo | Según condición | `chaleco refectivo.fbx` |
| clothing | Ropa o cordones sueltos | Inapropiado | `bufanda 01.fbx` |
| trousers | Pantalón ajustado | Núcleo | `jean_ajustado_doblado 1.fbx` |
| trousers | Pantalón ancho | Inapropiado | `jean ancho doblado.fbx` |
| trousers | Pantalón deportivo | Opcional | `pantalon deportivo doblado.fbx` |
| clothing | Coderas | Opcional | `codera.fbx` (par) |
| clothing | Rodilleras | Opcional | `rodillera.fbx` (par) |
| load | Carga asegurada | Núcleo | `guaya candado.fbx` |
| load | Objeto en mano | Inapropiado | `MobilePhone_01.fbx` |

**Cuál casco es cuál lo confirmó Charlie el 2026-10-07: el bueno es `df_g_helmet_01`.** Hasta
entonces estaba al revés y el módulo enseñaba lo contrario. La zona `clothing` recuperó su
riesgo el 2026-10-14 con la bufanda; `ZonesWithoutRisk` quedó vacía.

**Sin confirmar:** cuál de los dos cascos se ve dañado. Si está al revés, el aprendiz
aprende lo contrario. Hay un tercero disponible, `casco.fbx`.

Vehículo: 5 puntos (`point_steering`, `point_brakes`, `point_electrics`, `point_wheels`,
`point_lights`) en 3 zonas. **No están sobre el `ebike`**, aunque este doc lo dijera: son
esferas de 0.15 en fila en `y=1.30, z=0.80`, bajo `Module01_Spawned`, sin tocar ningún
vehículo. Los items no tienen `DisplayPrefab`. Arreglarlo es el tramo 2 de §10.

**Condición de la escena: día claro.** Por eso el chaleco es "aceptable pero no exigido".
Cambiarla a lluvia es la variable de la transferencia de fase 2.

### Localización

Categoría `Module1/` en `Assets/Resources/I2Languages.asset`. **Las 38 claves que pide el
código están las 38, en inglés y español, sin celdas vacías** (verificado 2026-10-07 contra
el asset). Lo que este doc decía de "~25 términos sin texto" ya no aplica.

La hoja de Charlie (`I2Loc RideSafe Localization`, Google Sheets) va **por detrás** del asset:
su pestaña `Module1` tiene 2 claves (`M01_Welcome`, `M01_Video`) frente a las 38 del asset.
Si alguien reimporta desde la hoja, borra 38 términos ya traducidos. Volcado en
`Docs/Module1_terminos_I2.tsv` para alinearla. Ojo: el idioma del asset se llama `Español`,
no `Español [es-CO]` como la hoja.

La pestaña `Menu` de la hoja (39 claves) está casi toda muerta: el código solo usa 3, desde
`MenuRuntimeTextLocalizer`. Las otras 36 duplican la pestaña `M0`, que sí cuadra con el código.

### Multimedia pendiente

Los dos videos de revisión y las líneas de voz del módulo 1. Los hace Charlie.

---

## 7. El paquete de arte

`garage v5.unitypackage` (1.7 GB) importado el 2026-10-05. Los GUID coincidían, así que
Unity escribió **sobre** `_Project/Models/CITY` sin crear carpeta duplicada.

Llegaron: el interior del garaje, casa y fachadas, **los dos vehículos** (`ebike`,
`scooter`), y los EPP (casco, gafas, chaleco, botas, guante, candado, guaya, luz delantera
y trasera, teléfono). La escena de arte `CITY/scenas/Garage.unity` trae todo montado con
12 Area Lights, probes y volumen de post.

`RideSafe_Garaje_v5` = la escena de lógica más ese mundo trasplantado.
**370.038 triángulos**, frente a los 648.614 de antes. El presupuesto de Quest es 150–350k.

---

## 8. Pendientes

### Para que la demo se vea

Todo lo de esta lista quedó hecho y afirmado por el test el 2026-10-06. Lo que sigue abierto:

- [x] ~~**El scooter**~~ — **DESCONGELADO y hecho el 2026-10-12**, porque la reunión pidió en su
      §4 justo lo que era el tramo 2: punto único de exhibición, el vehículo girando por zona y
      los 5 puntos colgando de la malla. Los dos vehículos comparten el punto del centro del
      garaje y `VehicleStage` recuelga los puntos del que esté encendido.
      **Ojo:** sus 3 `TaskSequenceSO` existen y filtran bien por `vehicle == escooter`, pero
      —igual que las de la e-bike— **siguen sin pasos y nadie las ejecuta**: el módulo lo mueve
      `Module01Director`, no `TaskSequence`. Eso sigue pendiente para los dos vehículos.
- [x] ~~Volver a encender `LogoPlate` y `Tagline`~~ — **descartado el 2026-10-07.** El logo vive
      en la puerta del garaje y Charlie no piensa volver a encenderlos. Siguen apagados como
      override de la instancia en la escena.

### Contenido

- [ ] **Dónde se enseña el `_Risk` en el reporte.** Los 18 textos del «por qué» están en I2 y
      **no los lee ningún código**: `SafetyItemSO.RiskKey` solo existe como propiedad. Charlie
      aprobó que salgan **solo en los elementos que estuvieron mal** —en los aciertos no enseñan
      nada y alargan una pantalla que ya iba justa—, pero falta montarlo. Ojo: **no** van por
      voz; locutarlos sesga la elección (bitácora 2026-10-16).
- [ ] **Decidir si los tramos de nota son 4 o 3.** El código parte en `>=75` Conocedor, `>=50`
      Por mejorar, `>=25` Recapacitar y el resto Inexperto, y solo el primero desbloquea
      «Continuar al Módulo 2». Charlie lo describió como tres casos (aprobó / 75-50 / <50). Si
      son tres, es una línea en `SelectionScore.RankOf` y un término que sobra.

- [ ] **Los 5 términos de I2 que faltan: los 3 pantalones y los 2 pares.** Hoy el panel enseña
      la clave cruda (`Module1/Item_TrousersFitted`). Son `Module1/Item_TrousersFitted`,
      `_TrousersLoose`, `_TrousersTech`, **`_ElbowPads`** y **`_KneePads`**, cada una con su
      `_Risk`, en inglés y español. Los pone Charlie en el Drive; el test de PlayMode avisa
      cuando estén para vaciar `Module01SliceTests.SinTraducirTodavia`.
- [ ] **Decidir la categoría de coderas y rodilleras.** Entraron como **Opcional** —se
      recomiendan y no se exigen, igual que las gafas—, y eso es una suposición mía, no una
      decisión de Charlie. Cambiarlas es una línea en `Module01Greybox.Items` más repetir el
      comando. Si fueran de núcleo, omitirlas pasaría a ser omisión crítica y cambiaría el
      veredicto del reporte.
- [ ] **El título de la zona nueva de pantalones en I2.** La estación propia existe desde el
      2026-10-15 y su clave es `Module1/Zone_Trousers`, **sin texto todavía**: en pantalla se ve
      la clave cruda. Hacen falta las dos cadenas, inglés y español. Las pone Charlie.
- [ ] **Pedirle a Daniel las piezas que faltan de la e-bike**: maneta de freno, batería y faro.
      Cada una que llegue es una fila más en `Module01VehicleStaging.Parts`, sin tocar código, y
      un `!` menos. ~~El patinete despiezado~~ **llegó el 2026-10-14**, y con él la maneta de
      freno que la bici no tiene. Del patinete faltaría el faro; la batería va dentro de la
      plataforma y seguramente no tiene sentido sacarla.
- [x] ~~**La estación propia de los pantalones**~~ — **hecha el 2026-10-15**, en el banco
      largo y vacío del fondo derecho (`mesa`, 3.72 m), por decisión de Charlie. Lo que sigue
      abajo es el estudio de la opción de las estanterías, que queda descartada.
      ~~Charlie los quiere **delante de las dos estanterías** de la pared izquierda (`df_g_shelf_01 (2)` y `(3)`, de z=−2.01 a 1.84).
      Medido: ahí **no caben dentro de la zona `clothing`** —desde su ancla, a 3,31 m de la
      pared, el encuadre cubre de z=2.01 a 8.81, y el test falla con `trousers_fitted` en
      viewport x=−0.205—. Hace falta zona nueva: una fila en `Module01Stations`, un `ZoneSO`
      con los tres pantalones sacados de `clothing`, el ancla sale sola, y el recorrido de
      PlayMode pasa de 6 zonas a 7. **Bloqueado por el título de zona en I2 (ES/EN), que lo pone
      Charlie.** Consecuencia de diseño: 4 estaciones personales en vez de 3 y una sección más
      en el reporte. La alternativa sin término nuevo es mover la estación de ropa **entera** a
      las estanterías, que es una sola línea.
- [ ] **`tapabocas` tapa la estación de cascos** y el despeje no encontró hueco libre donde
      apartarlo: avisa y lo deja. Hay que moverlo a mano en la escena.
- [ ] **Pantalón reflectivo o corta-viento**, que pidió Charlie. No existe modelo. Entrará como
      «según condición», igual que el chaleco. **No bloquea esta build**: `jean ajustado` ya es
      el pantalón correcto (núcleo) y la zona ya enseña algo sin él.
- [x] ~~**Pushear `feat/garaje-v6`**~~ — **ya está subida.** Comprobado el 2026-10-14 contra
      `git ls-remote`: el remoto tiene la misma punta que el local. Lo de «3 commits sin subir»
      llevaba tiempo siendo falso. Y **no hay nada que traer de `dev`**: su punta (`718a6e9`) es
      ancestro de esta rama, igual que `main` y `feat/garaje-v5`.
- [x] ~~**Decidir qué hacer con `Feature/tutorial`**~~ — **integrado el 2026-10-14.** De sus 186
      ficheros, 149 caían en `Assets/CITY` con los mismos GUID que los nuestros; comparados uno a
      uno, 126 eran idénticos, 0 nuevos y solo 23 traían contenido distinto. Se copiaron los 16
      que importaban sobre su pareja en `_Project` y se borró `Assets/CITY`. Detalle en §9 y en
      el mensaje del merge.
- [ ] Alinear la hoja de I2 con el asset (la hoja va detrás; ver §6)
- [ ] Voz del módulo 1 y los dos videos. **Bloqueados a propósito:** Charlie quiere dejar
      primero los textos bien en el Drive, y sacar de ahí locución y guion de video.
- [x] ~~Confirmar cuál casco es el dañado~~ — **resuelto el 2026-10-07: el bueno es
      `df_g_helmet_01`.** Estaba al revés, así que `helmet_ok` y `helmet_damaged` intercambiaron
      modelo y etiqueta de escena. Hasta hoy el módulo enseñaba lo contrario.
- [x] ~~Modelo de ropa suelta~~ — **apagado el 2026-10-07.** `clothing_loose` fuera del catálogo;
      su `SafetyItemSO` sigue en disco para reactivarlo. **Consecuencia: la zona `clothing` se
      queda sin ninguna respuesta equivocada**, así que no enseña nada: el aprendiz acepta las
      dos cosas y acierta. La excepción está declarada en `Module01DataTests.ZonesWithoutRisk`
      con su motivo, para que no se olvide.
- [ ] Decidir si entran `luz grontal` y `luz roja 1`. **No se puede consultar el GDD: los dos PDF
      no están en el repo.** Hoy las luces no son un EPP recogible, son el punto de inspección
      `point_lights` ("Luces y reflectivos") de la zona `visibility`. Esos dos modelos son los
      candidatos naturales para colgarlos del vehículo y que ese punto apunte a algo real — que
      era trabajo del tramo 2, ahora congelado.

### Técnico

> La lista viva, con números y nombres, está en `Docs/Pendientes_Reunion_2026-10-12.md`.
> Aquí solo queda lo que sigue abierto y lo que se cerró, para no buscarlo dos veces.

- [x] ~~**El HUD en la capa `Default`**~~ — **resuelto el 2026-10-12.** No se movió
      `BuildUiCamera` —que es lo que disparaba la regresión de la fuente del `BeginButton`—
      sino que se añadió una pasada de capas al final, que era lo único que faltaba.
- [x] ~~**Un casco no se dibuja en Game y sí en Scene**~~ — ya no ocurre: `df_g_helmet_01` sale
      correctamente en las capturas de PlayMode del garaje v6.
- [x] ~~**Los objetos no se apoyan en la mesa**~~ — **resuelto el 2026-10-12.** `ArrangeStations`
      dejó de reparentar, colocar y escalar: ahora solo coloca el punto de vista, derivado de
      donde el arte dejó los objetos. Y el collider lleva un grosor mínimo de 6 cm, que es lo
      que le faltaba a la guaya y al teléfono.
- [x] ~~Escena del vehículo giratorio~~ — **hecha el 2026-10-12.** Existe `anchor_vehicle`, los
      5 puntos cuelgan de mounts en la malla y llevan marcador `!`. Los sitios son aproximados
      y necesitan ojo humano; ver el documento de pendientes.
- [x] ~~**Partir `Module01SceneBuilder` en comandos no destructivos**~~ — hecho: `Module01WorldSwap`
      cambia el mundo sobre la escena abierta, `Module01EppLayout` agrupa el mostrador y
      `Module01VehicleStaging` monta el vehículo. El destructivo ya no hace falta.
- [x] ~~Fondo para el título del reporte~~ — hecho el 2026-10-12: el título y el logo van sobre
      una barra oscura. Los subtítulos siguen sin bajar.
- [x] ~~Panel Vehicle del módulo 0: la barra de confirmación se pisa consigo misma~~ —
      **resuelto el 2026-10-16**: dos líneas a la izquierda de los botones, y las tarjetas se
      ajustan al texto. El solape de título y subtítulo no apareció en las capturas de la v5.
- [ ] **Correr los 6 tests de PlayMode del M1** tras los cambios del 2026-10-16. El puente se
      cuelga con PlayMode, así que se corren desde el Test Runner. Recorren justo las pantallas
      que cambiaron.
- [ ] **Una sola versión de Unity para el equipo.** El horneado de la v5 hecho en 6000.5.4f1
      **no carga** en 6000.4.9f1 y la escena sale negra (§9). Se rehízo en 6000.4.9f1 el
      2026-10-16 (4 lightmaps, 3 sondas); si alguien vuelve a hornear en la otra versión, se
      repite el problema. Hasta decidirlo, el horneado no se commitea.
- [x] ~~`df_g_helmet_01` no tiene collider~~ — **falso, corregido el 2026-10-16**: el collider
      de su raíz está desactivado, pero tiene otro activo en un hijo y el rayo lo alcanza. La
      sonda solo miraba el primero (§9). Con la búsqueda por física el tutorial ya lo cuenta.
- [ ] **La puerta del garaje no tiene collider.** Por eso el EPP de dentro es alcanzable por el
      rayo desde fuera. El tutorial lo esquiva contando solo objetos 3D a menos de 4 m, pero
      el rayo de AutoHand sí llega.
- [ ] **Medir en Quest el coste del tutorial.** Ya no recorre la escena: busca objetos con la
      física a menos de 4 m y botones con la lista de uGUI (`198bb1e`). Falta confirmarlo con el
      Profiler en el visor. Pendientes menores en la revisión final (bitácora 2026-10-15/16).
- [ ] **Decidir con el equipo lo que la auditoría del 2026-10-16 dejó abierto:**
      - **Sacar `RideSafe_Garaje.unity` de la lista de build:** hoy entra en la app con todo el
        tutorial viejo, y `Module01SceneBuilder` la usa de origen. Es el primer paso para
        retirar `Systems/Tutorial`, `Demo/Tutorial`, `Garaje_Test` y sus tests.
      - **Arte sin uso de `CITY`:** unos 176 MB, más el HDRI `furstenstein_4k.exr` de 81 MB.
        Es de Daniel.
      - **Sistemas sin uso en producción:** `CommandSystem`, `SoundSystem`, `Brand` y parte de
        `SharedUtils`. Preguntar si se reutilizan en otros proyectos.
- [ ] **Retirar el tutorial viejo** (`Systems/Tutorial` + `Garaje_Test`) cuando ya no haga falta
      como referencia.
- [x] ~~Integrar `feat/garaje-v6` en `dev`~~ — **hecho el 2026-10-14.** Fue avance directo:
      `dev` no se había movido. **Queda avisar a Cindy.**
- [x] ~~Rehornear la luz~~ — **horneado de verdad el 2026-10-14, y era el fallo que Charlie
      veía.** La escena tenía su `LightingData` **prestado de la escena de arte**, y tras mover
      estaciones y vehículo ese préstamo dejó de corresponder: manchas duras de luz en el suelo
      y paredes apagadas. Ahora tiene el suyo en
      `Assets/_Project/Scene/RideSafe_Garaje_v5/` (2 lightmaps, 3 sondas, 64 MB, 378 de 436
      renderers). **Esto hay que repetirlo cada vez que se mueva el mundo o las estaciones.**
- [ ] **El occlusion sigue pendiente, y ahora también el de la escena de arte.** El de las tres
      escenas jugables es el de siempre, intacto. El de `CITY/scenas/Garage.unity` se perdió al
      integrar: Unity le reasignó el GUID al copiarlo y quedó huérfano, así que se borró. Se
      rehornea desde esa escena cuando haga falta; no está en la lista de build.
- [ ] **Reponer colliders: ni los armarios ni los maniquíes del v6 tienen ninguno.** Además de
      lo obvio, esto hace que toda comprobación de oclusión por raycast mienta (ver §9)
- [ ] **459.652 triángulos visibles** tras el cambio de mundo al v8 —bajan desde los 670.903 del
      v6—, pero siguen por encima del presupuesto de Quest (150–350k)
- [x] ~~**El orden de la lista de build**~~ — corregido el 2026-10-12: la escena del v6 ya es la
      primera. Antes un build habría arrancado en `RideSafe_Garaje.unity`, la vieja. Es un
      cambio en `ProjectSettings`: mirarlo al integrar con Cindy
- [x] ~~Esferas blancas en el pedestal~~ — resuelto el 2026-10-14: enseña la pieza real o el `!`.
- [ ] Unificar `MenuRuntimeTextLocalizer` con `UIText`
- [ ] Autorar los pasos de las 9 `TaskSequenceSO` cuando existan los términos; hoy ninguna
      tiene pasos y nada las ejecuta
- [ ] Ningún nivel de calidad asigna RP asset; `Mobile_RPAsset` sin usar

---

## 9. Trampas conocidas

**Añadidas el 2026-10-16 (noche, 2):**

- **Un objeto que se inspecciona no puede ser *Batching Static*.** En Play, Unity funde su malla
  con la de la escena: el `MeshFilter` pasa a apuntar a `Combined Mesh (root: scene)`, con los
  vértices ya en el mundo. Todo lo que copie o reutilice esa malla (el pedestal, un contorno, un
  halo) dibuja otra cosa o nada. Así salió vacío el calzado cerrado. Lo vigila
  `En_la_v5_ningun_objeto_que_se_inspecciona_se_funde_con_el_static_batching`. El arte puede
  traerlo marcado en cada garaje nuevo.
- **`VehicleStage.MoveToPart` mueve el punto después de `InspectionHighlight.Bind`.** Lo que
  cuelgue del punto se mueve con él. El área de clic de la pieza acabó 0,83 m al lado de la
  rueda hasta que se recolocó sobre la pieza cada vez que se enciende.
- **El puente del editor a veces ejecuta un clic dos veces**, así que recorrer zonas con
  `NextZoneButton` puede saltarse una o llegar hasta «Mi Elección». Para comprobar un caso
  concreto rinde más un método temporal que ponga el estado directamente.

**Añadidas el 2026-10-16 (noche):**

- **Antes de borrar un asset «sin uso», buscar su GUID también en los `.meta`.** Un FBX guarda
  en su `.meta` (`externalObjects`) los materiales que se le reasignaron. Ninguna escena ni
  prefab los nombra, pero sin ellos el modelo sale magenta. Pasó con `OculusControls/Mats`: el
  Touch Plus del tutorial salió sin material y se restauraron desde git.
- **`ServiceLocator` ya no escribe en la consola.** Sus logs pasan por un método
  `[Conditional("RIDESAFE_SERVICE_LOG")]`. Para volver a verlos, añadir ese símbolo en Player
  Settings → Scripting Define Symbols, y solo para depurar: en Quest, un log por petición era un
  tirón visible.
- **Para saber si un objeto tiene collider, no basta `GetComponentInChildren<Collider>()`.**
  Devuelve el primero aunque esté desactivado. `df_g_helmet_01` tiene el de la raíz apagado y
  otro activo en un hijo, y así lo di por no clicable sin serlo.

**Añadidas el 2026-10-16 (tarde):**

- **El puente de MCP rechaza arrancar el Test Framework.** Contesta *«User interactions are not
  supported for MCP tool calls»*, y también si la llamada se aplaza con
  `EditorApplication.delayCall`. Comprobado: `SaveOpenScenes` y `SaveAssets` pasan sin problema,
  así que lo que molesta es `TestRunnerApi.Execute`. La salida es `TestLaunchWatcher`: se deja
  una señal en `Temp/ridesafe-run-tests.txt` (modo en la primera línea, una assembly por línea
  debajo) y el Editor arranca los tests en su propio ciclo, ya fuera de la vigilancia del
  puente. El resultado sigue yendo a `Temp/ridesafe-tests.txt`.
- **Tras una corrida de PlayMode el Editor se queda en la escena del Test Runner**
  (`Assets/InitTestScene<guid>.unity`, una sola raíz). Cualquier medición contra «la escena
  abierta» devuelve cero de todo y parece que el cableado se ha perdido. **Reabrir la v5 antes
  de medir.**
- **Editar un `.cs` mientras corre PlayMode aborta la corrida en silencio.** El recompilado se
  lleva el callback, el fichero de resultado se queda en `RUNNING` para siempre y el Editor
  acaba sin estar en Play. No es un cuelgue: hay que relanzar.
- **Un `ContentSizeFitter` en un panel que no se ve guarda 0 en la escena.** En el editor el
  grupo de layout no mide un panel oculto, el ajuste calcula 0 y eso se serializa como
  sobrescritura de instancia: el texto sale como una columna de letras, el prefab parece
  correcto y **el fallo vuelve cada vez que alguien guarda la escena**. Para una caja de texto
  fija, un número y un test que lo guarde salen más baratos.
- **`LayoutElement` no «sube el mínimo» del ajuste: lo sustituye.** `LayoutUtility` no se queda
  con el mayor de los valores sino con el de **mayor prioridad**, y `LayoutElement` tiene
  `layoutPriority` 1 frente al 0 del grupo de layout. Poner `preferredHeight` para dar un suelo
  clava la caja en ese número y el texto largo vuelve a salirse.
- **La herramienta de audio nombra el fichero con el término SIN categoría.** Hace
  `term.Replace("<categoria>/", "")` y escribe en
  `Resources/Sounds/<categoria>/<idioma>/<nombre>.mp3`, que es justo donde lee `NarrationPlayer`.
  Por eso la categoría de I2 decide la carpeta: un término sin `/` cae en `Default` y el audio
  acabaría en `Sounds/Default/`, mezclado con el del menú.
- **La clave de API de ElevenLabs está escrita en el código** (`ElevenLabsAPIWindow.cs` y
  `GenerateAudio.cs`). Charlie lo da por bueno el 2026-10-16: el repo es solo de él y Cindy, y al
  cliente se le entrega el build, no el proyecto. Si eso cambia, rotarla.

**Añadidas el 2026-10-16 (mañana):**

- **Un horneado de 6000.5.x no carga en 6000.4.9f1 y la escena sale negra.** El log dice
  `Failed to load '…/LightingData.asset'. File may be corrupted or was serialized with a newer
  version of Unity`. Las texturas sí cargan, pero sin `LightingData` los renderers no saben qué
  parte del lightmap usar. La versión va en la cabecera del archivo:
  `head -c 300 LightingData.asset | tr -c '[:print:]' ' '`. Mirar eso antes de culpar a la luz.
- **Una sobrescritura de instancia vieja puede aplastar un panel.** La v5 guardaba `m_SizeDelta
  0×0` en la nota de «Mi Elección», resto de cuando la medía un `ContentSizeFitter`. Al quitar el
  fitter en el prefab, ese 0 ganó y el texto salió como una columna de letras. El prefab estaba
  bien y el test del prefab pasaba; el fallo solo se ve en la escena. Buscar
  `propertyPath: m_SizeDelta` en las modificaciones de la instancia.
- **Un collider en la capa UI no recibe clics.** El `PhysicsRaycaster` de la cámara principal la
  excluye. Por eso el `!` (en capa UI para que lo dibuje la cámara de overlay) lleva su zona de
  clic en un hijo en capa Default.
- **Un MonoBehaviour de un ensamblado de tests de editor no se puede `AddComponent`**: Unity dice
  *«is an editor script»* y devuelve null. En tests, usar un componente de runtime real
  (p. ej. `EventTrigger` como receptor de clic).
- **`Collider.bounds` no se actualiza al mover el objeto** hasta que la física sincroniza
  (`autoSyncTransforms` está apagado). Medido: un cubo movido a z=3 seguía con bounds en (0,0,0).
  Para «dónde está», usar `collider.transform.position`.
- **Un `RectTransform` nuevo bajo un `Transform` normal guarda `m_AnchoredPosition`, no
  `localPosition`.** Asignar solo `localPosition` funciona en memoria y se pierde al guardar.
  Asignar los dos.
- **Un namespace no puede llamarse como una clase pública que se usa desde `RideSafe.*`.** Con
  `RideSafe.TutorialHelp.TutorialHelp`, una llamada `TutorialHelp.Stop()` desde un módulo
  resolvería al namespace. Por eso el tutorial está en `RideSafe.Help`.
- **El puente del editor a veces ejecuta un comando dos veces.** Pasó con clics de la sonda en
  el M1, que avanzaron el recorrido solo hasta el final. Si un clic responde «No active object»
  justo después de funcionar, es la repetición.

**Anteriores:**

- **`UnityEngine.EntityId` choca con el de TaskSequence** en Unity 6. Todo archivo que
  use `EntityId` necesita `using EntityId = RideSafe.TaskSequence.EntityId;`.
- **Los asmdef de tests necesitan las 3 DLL de Sirenix** (`Serialization`, `Utilities`,
  `OdinInspector.Attributes`), porque `TaskSequenceSO` hereda de `SerializedScriptableObject`.
- **Compilar y correr tests en la misma llamada no funciona**: la compilación es asíncrona
  y los tests corren contra las assemblies viejas. Compilar en una llamada, correr en la siguiente.
- **Los callbacks del `TestRunnerApi` no sobreviven a PlayMode**: la recarga de dominio al
  salir se los lleva. El test tiene que escribir su propio resultado a disco.
- **`TaskEntity.OnValidate` avisa "no EntityId set"** al añadir el componente por código,
  antes de `Configure`. Es ruido esperado, no un fallo.
- **`UnityYAMLMerge` puede producir un prefab corrupto** saliendo con éxito y sin
  conflictos. Validar importando en Unity, no solo mirando el YAML.
- **Una zona sin entidad para alguno de sus elementos nunca cierra** y cuelga el módulo.
  El montador de escena lo completa y lo verifica.
- **`UnityEvent<T>` genérico no se serializa**: `ChoicePanel.onContinue` y
  `ComfortSettingsPanel.onStart` hay que cablearlos por código.
- **`CursorManager` esconde el cursor al arrancar**; por eso existe `DesktopCursor`.
- **El proyecto es solo Input System nuevo**; el modo escritorio de AutoHand usa la API vieja.
- **Errores de Odin en consola** (`MissingFieldException: UIElementsUtility`): son del
  Editor dibujando inspectores, no del runtime.
- **Dos copias de un root de UI se ven como un botón muerto.** La escena v5 traía dos
  `PF_Module00_UI` solapados pixel a pixel; `Show()` apagaba el Welcome de uno y el gemelo
  seguía encendido. `Module01PresentationBuilder.RequireUnique` ahora revienta en vez de
  elegir una en silencio.
- **Asignar una propiedad directa sobre un componente que vive en un prefab no persiste.**
  `VideoSurface.texture = rt` se perdía al recargar la escena porque no registraba el
  override. Va por `SerializedObject` + `ApplyModifiedProperties`.
- **`VideoPlayer.Play()` sin preparar se queda en `prepared=true, playing=false`** y la
  pantalla sale en negro. Hay que `Prepare()` y arrancar en `prepareCompleted`. Además
  `waitForFirstFrame` en `false`, porque si no espera a que alguien dibuje la Game View.
- **Un disco 3D se come la UI que tiene debajo, aunque las alturas no se crucen.** El pedestal
  del inspector es un cilindro: su borde cercano se adelanta media anchura hacia la cámara, así
  que en pantalla cuelga mucho más abajo que su centro, y al estar más cerca gana el z-buffer.
  Comparar `y` en el mundo **no lo detecta**: hay que proyectar las 8 esquinas de la caja. El
  popup ya no cuelga de una altura escrita a mano, se deriva de ese borde proyectado.
- **El kit de UI vive en `Assembly-CSharp-Editor` y no se puede meter en un asmdef**, porque
  `UIModules` es una clase parcial y una de sus partes usa `I2.Loc`. Un builder que necesite el
  kit tiene que vivir fuera de todo asmdef (p.ej. `Module01/Scene/Editor/`), no reinventar los
  botones. `UIElements` choca con el namespace `UnityEngine.UIElements`: hace falta alias.
- **`ItemInspector.Translate` no tenía caída a la clave** (al revés que `UIText.Resolve`): un
  término borrado del I2 salía como texto **en blanco**, no como clave visible. Arreglado. Y
  `??` no atrapa la cadena vacía, así que `SetQuestion("")` dejaba la pregunta muda.
- **Un canvas de world space lo atraviesa cualquier malla más cercana.** Se ordenan por
  profundidad junto con la geometría. La capa `UI` la dibuja una **cámara de overlay** en el
  stack de URP (`Module01PresentationBuilder.BuildUiCamera`), que comparte transform con la
  principal. No usar `ScreenSpaceOverlay`: no funciona en VR. El canvas del inspector se queda
  fuera **a propósito**, porque su popup va detrás del modelo que gira sobre el pedestal.
- **Medir los rects en unidades de canvas, nunca en píxeles de pantalla.** Leyendo píxeles
  concluí que un `sizeDelta` no se aplicaba —se aplicaba— y reverté un arreglo correcto. Un
  volcado de `rect.size`, `offsetMin` y `offsetMax` lo resuelve en un minuto. Ojo con las
  plantillas inactivas: no las toca el layout group, así que conservan su 100×100 y una
  etiqueta estirada con offsets grandes sale con **ancho negativo**.
- **El español es más largo que el inglés con el que se diseñó el kit.** Cada panel de ancho
  fijo es un candidato a que el texto se pise. Ya pasó en la lista lateral y sigue pasando en
  el panel Vehicle del módulo 0.
- **Una captura que solo comprueba que el archivo existe miente.** Si el borrado falla, el
  assert pasa sobre la imagen de la corrida anterior y se revisa evidencia caducada. Exigir
  marca de escritura nueva.
- **`VideoPlayer.prepareCompleted` no se vuelve a emitir si el reproductor ya estaba
  preparado.** Arrancar el video desde ese evento deja `prepared=true, playing=false` esperando
  un aviso que no llega, y la pantalla en negro. Es la otra cara de la trampa de arriba: no
  basta con preparar antes de `Play()`, hay que **sondear `isPrepared`** en vez de colgarse del
  evento. Tumbó los dos tests de recorrido completo y no dejó ni una excepción en el log.
- **`Montar escena jugable` no edita v5: la regenera y borra lo que no sepa rehacer.**
  Abre siempre `RideSafe_Garaje`, le trasplanta el mundo del paquete de arte y guarda **encima**
  de `RideSafe_Garaje_v5`. Todo lo que se añadió a v5 después se pierde: el 2026-10-07 se llevó
  28 GameObjects, entre ellos el `Module01Experience` que pone `Module01PresentationBuilder`, y
  dejó la escena con el `XRPlayer` encendido y dos cámaras de juego. El doc decía «idempotente»,
  y lo es solo en el sentido de que siempre produce lo mismo desde la escena de lógica. Para
  cambios acotados, un comando propio sobre la escena abierta (como
  `Cablear los vehículos en la escena abierta`); si hay que regenerar, volver a montar la
  presentación después.
- **Unity sigue con la escena vieja en memoria tras un `git checkout`.** Restaurar el archivo no
  basta: los tests de PlayMode siguieron fallando contra la copia destruida hasta la siguiente
  recarga de dominio. Recompilar antes de creerse el resultado.
- **Importar la pestaña `Module1` a I2 duplica el prefijo de categoría.** I2 trata `Module1/`
  como categoría, así que si las claves de la hoja ya la traen, la vuelve a añadir y quedan
  como `Module1/Module1/Orientation_Title`. El juego no encuentra ninguna y se ve la clave
  cruda en pantalla. Pasó el 2026-10-07 con las 61. **Al importar, la columna de claves debe
  ir sin el `Module1/`**, o hay que quitarlo después. Verificar siempre con un volcado:
  `src.mTerms.Count(t => t.Term.StartsWith("Module1/Module1/"))` tiene que ser 0.
- **Traducir la hoja entera con el traductor automático destroza el inglés.** El inglés es el
  **origen**; el español salió de él. En la misma importación volvieron 36 cadenas inglesas
  retraducidas desde el español: `Yes → "Yeah"`, `Prepare before riding → "Prepare yourself
  before filming"` (por «rodar»), `Secured load → "Cargo insurance"`, `Core item omitted →
  "Omitted nucleus"`. **Traducir solo la columna que falta, nunca la de origen.**
- **El Editor se queda en modo Play tras una corrida de PlayMode que falla.** Y entonces la
  tarea previa del Test Framework (`SaveModifiedSceneTask`) revienta con *"This cannot be used
  during play mode"* y tiñe de rojo un test que no tiene nada que ver — el primero o el último,
  según cuándo se emita. **Antes de lanzar PlayMode, comprobar `EditorApplication.isPlaying` y
  salir si está en true.** Esto explica todos los "fallos intermitentes" del 2026-10-07.
- **`HeadlessTestRunner.Run(null)` mete la assembly de PlayMode en modo EditMode** y revienta con
  `InvalidOperationException: This cannot be used during play mode`. Hay que nombrar las
  assemblies: `RideSafe.Module01.Tests`, `TaskSequence.Tests`, `Tutorial.Tests`.
- **Un atajo de test que apaga medio mundo esconde bugs.** El viejo `StartWithDefaults`
  desactivaba todos los `ModuleUI`, y por eso ningún test vio la escena duplicada durante
  semanas. El test entra pulsando Begin.
- **Un diálogo modal de Unity bloquea el relay de MCP para siempre.** Con la escena sin guardar,
  el Test Framework abre «Scene(s) Have Been Modified» **antes** de empezar, y también al salir
  de PlayMode. Delante de una persona es una pregunta; desde un script es un cuelgue: la ventana
  principal queda deshabilitada y no responde ni un ping. El log no dice nada —acaba en
  «Scanning for USB devices»— así que parece un crash y no lo es. Se detecta enumerando ventanas
  (`EnumWindows`, clase `#32770`) y se despeja mandando `BM_CLICK` al botón `Save`.
  `HeadlessTestRunner` ya guarda las escenas antes de lanzar, pero el de la **salida** sigue
  pudiendo aparecer. Costó media hora la primera vez.
- **`GetComponent<T>() ?? AddComponent<T>()` no funciona.** En el Editor, `GetComponent` de un
  componente ausente devuelve un objeto de diagnóstico que solo es null para el `==`
  sobrecargado de Unity; para C#, y por tanto para `??`, es un objeto válido, así que
  `AddComponent` nunca se llama y la primera línea que toque el componente revienta con
  `MissingComponentException`. **Solo falla cuando el componente falta de verdad**, que es el
  caso que la expresión pretendía cubrir: por eso aguantó ocho veces sin dar la cara. Usar
  `Module01EditorComponents.Ensure<T>`.
- **Nada del paquete de arte tiene collider: comprobar oclusión con un raycast miente.** Ni los
  armarios del mostrador ni los maniquíes. Un rayo del ancla a cada uno de los 7 EPP devolvía
  «despejado» mientras un maniquí tapaba las gafas y el calzado. **Eso solo se ve en una
  captura.** Y por lo mismo, «está en pantalla» no implica «se ve»: el popup de confirmación
  estaba dentro del encuadre (y de 0.03 a 0.26) y enterrado en la madera del armario.
- **Comparar rects del kit sin descontar el padding da falsos solapes.** `Panel()` infla el rect
  por el padding transparente del atlas para que el **arte visible** mida lo pedido. Dos paneles
  cuyos rects se cruzan por 3 unidades pueden estar separados por 3 en lo que se ve. Descontar
  `PadFor(multiplier)` antes de concluir nada.
- **Borrar los `mount_*` del vehículo borra los puntos de inspección**, que cuelgan de ellos.
  Si hay que rehacerlos, después toca `Module01SceneEntities.Apply()`, que recrea las entidades
  que falten.
- **El constructor estático de un `ScriptableObject` no puede llamar a
  `ScriptableObject.CreateInstance`.** `HeadlessTestRunner` lo hacía en su `[InitializeOnLoad]`
  y reventaba con `TypeInitializationException`, dejando el runner inservible justo cuando hacía
  falta. Solo pasaba si quedaba un archivo de resultado en `RUNNING` de una corrida anterior.
  Ahora se aplaza con `EditorApplication.delayCall`.
- **No mover la cámara de juego en una medición sin devolverla.** Un volcado que la colocó en un
  ancla para medir dejó la escena guardada con la cámara ahí, y el test siguiente falló con «el
  jugador debe empezar fuera del garaje». `Module01PresentationBuilder.Build()` la devuelve a
  `(0, 1.45, 9.3)`.
- **El montador de prefabs de UI abre una escena nueva.** `RideSafeUIPrefabBuilder.Build(null)`
  cierra la escena abierta; hay que reabrirla y volver a montar la presentación. Y **no** usar
  `BuildMenu()` desde un script: llama a `SaveCurrentModifiedScenesIfUserWantsTo()`, que es el
  diálogo modal de más arriba.
- **Una regresión de arte no siempre está en un `git diff`.** Dos veces seguidas aquí: el suelo
  blanco no era un fichero cambiado sino **cuál de los tres materiales** llevaba puesto el
  objeto, y las bicis invisibles no eran un fichero borrado sino un `MeshFilter` que se quedó
  **sin malla** al despiezarse el FBX. Buscar «qué fichero cambió» no podía dar con ninguna de
  las dos. Lo que sirve: medir en la escena —qué material lleva cada objeto, qué bounds tiene— y
  comparar **contenido**, no commits.
- **`git diff <commit> -- <ruta>` sobre una ruta que no existe responde «sin cambios».** Me hizo
  afirmar que `interior.fbx` era idéntico al del v8 cuando estaba mirando una ruta inventada —le
  faltaba `MODELADOS 3d/`—. Si la comparación va a sostener una conclusión, comprobar antes que
  la ruta existe. Y para ficheros de LFS, `git show <commit>:<ruta>` da el **puntero**, no el
  contenido: sirve para ver si cambió, no para hashear el modelo. Para comparar de verdad contra
  un `.unitypackage`, extraerlo (es un tar.gz con una carpeta por GUID) y hacer `sha1sum`.
- **El suelo del garaje lo entrega el arte en baldosa blanca, y el acabado bueno es otro.** La
  escena de arte pone en `piso` el material `suelo 1` —`Poliigon_TilesCeramicWhite`, tiling 2×2,
  smoothness 0.90—, que pinta el garaje de blanco con juntas de metro y medio. El acabado del
  proyecto es **`SUELO.mat`**: `plaster_grey_04` a 6×6 y smoothness 0.20, hormigón continuo. Lo
  reaplica `Module01WorldSwap.ApplyGarageFinishes()` después de cada cambio de mundo, porque el
  cambio sustituye el objeto `piso` entero y un ajuste a mano se pierde en la siguiente entrega.
  **Costó una tarde atribuirlo**, porque ni la malla ni los materiales habían cambiado en git —lo
  que había cambiado era *cuál de los tres* llevaba puesto el suelo—, así que parecía «han traído
  otro garaje». Hay tres: `SUELO` (hormigón, el bueno), `suelo 1` (baldosa blanca) y `suelo 2`
  (terrazo, crema). Y ojo: el **techo** también lleva `suelo 1`, y ahí sí está bien.
- **Un `LightingData` prestado de otra escena se ve bien hasta que algo se mueve.** La escena
  de trabajo referenciaba el de la escena de arte —y era gratis: 330 y pico renderers con bake
  sin hornear aquí—. Pero el horneado **es por escena**: en cuanto las estaciones y el vehículo
  cambiaron de sitio, el mapa dejó de corresponder y el garaje salió con manchas duras en el
  suelo y las paredes apagadas. **Se diagnostica mal a ojo:** parecía «otro garaje» o «el suelo
  cambió de material», y no era ninguna de las dos —la malla, el material y la textura del suelo
  llevaban sin tocarse desde el 5 de octubre, y la propia escena de arte de Daniel tiene el suelo
  blanco—. Lo que lo zanja es hornear la escena y mirar.
- **El post-proceso estaba apagado en las tres cámaras base.** El `Box Volume` con su perfil
  —Bloom, ColorAdjustments, WhiteBalance— llevaba ahí todo el tiempo, pero sin
  `renderPostProcessing` en la cámara no se aplica nada, así que el garaje salía lavado. Ojo
  también: ese volumen **no es global**, su caja es el interior del garaje (10.0 × 3.6 × 12.1),
  así que fuera no grada — y el aprendiz empieza fuera, a `z = 9.3`, a propósito.
- **Reimportar el `.unitypackage` de arte puede retroceder trabajo.** Reimportar `garage v8` el
  2026-10-14 no trajo **nada** nuevo —`Garage.unity`, `suelo 1.mat` e `interior.fbx` salieron con
  el mismo hash— y en cambio pisó el `scooter.fbx` despiezado con el de malla única del paquete.
  Antes de reimportar, comparar hashes; después, `git status` y restaurar lo que haya retrocedido.
- **Lo que suba Cindy no se mezcla tal cual: sus importaciones caen en `Assets/CITY`.** Su rama
  nunca tuvo la reestructura a `_Project`, así que cuando importa un paquete de arte, parte de
  los ficheros sobrescriben en sitio y el resto aterriza en el layout viejo —**con los mismos
  GUID que los nuestros**—. Un merge directo deja el proyecto con dos copias de cada asset y
  Unity reasignando GUID por su cuenta. La receta, del 2026-10-14: comparar cada fichero de
  `Assets/CITY/` con su pareja en `_Project` (la mayoría son idénticos: 126 de 149), copiar
  encima solo los que difieren **tras comprobar que el `.meta` lleva el mismo GUID**, y borrar
  `Assets/CITY` entero.
- **Un `.meta` copiado a mano no conserva su GUID si el viejo sigue registrado.** Se intentó
  guardar el `OcclusionCullingData` de Cindy al lado del nuestro copiando su `.meta`; Unity vio
  el GUID todavía registrado en el árbol recién borrado y le asignó uno nuevo, dejando el
  fichero huérfano. Para conservar un GUID hay que **mover** el asset (`AssetDatabase.MoveAsset`),
  no copiarlo a un sitio donde ya vive otro con el mismo id.
- **Un horneado se toma entero o no se toma.** `LightingData` y sus lightmaps son un conjunto
  coherente: resolver el conflicto quedándose el `LightingData` de uno y algún lightmap del otro
  da una iluminación que no corresponde a nada. En el merge del 2026-10-14 se tomaron los 15
  ficheros del lado de Cindy. La comprobación barata de que no se ha roto: que el índice de
  lightmap más alto que pide la escena no se salga de los disponibles —era 1 de 4—.
- **Las dos mallas de vehículo están en espejo, y el giro de zona no es compartible.** La
  e-bike trae el morro en la X local **baja** (`manubrio` en u=0.19) y el patinete en la **alta**
  (`control_de_direccion` en u=0.74), así que el giro que presenta una de frente presenta la otra
  de espaldas. Con el signo `!` daba igual —se colocaba a mano en un hueco despejado—; desde que
  el aviso es el contorno de la pieza real, el giro decide si esa pieza se ve. `Yaws` pasó a ser
  por vehículo, con los del patinete **invertidos**: cockpit −90, wheels 180, visibility 90. Y la
  tabla de *mounts* sí se comparte, pero por casualidad: sus fracciones se escribieron con el
  morro en la X alta y en la bici el punto acaba en el borde correcto porque `MoveToPart` usa
  `Bounds.ClosestPoint`.
- **Un test del vehículo que se para en el onboarding no prueba el vehículo.** El del patinete
  afirmaba el perfil elegido y que la bici quedaba apagada, y ahí terminaba: nunca entró en sus
  tres zonas. Mientras fue malla única no faltaba nada por ver; en cuanto llegó despiezado, todo
  lo nuevo —contornos, giros, el pedestal— quedó sin afirmar y los tests seguían en verde.
- **Con un error de compilación, Unity no recarga el dominio y sigue ejecutando el build
  anterior.** Reconstruye los ensamblados que sí compilan, deja el resto, y por el relay no se
  nota: los comandos funcionan pero corren el código viejo. El 2026-10-15 costó media hora y lo
  atribuí mal dos veces. Los síntomas engañan todos a la vez: `isCompiling` en false, el
  `AssetDatabase` **veía** el texto nuevo del fichero, `Assembly.Location` apuntaba al DLL bueno
  y el DLL del disco **sí** tenía el método nuevo. La causa eran 5 `CS0103` en
  `StationLayoutTests.cs` —unos tests metidos en la clase de al lado, que no tenía los
  ayudantes—. **El detector correcto es la consola** (`GetConsoleLogs` con `logTypes: Error`),
  no `Editor.log`. Y la verdad última es el comportamiento: dejar un `Debug.Log` con un texto
  distinguible y ver qué frase sale. Buscar un **literal de cadena** en un DLL con `grep` da
  falso negativo siempre —van en UTF-16—; los nombres de método sí se encuentran.
- **`Editor.log` es de un solo Editor, y puede no ser el nuestro.** Charlie tiene abierto a
  ratos `Alphatecn-NavalConquestV1`, y `AppData/Local/Unity/Editor/Editor.log` es el del Editor
  que arrancó último. Buscar ahí `error CS` y no encontrar nada **no prueba nada**.
- **Con dos Editores abiertos, el relay puede contestar desde el otro proyecto.** Pasó el
  2026-10-15: un comando reventó con un `DirectoryNotFoundException` sobre una ruta de
  `Alphatecn-NavalConquestV1`, y la escena abierta allí era `Login.unity`. No hubo daño porque
  murió al leer, pero una escritura habría ido al proyecto equivocado. **Comprobar
  `Application.dataPath` antes de escribir nada**, y si no es el nuestro, pedir que cierren el
  otro Editor: desde aquí no se elige.
- **Medir el FBX crudo para decidir una colocación engaña.** El `jean_ajustado_doblado` del v9
  mide 2.56 × 3.17 × 0.48 como asset, y en la escena mide 0.26 × 0.32 × 0.05: la instancia de la
  escena de arte lleva **escala 0.1**. Y encima el arte **rota** las instancias, así que el lado
  largo del objeto no cae por el eje que uno supone —las botas son 0.349 de ancho en el FBX y
  0.295 en la escena, con el otro valor por el eje de la fila—. Con las medidas del FBX calculé
  que los seis de ropa pedían 1.11 m de fila; en la escena pedían 2.81. **Medir siempre la
  instancia, nunca el asset.**
- **Un EPP que el arte entrega por pares es UN elemento del catálogo, no dos.** El v9 trae
  `codera` + `codera (1)` y `rodillera` + `rodillera (1)`. Si cada pieza va suelta, la segunda
  se queda sin entidad y aparece como decorado encima de la mesa. Se agrupan bajo un padre
  (`coderas`, `rodilleras`) y la entidad va en el padre. Y las dos piezas se colocan **a lo
  ancho** de la tapa y alineadas en el eje de la fila: si no, el par pisa dos huecos y no se gana
  sitio. La primera versión solo las alineó a lo ancho y la fila siguió midiendo lo mismo.
- **Un nombre de objeto con un espacio al final deja un EPP fuera del ejercicio en silencio.** El
  v9 entrega `"jean_ajustado_doblado "`. `Module01SceneEntities.Find` comparaba el nombre tal
  cual, así que no lo encontraba y solo quedaba un aviso en consola. Ahora compara con `Trim()`:
  un carácter invisible no puede decidir si un elemento entra al ejercicio.
- **Cuando la fila no cabe en la tapa, la salida es el fondo del mueble, no apretar más.** Con 8
  elementos la zona de ropa pedía 3.51 m sobre una mesa de 2.62, y **los objetos solos ya sumaban
  2.81**: ningún reparto de huecos lo arregla. Lo que sí: **lo plano delante y lo alto detrás.**
  Un pantalón doblado de 5 cm de canto no puede tapar nada —la vista y el rayo del clic le pasan
  por encima—, así que ocupa la fila de delante y libera la de atrás. Al contrario sería peor: el
  chaleco mide 60 cm y esconde detrás de sí cualquier cosa. Quedó en 5 detrás con hueco 0.15 y 3
  planos delante con hueco 0.34, los 8 sobre la tapa y sin una caja solapada.
- **El hueco declarado entre elementos es un máximo cómodo, no una medida sagrada.** Antes, si la
  fila no cabía, arrancaba en el borde del mueble y colgaba por el otro lado. Ahora
  `StationLayout.FittingGap` reparte el sobrante y nunca baja de `MinGap` (10 cm), dejando
  `EdgeInset` (10 cm) libre en cada canto.
- **La Scene View no dibuja el stack de cámaras de URP.** Lo que vive en la capa `UI` —el HUD,
  los marcadores `!`— no sale en una captura de Scene View, aunque en el juego se vea perfecto.
  Para verlo de verdad, la captura de la Game View que deja el test de PlayMode en
  `Docs/verification/module01/`.

---

## 10. El scooter — diseño pendiente

> **Estado: aprobado el 2026-10-07. Tramo 1 hecho y afirmado; tramo 2 pendiente.**
> Charlie confirmó la premisa: «los mismos items de la inspección de e-bike también para la
> scooter, se revisa casi lo mismo, llantas, luces, frenos». Y eligió hacerlo en dos entregas.

### Lo que decidió Charlie

Los **mismos cinco chequeos** que la e-bike, no unos propios: «las dos tienen frenos, las dos
toca revisar luces, solo que en lugares diferentes». Así que se reutilizan los 5 `SafetyItemSO`,
sus claves de localización y las 3 `ZoneSO`. **Cero términos nuevos en I2.**

Y la puesta en escena cambia: **un solo punto de exhibición en mitad del garaje**, donde aparece
el vehículo elegido. El otro se apaga. Hoy los dos están en el suelo a la vez, en sitios fijos.

### El hallazgo que rompe la spec

**No es «cero líneas de código», como afirman `2026-10-05-modulo-01-ready-to-ride-design.md` §3.2
y el comentario de `Module01Sequences`.** Las *secuencias* sí se filtran solas por
`ContextRequirement`, pero `Module01Director._vehicle` es **un solo** `VehicleProfileSO`
serializado, hoy fijo en el de la e-bike. Las zonas que recorre el director salen de ese campo,
así que con solo crear assets el scooter recorrería las zonas de la bici. Hay que tocar código.

### Tramo 1 — HECHO el 2026-10-07

1. ✅ `Module01Director`: `_vehicle` → `List<VehicleProfileSO> _vehicles` + `SelectVehicle(string)`,
   que llama `Module01Experience.StartZones` antes de `Begin()`.
2. ✅ `SO_VehicleProfile_Escooter`: `escooter`, `scooter.fbx`, **las mismas 3 zonas** que la e-bike.
3. ✅ Tres filas más en `Module01Sequences.Sequences` con `"escooter"`. **Y los `SequenceId` pasaron
   a llevar el vehículo** (`Module01.Vehicle.Ebike.Cockpit`): `TaskSequenceRunner` indexa lo
   completado y el punto de reanudación por ese id, así que dos vehículos con el mismo id
   compartirían estado y cerrar uno daría por hecho el otro.
4. ✅ `VehicleStage` apaga el vehículo no elegido. Cableado por
   `RideSafe → Módulo 1 → Cablear los vehículos en la escena abierta`.
5. ✅ La tarjeta `Option_escooter` **ya existía** y la construye `UIModules.Module00.cs:89`; lo de
   «reactivarla» era falso.
6. ✅ Borrado el `ebike (1)` suelto (era el punto 9 del tramo 2, pero sin él «se apaga el otro»
   era mentira: quedaba una bici flotando).

### Tramo 2 — que los puntos estén donde van (PENDIENTE)

6. Punto de exhibición en `(-0.14, 0, 0.78)`, centro del `piso` (9.54 × 11.28).
7. Las 3 anclas del vehículo dejan de ser la fila fija de `z=2.30` y **orbitan** ese punto:
   `cockpit` de frente, `wheels` de costado, `visibility` por detrás, radio ~2 m.
8. Mounts por vehículo (`mount_point_brakes` y compañía, hijos de cada malla), porque la
   geometría difiere: la bici es larga (`z=1.97`), el scooter alto (`y=1.35`). Posiciones por
   defecto derivadas de los bounds. **Aproximadas: necesitan ojo humano en la escena.**
9. Borrar el `ebike (1)` suelto en `(-4.83, 1.45, 4.39)`, flotando a 1.45 m.

### Pruebas

Un tercer test de PlayMode con `vehicle = escooter`: recorre las zonas del scooter, la e-bike
apagada, el scooter en el punto de exhibición, los 5 puntos colgando de él, y corre la secuencia
de escooter y no la de ebike. El test de la e-bike afirma lo simétrico.

### Datos de la escena que ya medí

| Qué | Dónde |
|---|---|
| `piso` | centro `(-0.14, 0.02, 0.78)`, 9.54 × 11.28 |
| `ebike` | desde el v8, **4 piezas**: `manubrio` (todo el tren delantero, 0.66 × 1.11 × 0.76), `marco`, `pedales`, `rueda_trasera` |
| `scooter` | desde el 2026-10-14, **7 piezas en dos niveles**: `control_de_direccion` (con `freno`, `guardabarros` y `rueda_frontal` dentro) y `cuerpo` (con `rueda_tracera` y `soporte`). Caja igual que antes: 1.27 × 1.35 × 0.55 |
| `scooter.fbx` | `Assets/_Project/Models/CITY/MODELADOS 3d/scooter.fbx` |
| estaciones EPP | `head` mostrador del fondo, `clothing` mesa metálica (izq.), `load` banco (der.) |
| giros por zona | `cockpit` 90°, `wheels` 0°, `visibility` −90° |
| los 5 puntos | cuelgan de `mount_*` en la malla; los 2 con pieza se mudan encima de ella al encender el vehículo |

Los ids de entidad son `module01.{zoneId}.{itemId}`: **no llevan vehículo**, así que una sola
entidad sirve a los dos. Por eso ambos perfiles pueden apuntar a las mismas 3 zonas.

---

## 11. Bitácora

### 2026-10-16 (noche, 4) — correcciones de Cindy sobre la revisión de UI

Lo que no le gustó de la tanda anterior, ya cambiado (mismos menús, idempotentes):
- **Regla: los botones de Atrás / Continuar van FUERA de la caja**, sin fondo, alineados a sus
  bordes y con 16 de aire debajo. Idioma, Normas y Comodidad. Las tarjetas de Idioma y Normas
  se acercan al subtítulo y llevan el contenido centrado.
- **Vehículo vuelve a como estaba** (sin caja, barra abajo, etiquetas arriba, «SELECCIONADO»
  debajo de la tarjeta).
- **«Mi Elección»** como «Revisa tus elecciones»: título, lista en su caja, la nota debajo de la
  lista, y «Ver explicación» libre bajo el panel (`FollowBelow` a la caja). Los botones finales
  siguen a la caja.
- **Decisión go/no-go en vertical**: pregunta, pista (`M1/Decision_Hint`, nueva en I2) y las tres
  respuestas apiladas con su icono de estado.
- **«Continuar al Módulo 2» bloqueado**: píldora blanca sólida con texto atenuado. Los sprites
  `*_Disabled` del atlas son translúcidos y se perdían sobre el suelo.
- Sin la etiqueta «Reproduciendo» del video. Subtítulos 17° bajo la mirada (a 14 tocaban el panel
  de bienvenida del M1). Popup del EPP con 35 de aire bajo los botones. Frase del tutorial en dos
  líneas, con margen a los lados.
- Tests: EditMode 343/345 (los 2 de `Garaje_Test`), PlayMode M1 6/6.
- **Los «M1/...» que se veían en el editor:** 11 textos de la escena guardaban la clave de I2 como
  texto («M1/Yes», «M1/Decision_Question»...) y solo se traducían en Play. La composición de escena
  les escribe ahora su texto en español; la clave sigue en su `Module01Text`. En Play no había
  ninguno (comprobado con `TutorialHelpProbe.RawKeys` en cada pantalla del recorrido).
- **`trousers_fitted` en «Revisa tus elecciones»:** `SO_Module01PresentationCatalog` se generó antes
  de la estación de pantalones y no la tenía. La composición de escena lo resincroniza con el
  catálogo.

### 2026-10-16 (noche, 3) — revisión de las UIs: títulos dentro de la caja, tutorial y atlas

Trabajo de Cindy (con Claude), sin subir todavía. Recorrido completo en Play con capturas.

- **El tutorial salía encima del video.** `QuietDetector` solo contaba el video mientras
  `isPlaying`; al pausarlo (botón, foco perdido, menú del Quest) el contador de atasco seguía y a
  los 10 s aparecía la ayuda. Ahora un video pausado a mitad cuenta como video, y tras el video
  la ayuda espera a que la pantalla se asiente. Comprobado: pausado 25 s, sin ayuda.
- **Cada pantalla en una sola caja** (`Enclose`, `UI/Runtime`): un marco `Panel_Modal_Light`
  detrás de título, opciones y botones, con el mismo margen y que sigue a las listas que crecen.
  - M0: Idioma, Normas y Comodidad en su caja (el logo de la puerta queda detrás); en Vehículo la
    caja es solo la cabecera y la barra de confirmación se une a ella al elegir. Ya no tapa la
    bici: las etiquetas bajan junto a su vehículo. «SELECCIONADO» va dentro de la tarjeta.
  - «Mi Elección» en una caja con título azul sobre claro (la barra oscura era translúcida y se
    veía gris). Los botones finales, libres, sin recuadro (lo pidió Cindy).
  - Popup del EPP: el nombre es la cabecera del popup, que conserva su borde superior.
  - Cartel de estación con la zona en negrita y aire; go/no-go sin hueco abajo; botones finales
    centrados.
  - Pausa/repetir del video con los iconos del atlas; ninguna etiqueta de botón salta a dos
    líneas (se encoge antes); «Repetir instrucciones» e «Izquierda» caben.
- **Tutorial:** tarjeta a escala 1,3 y 65 cm (antes ocupaba un tercio de la vista y se cortaba
  abajo en PC), frase dentro de la tarjeta en Inter SemiBold, y el mando inclinado como se
  sostiene, con el gatillo de perfil. Las posturas se recalculan en sitio
  (`RideSafe → Tutorial → Recalcular posturas Touch Plus`).
- **Atlas:** `UI/RideSafe_UI.spriteatlasv2` empaqueta las dos hojas de 4096 (casi vacías) y el
  logo en 3 páginas de 2048 con mipmaps, ASTC 6x6 en Android y sin crunch. Las hojas quedan como
  fuente sin comprimir. `EditorSettings` pasa a *Sprite Atlas V2*.
- **Textos:** `M1/Orientation_Tag` y `M1/Video_Playing` en I2 (salían en inglés). Faltan en la
  hoja de Google. Los `!` usan Manrope a 82 en vez de AmmoText.
- **Cómo se aplica (todo idempotente, sin regenerar):** `RideSafe → UI → Componer paneles en
  sitio`, `RideSafe → Módulo 1 → Componer paneles de la escena`, `RideSafe → UI → Crear o
  actualizar el SpriteAtlas`, `RideSafe → Tutorial Help → Montar en RideSafe_Garaje_v5`. Los
  builders llaman a lo mismo, así que una regeneración no lo pierde.
- **Tests:** EditMode M0+M1+tutorial 343 de 345 (los 2 de `Garaje_Test`); PlayMode M1 6 de 6.
  UI PlayMode 10 de 13: los 3 que fallan ya fallaban con los prefabs de antes (comprobado
  restaurándolos): esperan dos columnas en «Mi Elección» y textos en inglés.

**Para retomar:**
1. Probar en Quest el tamaño y la postura del mando del tutorial.
2. El texto de detalle (13 px) se lee justo en VR: en captura a 1x se pierden letras finas. Subirlo
   a 14–15 es una decisión de la escala tipográfica.
3. Actualizar los 3 tests de UI PlayMode que se quedaron atrás.

### 2026-10-16 (noche, 2) — lo que salió en la prueba: el calzado y la rueda

Trabajo de Cindy (con Claude), sin subir todavía. Dos cosas vistas al probar el M1.

- **El calzado cerrado no aparecía en el pedestal** (estación «Ropa y calzado»).
  - Causa: `botas` estaba marcado *Static* y el static batching le cambia la malla por la
    combinada de la escena (§9).
  - Escena: se quitó solo *Batching Static* a `botas`, `df_g_helmet_01` y
    `jean_ajustado_doblado`; los otros dos no se fundían en el editor, pero podían hacerlo en el
    build. El horneado no cambia.
  - Código: `ItemInspector.CopyOf` devuelve null si la malla viene fundida, y el pedestal usa el
    prefab del catálogo.
  - Tutorial: el halo deja de colgar del objeto (§4), así que tampoco se copia.
- **En «Ruedas y estructura» no se sabía dónde tocar.** Brillaba toda la rueda y solo respondía
  una esfera invisible de 15 cm en el borde de la llanta.
  - `InspectionHighlight` le da al punto un área de clic con la caja de la pieza
    (`PartClickArea`), que solo responde si ese punto es el único por revisar y se apaga al
    resolverse.
  - En la cabina, con tres puntos a la vez, cada uno conserva su esfera: el área del manubrio
    enterraría los frenos.
- **Comprobado en Play:** el calzado aparece en el pedestal con su malla (`Ch20`), y cuatro
  rayos a la rueda (centro, arriba, delante, detrás) llegan a `point_wheels`.
- **Tests:** EditMode **479 de 481** (los 2 de siempre de `Garaje_Test`) y 1 inconcluso: el
  editor no funde mallas fuera de Play, así que el test del pedestal con malla fundida no se
  puede montar ahí. `TutorialHelp.Tests` 70 de 70.
- Al guardar la escena en 6000.4.9f1, un `SpriteRenderer` pasó de `serializedVersion` 3 a 2. Es
  inofensivo.

**Para retomar:**
1. Probar en Quest el calzado y la rueda.
2. Decidir si «Dirección y manubrio» vuelve a mostrar el `!`. Tiene el mismo problema que tenía
   la rueda: brilla el manubrio y solo responde una esfera invisible.
3. Lo que seguía pendiente de la entrada anterior.

### 2026-10-16 (noche) — limpieza de lo que sobraba y optimización para Quest

Trabajo de Cindy (con Claude), subido en `198bb1e`. Antes de tocar nada se auditó el proyecto
en tres frentes: el tutorial viejo, assets y scripts sin uso, y el coste en Quest.

- **Quitado, porque ningún GUID lo referencia:**
  - `Assets/_Recovery/` (3,4 MB de escena de recuperación);
  - `Models/plataform.prefab`: la escena usa otro `plataform`, el de `CITY/FX`;
  - los restos de la plantilla de Unity (`Scenes/SampleScene`, `Readme`, `TutorialInfo`,
    `InitTestScene*`);
  - una carpeta vacía de `CommandSystem` y `TouchPlusPosePreview.cs`.

  El zip de diseño pasó a `Docs/design/`. Queda sin subir el borrado del `.txt` de
  `unityassetcollection`.
- **Un borrado que salió mal y se deshizo:** los materiales de `OculusControls` no los nombraba
  ninguna escena ni prefab, pero los FBX del Touch Plus los usan desde su `.meta`. El control
  salió magenta y se restauraron desde git; la comprobación se repitió sobre los `.meta` y el
  resto estaba limpio (§9).
- **Lo que la auditoría daba por quitable y no lo es:** `ControllerHint3DView`,
  `SpriteTargetHighlight` y `TextBackplate`. Los usa el código del tutorial viejo; se irán
  con él.
- **Optimización:**
  - `ServiceLocator` sin logs por petición, tras el símbolo `RIDESAFE_SERVICE_LOG` (§9), y
    `UIText` guarda el proveedor;
  - el tutorial busca objetos con `Physics.OverlapSphereNonAlloc` a menos de 4 m y botones
    con `Selectable.AllSelectablesNoAlloc`, en vez de recorrer la escena cada segundo;
  - el control lee los canvas con `GraphicRegistry` en lugar de buscar todas las imágenes;
  - `HelpDirector` no redibuja si nada cambió;
  - audio y video se listan cada 5 s, y el panel de Comfort solo hasta encontrarlo;
  - `FollowBelow`, la barra de progreso del video y `GrowWithContent` dejan de rehacer canvas
    sin necesidad;
  - el `!` no trabaja mientras está oculto.
- **El control ya no se aparta por un roce.** En Welcome su borde rozaba «Comenzar» un 1 % de
  la pantalla y saltaba al lado, medio fuera de la vista. Ahora un solape menor del 5 % de la
  tarjeta no cuenta (`HintSpot.Tolerance`).
- **Comprobado en Play:** la ayuda de primera vez en Welcome, el clic aprendido y la ayuda 3D en
  la estación, con el casco negro ya contado como opción.
- **Tests:** EditMode **471 de 473** tras bajar los últimos commits, con los 2 de siempre de
  `Garaje_Test`; `TutorialHelp.Tests` 68 de 68.

**Para retomar:**
1. Subir el borrado del `.txt` de `unityassetcollection`.
2. Correr los 6 tests de PlayMode del M1 desde el Test Runner.
3. Decidir con el equipo lo que dejó abierto la auditoría (§8).

### 2026-10-16 (tarde) — la categoría pasa a `M1/`, el EPP elegido se queda en verde y hay voz

- **El pedestal enseñaba otra cosa que la mesa, y el cliente tenía razón.** Medido: instanciar
  el FBX del catálogo da una pose distinta a la que el arte dejó en la mesa en **12 de los 13**
  EPP —el casco bueno y las gafas a 270° en Y, el jean ajustado a 270° en X con escala 0.1, el
  teléfono a (270,270,0)—, así que un pantalón doblado que en la mesa se ve plano salía de
  canto. Y en los pares (coderas, rodilleras) el FBX es **una** pieza y la mesa tiene **dos**.
  Ahora `ItemInspector` **copia el objeto de la escena** y el prefab del catálogo queda de
  respaldo. Es el mismo camino que ya usaban las piezas del vehículo.
- **El EPP elegido ya no desaparece: se queda marcado en verde.** Lo pidió Charlie. Tres
  estados derivados —apagado en el pedestal, verde si está elegido, normal si no—, con shader y
  material nuevos (`SelectedHolo`), montados como hijos con la misma malla y apagando el
  renderer original, sin tocar un solo material del arte. **De regalo arregla el
  deseleccionar:** un objeto apagado tiene el collider apagado, así que antes no se podía volver
  a pulsar. Ahora el popup pregunta «¿Quitar este elemento de tu preparación?»
  (`M1/Remove_Question`) y el sí lo saca del libro y del checklist.
- **Toda la categoría de I2 pasa de `Module1/` a `M1/`**, por decisión de Charlie. Son 101
  términos, 70 literales en 15 `.cs`, 43 claves en 25 assets, `PresentationTexts.txt`, el prefab,
  las dos escenas y la carpeta de audio (`Sounds/Module1` → `Sounds/M1`, **movida** con
  `MoveAsset` para conservar los GUID). **Las otras categorías no se tocaron:** 86 términos antes
  y 86 después, sin un cambio de texto; se comprobó contra un volcado previo.
- **El asset de I2 se puso al día desde `Docs/Module1_I2.csv`**, que es ahora el único CSV del
  módulo. De 164 términos a **187**: 101 en `M1/` y 86 de los demás módulos. Entraron las 24 que
  faltaban —los 18 `_Risk`, los 3 pantalones, coderas, rodilleras y `Zone_Trousers`—, así que
  **se acabaron las claves crudas en pantalla**: el volcado de textos de PlayMode las enseñaba.
- **El inglés del CSV sale del asset, no de la hoja del Drive.** La hoja volvió del traductor
  automático con el inglés retraducido desde el español: `Yes`→«Yeah», `Secured load`→«Cargo
  insurance», «prepárate antes de **rodar**»→«before **filming**», y «una dirección con
  **juego**»→«a steering system with a **game**». El inglés es el origen.
- **Locución de los 18 elementos en los dos idiomas**, generada con la tool (voz **Alice —
  Clear, Engaging Educator**, la misma de `M01_Welcome`; estabilidad 0,75 · similitud 0,85 ·
  estilo 0,1 · speaker boost). 36 clips con su `.json` de tiempos por palabra.
  `M1ItemAudioBatch` lo deja repetible desde el menú.
- **Y una lección de diseño que costó generar dos veces:** la primera tanda locutaba el **por
  qué** de cada elemento («es la única protección de la cabeza»). Charlie lo paró: eso **sesga
  la elección**, porque le da la respuesta al aprendiz mientras decide. Ahora se locuta **solo el
  nombre**; el motivo se explica al final, con el video. Los 18 textos `_Risk` se quedan como
  **texto de feedback**, no como voz.
- **`Speak` resuelve el clip por idioma** (`Cachacos.LocalizedResource`, genérico: los videos
  van a necesitar lo mismo). El seam `ILocalizationProvider` aprende `CurrentLanguage`. Y el
  `ItemInspector` tiene por fin su `AudioSource` (`ItemVoice`, 2D): sin él se callaba sin
  quejarse.
- **Arreglado el subtítulo del reporte**, que tumbaba 3 de los 6 tests de PlayMode desde el
  commit de Cindy: `ColumnComposition` le activa el ajuste de línea al texto y la caja seguía
  clavada en 60, y en español pedía 36,3 sobre los 34 disponibles. Ahora 64. Lo intenté antes
  con `ContentSizeFitter` y fue peor: ver §9, vuelve en cada guardado.
- **466 de 468 en EditMode** —los 2 de siempre del módulo 0, de la rama del tutorial— y **6 de 6
  en PlayMode**.
- **El horneado no se sube**, para no romperle la escena a Cindy (§3 y §8).

- **«Mi Elección» deja de listar lo omitido.** Charlie lo vio en la demo: salía «Calzado
  cerrado · OMISIÓN» y eso convierte el resumen en un boletín de fallos. Ahora la lista recorre
  **lo que el aprendiz se llevó**, no el catálogo, y en **tres estados y ya**: núcleo →
  *Correcta*, inapropiado → *Incorrecta* (deteriorado o no, se dicen igual), opcional y según
  condición → *No afecta* (`M1/Legend_NoEffect`, término nuevo). Los estados `CoreOmitted` y
  `AtRisk` siguen en el enum pero el reporte ya no los emite.
- **Queda una incoherencia declarada:** la **nota sí cuenta las omisiones**
  (`SelectionScore.Of` juzga cada elemento de núcleo), así que quien se deje el calzado verá
  todas las filas correctas y un «Por mejorar» que la pantalla no explica. Se propuso poner el
  recuento en la nota de arriba y Charlie prefirió los tres estados a secas.
- **La vitrina del módulo 0 sube de 0 a 0,40.** No estaban «bajos»: estaban fuera de cuadro.
  Desde los ojos (y = 1,45), a 2,25 m y con 60° de campo, la vista cubre de **y = 0,15 a 2,75**,
  y los vehículos se apoyaban en 0. Con 0,40 la bici ocupa 0,40–1,07 y el patinete 0,40–1,21:
  25 cm de margen abajo y sin llegar a las tarjetas (1,48). Sube el conjunto, porque las nueve
  partículas de la plataforma simulan en **espacio local** y cuelgan del mismo ancla. El número
  vive en `Module00ShowroomSetup.GroundHeight` y en el componente de la escena.

**Para retomar, por orden:**

1. **Correr PlayMode del M1.** El último cambio —el de «Mi Elección»— se subió con **EditMode
   verde (336/2) y PlayMode sin correr**, porque el Editor estaba cerrado. Toca justo esa
   pantalla.
2. **Decidir dónde se enseña el `_Risk` en el reporte.** Está aprobado que salga **solo en los
   elementos que estuvieron mal**, pero falta montarlo. Hoy la fila dice `Dirección y manubrio ·
   CORRECTA` y nada más.
2. Pegar `Docs/Module1_I2.csv` en la hoja del Drive, como copia de referencia. **No reimportar
   desde la hoja:** el asset ya es la fuente buena.
3. Acordar la versión de Unity y commitear el horneado una sola vez.

### 2026-10-15 / 16 — el tutorial agnóstico y un repaso de paneles del M0 y el M1

Trabajo de Cindy (con Claude) sobre `dev`, **sin commit todavía**.

- **La v5 salía negra en 6000.4.9f1.** Su horneado se había hecho en 6000.5.4f1 y no carga en la
  versión anterior (§9). Se rehízo en 6000.4.9f1 con la misma configuración: 4 lightmaps, 3
  sondas, mismo GUID de `LightingData`, sin tocar la escena. Queda pendiente acordar una versión
  (§8).
- **El tutorial se rehízo desde cero como apoyo agnóstico.** El de la rama `Feature/tutorial` era
  un flujo propio con su copia de las pantallas del M0 y su secuencia de pasos. El nuevo
  (`Systems/TutorialHelp`, namespace `RideSafe.Help`) no conoce módulos: observa la escena y
  muestra el Touch Plus con el gatillo, el resaltado y una frase corta. Las decisiones de
  producto están en la spec: primera vez por tipo, atasco a 10 s, todas las opciones por igual,
  mano dominante, lo aprendido dura la sesión. Detalles que salieron al probarlo en la v5:
  - **El clic se detecta en el propio objetivo** (`ClickWitness`, un componente invisible que no
    se guarda). En PC la escena usa la entrada de Unity, no la de AutoHand, y escuchar solo a
    AutoHand no veía ningún clic.
  - **Solo cuentan los objetos 3D que el rayo alcanza a menos de 4 m.** La puerta del garaje no
    tiene collider y el EPP de dentro contaba ya en la pantalla Welcome.
  - **El cono de visión es de 45°.** Con 35° las gafas de la estación quedaban fuera y solo se
    resaltaba el casco.
  - **En la orientación del M1 el tutorial está en pausa** (lo decidió Cindy): el casco ya está
    en la mesa y nada distingue esa pantalla de una estación.
  - **El control se coloca abajo al centro**, y si ahí tapa un panel se aparta al lado que menos
    tape.
  - **Revisión final independiente:** corregidos tres importantes (resaltar todas las opciones
    aunque una salga de la mirada, restaurar el color de los botones con tinte, avisar una sola
    vez si falta el botón). Quedan como menores: dos pausas superpuestas (gana la última),
    objetivos sin imagen o con malla skinned, gesto sin asignar, objetivo explícito destruido,
    testigo en un Slider, clics fuera del cono, traducción por fotograma. El rendimiento en Quest
    queda por medir.
- **Módulo 0, pantalla de vehículo:**
  - **tarjetas ajustadas al texto** en cada idioma, que crecen para el check al seleccionar;
  - **barra de confirmación en dos líneas**;
  - **vitrina propia** fuera del garaje con las copias girando sobre sus plataformas.

  El prefab se editó en sitio (`Module00VehicleLabelLayout`) y el builder hace lo mismo.
- **Módulo 1:**
  - **los `!` responden** (`ClickArea`);
  - **«Revisa tus elecciones» crece con la lista**;
  - **«Mi Elección» va en una columna** con el título centrado;
  - **los botones finales siguen a la lista**;
  - **la nota de «Mi Elección» se quedaba en 0×0** por una sobrescritura vieja de la escena,
    ya revertida (§9).

  Prefab editado en sitio (`Module01LayoutFixes`), builder y `Module01PresentationBuilder`
  al día.
- **Tests:** EditMode **441 de 443**. Los 2 que fallan son los de siempre de `Garaje_Test`
  (escena de prueba vieja del tutorial). PlayMode del M1 **sin correr** tras los cambios.

**Para retomar, por orden:**

1. Correr los 6 tests de PlayMode del M1 desde el Test Runner.
2. Hacer los commits: tutorial → módulo 0 → módulo 1 + escena.
3. Acordar la versión de Unity antes de commitear el horneado.
4. Grabar el video del recorrido en PC y probar en Quest.
5. Siguen pendientes de Charlie: los 5 términos de I2 y `Module1/Zone_Trousers`.

### 2026-10-15 (mañana) — el garaje v9: pantalones doblados y dos EPP nuevos

Daniel entregó el v9 y Charlie pidió dos cosas: cambiar el garaje y meter los EPP nuevos en la
lista de seleccionables.

- **El v9 no cambia la malla del garaje.** De sus 636 GUID, **589 son byte a byte los que ya
  teníamos**, 14 cambian y 33 son nuevos. `interior.fbx` no se toca —sigue sin cambiar desde el
  v6—. Lo que cambia es la escena de arte (donde Daniel colocó lo nuevo), su horneado,
  `House04.prefab`, `suelo 2.mat` y los dos materiales de pantalón. Se comprobó **antes** de
  importar, hasheando cada entrada del paquete al vuelo por una tubería, sin extraer 1,5 GB a un
  disco que tenía 11 GB libres. Y el patinete despiezado **no se pisó esta vez**: el `scooter.fbx`
  del v9 es idéntico al nuestro, igual que el `ebike.fbx`.
- **Los 33 nuevos son 5 modelos y sus materiales**: los tres pantalones **doblados** y las
  **coderas** y **rodilleras**, estas dos por pares. Cayeron en `Assets/CITY` y se movieron con
  `MoveAsset`, que conserva el GUID.
- **Los pantalones doblados arreglan un apaño.** De pie medían 1,16 m y había una regla para
  plantarlos en el suelo porque no cabían en la tapa; doblados miden 5 cm de canto y se apoyan en
  la mesa como el resto, que es lo que Charlie pedía.
- **Coderas y rodilleras entran al catálogo como Opcional**, en la zona `clothing`. La zona pasa
  de 6 a 8 elementos y el catálogo de 11 a 13. **La categoría es una suposición mía** —se
  recomiendan y no se exigen, igual que las gafas— y está en la lista de pendientes para que
  Charlie la confirme: si fueran de núcleo, omitirlas cambiaría el veredicto.
- **Lo que costó de verdad fue que los 8 no caben en una fila.** Los objetos solos suman 2,81 m
  sobre una mesa de 2,62, así que ningún reparto de huecos lo arregla, y mi primera estimación
  —1,11 m— era mala porque medí los FBX en vez de las instancias, que el arte escala y rota. La
  salida fue usar el **fondo** del mueble: lo plano delante, lo alto detrás. Quedó en 5 detrás con
  hueco 0,15 y 3 planos delante con 0,34, **los 8 sobre la tapa, apoyados en y=0,92 y sin una
  caja solapada**. De paso el hueco entre elementos dejó de ser fijo: se aprieta hasta 10 cm
  antes de dejar la fila colgando, que es lo que hacía antes.
- **Tres cosas del v9 que habrían pasado en silencio:** el objeto del jean ajustado se llama
  `"jean_ajustado_doblado "` **con un espacio al final** y el buscador por nombre no lo
  encontraba; un par de coderas son **un** elemento y no dos, así que hay que agruparlas bajo un
  padre; y `Module01Greybox` seguía declarando la zona de ropa con **dos** elementos desde el
  7 de octubre, así que volver a correr ese comando borraba la bufanda y los tres pantalones.
- **La puerta, otra vez.** El cambio de mundo se llevó `Module01Experience._door`, como en el v8.
  Recableada y con sus partes fuera de estático.
- **488.648 triángulos**, suben desde 459.652 por el decorado nuevo. Sigue por encima del
  presupuesto de Quest.
- **Media hora perdida por un error mío mal diagnosticado:** metí cuatro tests nuevos en la
  clase equivocada de `StationLayoutTests.cs` y, con el error de compilación presente, Unity
  **no recarga el dominio** y sigue ejecutando el build anterior sin que nada lo diga. Lo
  atribuí al relay, y era falso. El detector era la consola, que es lo que no miré. En §9.
- **Los pantalones acabaron con estación propia, y eso lo pidió Charlie al ver el garaje.** En
  vez de apretar ocho elementos en la mesa de ropa, los tres doblados se van al **banco largo y
  vacío del fondo derecho** (`mesa`, 3.72 m, el más largo del garaje) y la ropa se queda en
  cinco. El recorrido pasa de 6 zonas a **7** y el reporte gana una sección. Su tapa **no se
  pudo derivar de lo que el arte dejara encima, porque el banco está vacío**: se midió la malla
  quedándose con la franja horizontal de más área por debajo de la balda alta —2.78 m² mirando
  hacia arriba a **1.02 m**, frente a 1.39 m² a 1.74—. Falta el título de la zona en I2
  (`Module1/Zone_Trousers`), que lo pone Charlie.
- **Los tres pantalones de pie se apagan.** Quedaban en la escena sin entidad, duplicando el
  ejercicio —dos pantalones anchos y solo uno pulsable— y, al no pertenecer ya a ninguna
  estación, nadie los colocaba: el despeje de pasillo arrinconó el ajustado contra la pared del
  fondo. `Module01SceneEntities.HideSuperseded` declara el par (viejo → sustituto) y los apaga,
  **sin borrarlos**, y solo si el sustituto está en la escena.
- **El test del recorrido llevaba la lista de zonas escrita a mano** y por eso, al meter la
  estación nueva, tres tests de PlayMode se quedaron esperando `load` mientras el recorrido
  estaba en `trousers` —y el fallo apuntaba a un sitio que no era—. Ahora la deriva del catálogo
  y del perfil del vehículo, que es justo lo que recorre el director.
- **322 de 322 en EditMode y 6 de 6 en PlayMode, dos corridas seguidas.** Captura nueva:
  `estacion-trousers`. Los 322 son las tres assemblies del módulo 1; el histórico de «347/349»
  incluía además la del módulo 0, con sus 2 fallos de siempre.
- **Horneado rehecho tras mover las estaciones**, y comprobado: **379 de 430** renderers con
  mapa, índice más alto 3 de los 4 disponibles, y post-proceso encendido en las tres cámaras
  base. Son 430 y no 433 porque los tres pantalones de pie quedaron apagados.

**Para retomar, por orden:**

1. Pedirle a Charlie el título de la zona en I2 (`Module1/Zone_Trousers`, ES/EN) y los 5
   términos de elementos que faltan; hoy se ven las claves crudas.
2. Que confirme la categoría de coderas y rodilleras: entraron como **Opcional** y es
   suposición mía.
3. Avisar a Cindy de que `dev` se movió otra vez, que ella trabaja sobre `RideSafe_Garaje.unity`.

### 2026-10-15 (madrugada) — el garaje del v8 en la escena, y la luz vuelve a su sitio

Charlie vio el garaje «distinto» y con la luz rota. Costó varias vueltas porque **no era una
cosa, eran tres**, y ninguna se veía en un `git diff`.

- **La luz: la escena no horneaba la suya, la tomaba prestada de la escena de arte.** Era gratis
  mientras nada se moviera, pero el horneado es por escena: en cuanto las estaciones y el
  vehículo cambiaron de sitio, el mapa dejó de corresponder y salieron manchas duras en el suelo
  con las paredes apagadas. Ahora tiene el suyo en `Assets/_Project/Scene/RideSafe_Garaje_v5/`,
  2 lightmaps y 3 sondas, 378 de 436 renderers. **Hay que repetirlo cada vez que se mueva el
  mundo o las estaciones.**
- **El post-proceso estaba apagado en las tres cámaras base.** El `Box Volume` —Bloom,
  ColorAdjustments, WhiteBalance— llevaba ahí todo el tiempo sin aplicarse.
- **El suelo: no era el material que parecía.** El objeto `piso` llevaba `suelo 1` —baldosa
  cerámica blanca brillante, tiling 2×2— en vez de `SUELO.mat` —`plaster_grey_04` a 6×6, mate,
  hormigón continuo—. Hay tres materiales de suelo y el bueno no lo usaba nadie. Se reaplica
  desde `Module01WorldSwap.ApplyGarageFinishes()` porque el cambio de mundo sustituye el `piso`
  entero y un ajuste a mano se pierde en la siguiente entrega.
- **Cambiado el mundo al del paquete de arte**, con los tres comandos que eso obliga a rehacer
  detrás: entidades, estaciones y vehículo. **459.652 triángulos.** Y hubo que recablear a mano
  `Module01Experience._door`: la puerta es arte, así que el cambio se llevó la instancia. Lo cazó
  PlayMode con una `UnassignedReferenceException`.
- **Vuelven las dos bicis colgadas.** Seguían en la escena y activas pero **invisibles**: eran
  copias de la e-bike de antes del v8 y su `MeshFilter` se quedó sin malla al despiezarse el FBX.
  Bounds `(0,0,0)`. Rehechas desde el `ebike.fbx` actual.
- **Reimportar `garage v8.unitypackage` no trajo nada** y encima pisó el scooter despiezado con
  el de malla única. Comprobado abriendo el paquete por fuera de Unity: `Garage.unity`,
  `interior.fbx` y `suelo 1.mat` son **byte a byte** los que ya había. La malla del garaje no se
  toca desde el **v6** del 12 de octubre: el v7 fue horneado y el v8 fue la e-bike y horneado.
- **Lo que más tiempo costó fue diagnosticar por ojo.** Atribuí el suelo blanco al merge de
  Cindy y me equivoqué; también di por buena una comparación de `git diff` sobre una **ruta que
  no existía** —y git responde «sin cambios»—. Lo que zanjó cada punto fue comparar hashes y
  medir, no mirar capturas. Está en §9.
- 347/349 EditMode y 6/6 PlayMode. `dev` y `feat/garaje-v6` al día en `3dff540`.

### 2026-10-14 (noche, 3) — lo de Cindy entra, y `dev` se pone al día

- **`dev` queda al día y pusheada**, con todo: garaje v8, módulo 1 entero, patinete despiezado y
  los materiales e iluminación de Cindy. Fue avance directo porque `dev` no se había movido, así
  que **no hizo falta hacer checkout de `dev`**: se empujó la referencia con
  `git push origin feat/garaje-v6:dev`, que con Unity abierto evita mover miles de ficheros
  atrás y adelante. `feat/garaje-v6` apunta al mismo commit.
- **El merge de `Feature/tutorial` no se podía hacer tal cual.** 149 de sus 186 ficheros caen en
  `Assets/CITY`, el layout viejo, con los mismos GUID que los nuestros. Comparados uno a uno:
  **126 idénticos, 0 nuevos, 23 distintos**; de esos 23, siete son `.meta` de carpeta que no hay
  que tocar. Se copiaron los 16 que importaban sobre su pareja en `_Project` y se borró
  `Assets/CITY`. Resultado: 24 ficheros, todos en `_Project`, cero duplicados.
- **Los 13 conflictos se resolvieron tomando un lado entero**, nunca fusionando a mano. Las
  cuatro fuentes TMP, las nuestras: medido sobre `Inter-Regular`, lo nuestro tiene 9428 glifos y
  74 caracteres dinámicos y lo suyo 8958 y ninguno, así que lo nuestro es superconjunto. El
  horneado y `House04.prefab`, los suyos. `OpenXRPackageSettings`, el nuestro: solo churn de
  fileID.
- **El garaje se ve distinto, y es a propósito.** El suelo pasa de hormigón gris moteado a
  baldosa blanca —viene de `suelo 2.mat`— y la escena queda más luminosa. Es el cambio de Cindy,
  no una regresión; conviene que Charlie lo mire.
- **Lo que confirma que no se rompió nada:** 0 materiales con shader roto, y el índice de
  lightmap más alto que pide la escena v5 es 1 de los 4 disponibles, así que ninguna referencia
  se sale de rango. Los 112 de 436 renderers con horneado no los cambió el merge: la escena v5
  no está entre los ficheros tocados.
- **Dos trampas nuevas en §9**, las dos caras de lo mismo: copiar un `.meta` a mano no conserva
  el GUID si el viejo sigue registrado —Unity le asignó uno nuevo al occlusion de Cindy y lo
  dejó huérfano, así que se borró—, y un horneado se toma entero o no se toma.
- 347/349 EditMode y 6/6 PlayMode, los mismos números que antes del merge.

### 2026-10-14 (noche, 2) — el patinete despiezado

Daniel entregó el `scooter.fbx` partido, y con él se le hizo al patinete lo mismo que al v8 le
había hecho a la bici: delinear la pieza real en vez de marcarla con un signo flotante.

- **Llega en siete piezas y en dos niveles**, con el GUID intacto, así que el perfil y la
  instancia de la escena lo recogieron solos: `control_de_direccion` (con `freno`,
  `guardabarros` y `rueda_frontal` colgando) y `cuerpo` (con `rueda_tracera` y `soporte`). El
  anidamiento no estorba porque `VehicleStage.FindPart` busca en toda la jerarquía. La caja
  total no cambia —1.27 × 1.35 × 0.55—, así que los cinco mounts siguen valiendo y no hubo que
  recolocar ninguno.
- **Tres de cinco puntos con contorno, uno más que la bici**, porque el patinete sí trae la
  maneta de freno como pieza: `point_steering`→`control_de_direccion`, `point_brakes`→`freno`,
  `point_wheels`→`rueda_tracera`. `point_electrics` y `point_lights` se quedan con el `!`, y es
  la misma decisión que en la bici: la batería va dentro de `cuerpo` —la plataforma entera, 1,16
  m— y delinearla para decir «revisa el sistema eléctrico» enseña algo falso; el faro no existe
  como pieza. Ojo con `rueda_tracera`, con la falta: es como viene en el FBX y se copia tal cual.
- **Lo que no se veía venir: las dos mallas están en espejo.** La bici tiene el morro en la X
  local baja y el patinete en la alta, así que la tabla de giros compartida presentaba el
  patinete **de culo** en la zona de manubrio y con la rueda detrás del checklist en la de
  llantas. `Yaws` pasa a ser por vehículo. Está en §9.
- **El invariante se afirma, no se repite.** `VehiclePartsTests` no comprueba «el giro vale −90»:
  comprueba que, girado para esa zona, la pieza de la dirección queda del lado del aprendiz y la
  rueda del lado libre del checklist, derivándolo de la geometría del FBX. Así una entrega que
  reexporte una malla girada lo rompe. Y otro test exige que cada pieza declarada **exista** en
  su malla: si Daniel renombra una, hoy eso solo dejaba un warning en la consola y el punto caía
  al signo sin que nada fallara.
- **El patinete no lo recorría nadie.** Su único test de PlayMode se paraba en el onboarding. Se
  añadió `El_patinete_recorre_sus_zonas...`, que es `RunFlow` con `escooter`: el helper ya estaba
  parametrizado y `AssertInspectionCues` ya leía el perfil, solo faltaba llamarlo. Las capturas
  llevan sufijo por vehículo para que las dos corridas no se pisen los PNG.
- **El contorno del freno mide 58 × 12 px** de 2560×1440 a los 2,7 m del punto de exhibición —el
  más pequeño con diferencia; el siguiente es la rueda con 125 × 32—. Se ve en la captura, pero
  al delinearse se calla su `!`, así que **es decisión de Charlie** si ese punto se queda con los
  dos avisos. No se tocó: la regla pedida era la de la bici.
- Ninguna esfera blanca encendida en toda la escena: medido, 0.
- 347/349 EditMode y **6/6 PlayMode**, dos corridas seguidas. Capturas nuevas con sufijo
  `-escooter`, entre ellas `vehiculo-contorno-cockpit-escooter` y `-wheels-escooter`.

### 2026-10-14 (madrugada) — repaso de diagramacion, texto por texto

Charlie pidio una revision lenta: «que nada se solape ni quede encima de nada, sobre todo el
panel de seleccion». Se le anadio al recorrido un **volcado** que escribe cada texto visible con
sus medidas a `Temp/ridesafe-textos.txt`, para poder revisarlo renglon a renglon en vez de a ojo
sobre una captura.

- **El resultado del repaso:** ningun texto se sale de su caja en ninguna pantalla, y los
  avisos de cercania que salen son todos filas consecutivas de una lista (4,3 cm entre letras)
  y el pie del video con sus botones. Ningun solape de verdad.
- **Pero salio un fallo que ningun test tocaba, y es del panel de seleccion.** `LeftColumn`
  lleva `ContentSizeFitter` y crece con las filas; el boton «Ver explicacion» estaba clavado en
  `y = -469`. Medido: con 5 filas sobran 62 px y **a partir de 7 la lista se le echa encima**.
  La seccion personal tiene ahora 11 elementos y todos son aceptables, asi que es alcanzable.
  El recorrido solo acepta 5 —nucleo y segun-condicion— y por eso nunca lo destapo.
  Arreglado con `ComparisonView.KeepBelow`: el boton se coloca **debajo de la columna**, no en
  un sitio fijo. Lo guarda un test de PlayMode que llena la lista a 11 filas.
- El separador entre la leyenda y el veredicto pasa de tres espacios a `·`, que es el mismo que
  ya usaba el titulo dos lineas mas abajo. En la captura el hueco se leia como un error.
- 340/342 EditMode y **5/5 PlayMode**, dos corridas seguidas.

### 2026-10-14 (noche) — los pantalones entran al ejercicio y se acaban las esferas blancas

- **Los tres pantalones tapaban la puerta de la casa**, y no era culpa del arte: la regla de
  despeje de una sesión anterior los había empujado a esa pared. `pantalon ancho` estaba justo en
  mitad del vano (`Door_Windowed_Brown (1)`, `z = −2.88`, 1,02 m de hueco) y `pant low` flotaba
  18 cm sobre el suelo.
- **Ahora son parte del ejercicio**, dentro de `clothing` —que literalmente se llama «Ropa y
  calzado»—, con la misma forma que la zona de cascos: `jean ajustado` núcleo, `pantalon ancho`
  inapropiado (la pernera se engancha en la cadena), `pant low` opcional. Charlie pidió además
  uno **reflectivo o corta-viento**: ese modelo no existe, hay que pedírselo a Daniel y entrará
  como «según condición», igual que el chaleco.
- **El montador aprende a apoyar en el suelo.** Lo que mide más de 70 cm se planta en el suelo en
  vez de sobre la tapa, conservando su hueco en la fila. Se probó primero con una franja **por
  delante** de todo el mueble y era peor: un pantalón de 1,16 m plantado delante de unas botas
  que descansan a 0,92 las tapa.
- **Y si la fila no cabe en el mueble, arranca en su borde en vez de centrarse.** Con seis
  elementos la fila de ropa mide 3,62 m y la mesa da 2,62: centrada, las botas quedaban 32 cm
  fuera de la mesa, flotando. Como los altos van al final del catálogo, arrancar por el borde
  deja en la tapa justo a los que les toca.
- **Se acabaron las esferas blancas.** Los cinco puntos del vehículo no tienen modelo propio y
  `ItemInspector` les fabricaba una `PrimitiveType.Sphere`: al pulsar «frenos» lo que giraba en
  el pedestal era una bola. Ahora el pedestal enseña, por orden: su propio modelo, **una copia de
  la pieza real** del vehículo si la hay, o el **`!`**. Y el signo **no gira** —es texto plano y
  al dar media vuelta se queda de canto—. Las esferas que fabrica el montador para entidades que
  faltan nacen con el renderer apagado: son volumen de clic, no se ven.
- **Dos asserts del recorrido daban por hecho que en el pedestal siempre hay un `Renderer`.**
  `AssertModelClearsName` reventaba con `IndexOutOfRange` en cuanto dejó de haber esfera. Ahora
  mide el glifo cuando no hay malla.
- **Los textos de los tres pantalones no están en I2**; los pone Charlie en el Drive. No se
  desactivó la comprobación de «nombre sin traducir»: la deuda está declarada en
  `Module01SliceTests.SinTraducirTodavia`, y el test **avisa cuando ya estén puestos** para
  vaciar la lista.
- 340/342 EditMode y 4/4 PlayMode, dos corridas seguidas. Capturas nuevas:
  `pedestal-con-pieza`, `pedestal-sin-pieza`.

### 2026-10-14 (tarde) — las estaciones repartidas por el garaje

Las tres zonas personales estaban en el mismo mostrador del fondo: el aprendiz se
teletransportaba tres veces para mirar siempre a la misma pared. Charlie: «tenemos un entorno
bonito, podemos usarlo». Ahora cada zona vive en un mueble distinto.

| Zona | Mueble | Tapa | Cómo se midió |
|---|---|---|---|
| `head` | `cabinetmid 1`, pared del fondo | 1.079 | las bases de los 7 EPP que dejó el arte |
| `clothing` | `metal table`, pared izquierda | 0.92 | mesa de una pieza: su caja **es** la tapa |
| `load` | `otra mesa`, banco de la derecha | 1.04 | la regla y el medidor ya apoyados |

- **La estructura ya estaba:** cada zona tenía su ancla desde siempre. Lo que faltaba era que
  el ancla **se derivara** del grupo en vez de ser una posición escrita. Ahora sale del mueble,
  y por eso mover una estación es cambiar una fila de `Module01Stations`.
- **Las tapas son dato, no derivadas, y eso tiene motivo:** cada mueble del garaje es **un solo
  renderer**. No hay sub-malla «tapa» que medir, y la caja del mueble incluye las alacenas —la
  del banco derecho llega a 2.43—. Un test comprueba que la tapa declarada cae dentro de la caja,
  para que una entrega que cambie el mobiliario no pase en silencio.
- **Vuelve `clothing_loose`, con la bufanda.** Existía desde siempre con su clave de I2 ya
  traducida («Ropa o cordones sueltos»); se apagó el 2026-10-07 por no haber modelo y el v7 lo
  trajo. **Cero términos nuevos.** Con esto la zona `clothing` recupera su respuesta equivocada
  —hasta hoy el aprendiz aceptaba las dos cosas y acertaba— y `ZonesWithoutRisk` queda vacía.
- **El despeje de pasillo casi se carga el garaje.** La primera regla cogía la caja que envuelve
  al grupo y al ojo, y apartó **catorce herramientas** del banco de la derecha —regla, martillo,
  machete, segueta—; dos acabaron fuera del garaje. Estaban AL LADO de la estación, no ENTRE el
  aprendiz y ella. Lo correcto: el pasillo empieza **un palmo por delante** del grupo, no en su
  borde, y se mide el **centro** del objeto, no el roce. Y el buscador de hueco se queda dentro
  de la planta del garaje. Se restauró con `git checkout` del `.unity` y se rehizo con los
  comandos, que para eso son idempotentes.
- **Tres mediciones para el encuadre, y las dos primeras mal.** El checklist ocupa de la mitad
  de la pantalla a la derecha, así que un grupo centrado delante del aprendiz pierde lo que cae
  a su derecha: el **casco bueno** desaparecía entero. (1) Empujar el grupo a la izquierda sacó
  las botas por el otro borde. (2) Centrar sobre `cabinet (1)` metió el casco dentro de un
  armario alto: ese módulo abarca de x=−4.14 a −0.80 pero **su tapa útil no llega hasta ahí**,
  y el arte había dejado los EPP entre −2.22 y 2.58. Lo que funciona: **centrar el grupo en la
  mitad limpia** y **alejarse si no cabe**, las dos cosas derivadas del campo de visión
  (`HudClearFraction`, `HalfWidthPerMeter`). La fila de ropa mide 1.70 m y a 1.90 m no cabía.
- La aritmética vive en `StationLayout`, fuera del montador, y se afirma sin abrir una escena:
  13 tests. La captura es lo que caza lo demás.
- 329/331 EditMode y 4/4 PlayMode, dos corridas seguidas. Los 2 de siempre son del módulo 0.

### 2026-10-14 — el garaje v8 y el delineado de la pieza

Daniel entregó el v8 y, con él, **la e-bike despiezada**. Eso es lo que permitió por fin hacer lo
que la reunión del 12 había pedido: delinear la pieza que toca revisar en vez de marcarla con un
signo flotante.

- **El v8 entra prácticamente entero en sitio.** De sus 603 GUID, **595 ya estaban**, así que
  sobrescriben bajo `_Project/Models/CITY` sin duplicar carpeta. Solo 8 ficheros son nuevos
  —seis son la ropa del `E_BAKE`: `e_bike.mat`, sus tres mapas, el AO y `Pantalla.mat`— y caen
  en `Assets/CITY`, el layout viejo; se mueven con `AssetDatabase.MoveAsset`, que arrastra el
  `.meta` y conserva el GUID. Lo demás que cambia es, otra vez, **el horneado**: 4 lightmaps, 2
  sondas, el `LightingData` y la escena de arte.
- **`ebike.fbx` cambió conservando su GUID, y ahora viene en cuatro piezas**: `manubrio`,
  `marco`, `pedales` y `rueda_trasera`. Como el GUID es el mismo, el perfil y la instancia de la
  escena lo recogieron solos: los 5 mounts siguen ahí y no hubo que recablear nada. El
  `E_BAKE.fbx` que Daniel mandó aparte **sobra**: es el mismo archivo con otro nombre.
- **`manubrio` no es el manillar.** Es todo el tren delantero —0,66 × 1,11 × 0,76, con horquilla
  y rueda delantera—, y es el detalle del que cuelga medio diseño. Solo 2 de los 5 puntos tienen
  pieza propia (`point_steering`→`manubrio`, `point_wheels`→`rueda_trasera`); el freno vive
  dentro del manubrio, la batería dentro del marco y el faro no existe. Esos tres se quedan con
  el signo, **y eso es la decisión, no una carencia**: delinear el marco entero para decir
  «revisa el sistema eléctrico» enseña algo falso. El patinete sigue siendo malla única.
- **Dos intentos de hacer clicable la pieza, los dos tumbados por PlayMode.** Conviene no
  repetirlos:
  1. `MeshCollider` sobre la pieza más un reenviador de eventos. Exacto sobre el papel y
     **entierra dos puntos**: al hacerse opaco al raycast, el tren delantero tapa las esferas de
     `point_brakes` y `point_electrics`, que cuelgan por detrás. «Clic bloqueado por manubrio».
  2. Mudar la esfera del punto **al centro de la pieza**. La rueda se iba a su centro, que en la
     vista de llantas cae detrás del checklist: «clic bloqueado por Row · Frenos».
  Lo que funciona es mudarla **al punto de la pieza más cercano a donde ya estaba**
  (`Bounds.ClosestPoint`): si el mount ya caía dentro, no se mueve ni un milímetro, y el ajuste
  a ojo de sesiones anteriores se respeta. Sigue habiendo **un collider por punto**, como
  siempre: no se añade ninguno nuevo al arte.
- **El giro de la zona `wheels` pasó de 180° a 0°.** Con el signo daba igual —estaba colocado a
  mano en un hueco despejado—, pero el contorno va sobre la rueda de verdad, y a 180° la rueda
  trasera queda **detrás del checklist**. El test pasaba igual (el clic iba bien); lo cazó la
  captura. Cambiar el aviso cambió qué encuadres valen.
- **El contorno es un hijo con la misma malla, no un material añadido al renderer del arte.**
  Añadir un material a la lista de un renderer repite solo la *primera* submalla, y la bici tiene
  dos; además así cambiar de vehículo no deja nada que restaurar, se borra el hijo.
- **El shader está fuera del `CBUFFER` de `UnityPerMaterial` a propósito.** El SRP Batcher
  ignora los `MaterialPropertyBlock`: si alguien «arregla» eso metiendo las propiedades dentro,
  los cinco puntos se delinean del mismo color y el verde de resuelto deja de verse.
- 312/314 EditMode y 4/4 PlayMode, dos corridas seguidas. Los 2 que fallan son los de siempre
  del módulo 0 (título bilingüe y `Manrope-SemiBold`), que vienen de la rama del tutorial.

### 2026-10-12 (noche) — el garaje v7

Daniel entregó el v7 y se cambió igual que el v6, pero esta vez las reglas tuvieron que dejar de
depender de los nombres: un paquete nuevo reagrupa las cosas y una lista escrita a mano no
sobrevive a eso.

- **El v7 es, sobre todo, el horneado de luz.** De sus 596 GUID, 434 ya estaban y solo **34
  ficheros cambian**: 4 lightmaps, 2 sondas, el `LightingData` y `generateSecondaryUV` activado en
  `garage/interior.fbx`. Y nos entra gratis porque **nuestra escena ya referenciaba el mismo
  `LightingDataAsset`**: 330 de 423 renderers quedan con bake sin hornear nada aquí. Esto conviene
  no perderlo: si alguna vez se rompe esa referencia, se pierde la iluminación entera.
- **Qué es mundo ya no se decide por el nombre, sino por de qué está hecho.** El v7 reemplaza la
  raíz `enviroment` por tres prefabs y se lleva los maniquíes; con la regla vieja esas raíces no se
  reconocían y la escena se quedaba con **dos garajes encima**. Ahora: lo que son solo mallas de
  `_Project/Models` es mundo. Lo que no tiene mallas (un `Box Volume`, un `GameObject` vacío) ni se
  afirma ni se niega: se avisa.
- **Lo que se planta delante del mostrador se aparta buscando hueco.** El v7 pone seis prendas de
  pie justo delante de la estación de ropa. Tres mediciones, y las dos primeras estaban mal:
  apartar el **cuerpo** del aprendiz no basta (la captura seguía enseñando el mostrador tapado: lo
  que hay que dejar libre es por donde se **mira**), y **un solo rayo al centro** tampoco (pasaba a
  4 cm de la bufanda y daba la estación por despejada). Hay que comparar cajas. Y nada de esto se
  ve con física: el arte llega **sin colliders**, así que cualquier `Raycast` contesta «despejado».
  Además la primera colocación fue a ciegas, en fila a intervalos fijos, y cuatro de las seis
  acabaron dentro de la estantería: el hueco se **busca**.
- **Una regla nuestra se comía arte nuevo sin que nadie se enterara.** `RemoveStrayCopies` borraba
  toda copia `<modelo> (N)`; en el v7 eso son **dos e-bikes de adorno** colgadas de una balda. El
  módulo seguía pasando sus cuatro pruebas y la escena perdía arte recién entregado. Lo cacé
  **contando triángulos por raíz**, no mirando. Ahora solo borra lo que está a menos de 1,5 m del
  punto de exhibición, y dice en voz alta lo que deja estar.
- **Dos trampas del entorno, las dos caras:**
  - Un fichero editado **desde fuera** del Editor puede no verlo el `AssetDatabase.Refresh()` del
    relay. Tocarlo desde dentro (`File.WriteAllText` del mismo contenido + `ImportAsset`) sí lo
    despierta.
  - Y sobre todo: **el relay no avisa de que el proyecto no compila**. Me cargué un helper al
    reemplazar un método, el ensamblado llevaba media hora sin construirse, y cada orden seguía
    corriendo contra el dll viejo con `isCompilationSuccessful: true`. Hay que mirar
    `Logs/Editor.log` buscando `error CS`, no fiarse de `isCompiling`.
- **611.411 triángulos**, no baja: se van los maniquíes (−150.000) pero entra más decorado. El
  39 % es `barrio_exterior`, el vecindario que solo se ve por una ventana.
- **El v7 trae seis prendas que parecen pensadas como EPP** (bufanda, jean, tapabocas, mascada,
  guantes). No están en el catálogo. Añadirlas cambia las respuestas correctas del ejercicio, así
  que es decisión del cliente, no mía.

### 2026-10-12 — el garaje v6 y las tareas de la reunión

Dos cosas a la vez: cambiar el mundo por el del paquete v6 y aplicar las ~40 tareas del acta.
El detalle de lo que quedó pendiente está en `Docs/Pendientes_Reunion_2026-10-12.md`; aquí lo
que conviene recordar.

- **El v6 no venía en la rama del tutorial.** Ese merge trajo código del tutorial, los prefabs
  de los Touch Plus y los atlas de fuentes; cero modelos. El paquete estaba en Descargas.
- **El paquete trae el layout viejo** (`Assets/CITY`), así que 365 de sus 436 GUID sobrescriben
  en sitio y 71 caían fuera de `_Project`. Esos 71 se movieron con su `.meta` al lado, que es
  lo que conserva el GUID.
- **El constructor de escena quedó obsoleto en el momento justo:** v6 agrupó los 7 EPP bajo una
  raíz nueva `epps`, y la lista de nombres escrita a mano ya no los encontraba. De ahí
  `Module01WorldSwap`, que decide qué es mundo preguntándole a la escena de arte en vez de a una
  lista, y contra **toda** su jerarquía: `luz grontal`, `luz roja 1` y los dos `plataform`
  dejaron de ser raíz y comparando solo raíces se quedaban duplicados.
- **v6 trae los EPP casi bien, y eso solo se ve midiendo.** Apoyados en el mostrador y cada uno
  a su altura —mucho mejor que la fila flotante que montaba el presentador—, pero `casco 1`
  tenía la base **16 cm hundida** y otros dos un par de centímetros. Y estaban desparramados:
  la zona `head` ocupaba 4,99 m. La altura de la tapa se deriva ahora de la **mediana** de las
  bases, no de un número escrito: con un objeto hundido, la media se lleva por delante a los
  que estaban bien.
- **Lo que más tiempo costó no fue escribir código, fue un diálogo modal.** Dos veces. Ver §9.
- **Cinco bugs los cazaron los tests y las capturas, no la vista:** el checklist comiéndose el
  popup y luego la navegación, el punto clicable mientras el vehículo aún giraba, el marcador de
  llantas debajo del checklist, el popup **dentro** del armario, y la leyenda en inglés porque
  al renombrar sus objetos el binding —que localiza por nombre— dejó de encontrarlos y callaba.
  Ese último ahora avisa.
- **Tres tests de PlayMode codificaban el comportamiento viejo** y se actualizaron para afirmar
  el nuevo: que el video no se puede saltar, que en las zonas del vehículo el aprendiz no viaja,
  y que por eso hay 4 fundidos y no 6.
- **670.903 triángulos**, casi el doble del presupuesto de Quest. El 39 % se lo llevan tres
  maniquíes en pose T y 22 troncos de árbol, y ninguna de las dos cosas es contenido del módulo.
- 257 EditMode y 4 PlayMode en verde, PlayMode dos corridas seguidas.

**Tanda de la tarde: una sola UI de seleccion y un medidor de legibilidad.**

- El reporte queda en **una sola lista** («Mi Elección»). La segunda tabla, la «referencia para
  este recorrido», se calculaba desde las mismas filas cambiando los estados: no aportaba
  información y duplicaba doce renglones en una pantalla que ya iba justa.
- **Las leyendas se leen, ya no se adivinan.** Cada fila lleva su estado escrito en su propia
  columna a la derecha, no solo el icono de color; y el id crudo (`helmet_certified`) dejó de
  asomar en la columna de detalle.
- **El veredicto del apto / no apto se mudó al panel.** Vivía en un rótulo suelto del HUD y se
  cruzaba con «Ver explicación». Y no bastaba con apagarlo al abrir el reporte: `Decide()`
  avanzaba de zona —lo que abre el reporte— **antes** de pintarlo, así que se encendía después.
  Ahora el informe llega por el evento de sección completada y la frase se compone con la nota.
- **El medidor de textos.** Esto es lo que conviene no perder: el recorrido de PlayMode ahora
  mide, en cada pantalla, que ningún texto se salga de su caja, que el autoajuste no se haya
  hundido hasta lo ilegible, y que **ningún par de textos se pise**. Encontró tres solapes que
  mirando la pantalla no se veían.
  - Compara **el polígono real del glifo** (`textBounds`), no el `RectTransform`: un rect ancho
    con dos palabras cortas se cruza con el vecino sin que las letras se toquen.
  - Y lo compara con el **teorema del eje separador**, no con cajas alineadas: la UI va rotada
    en 3D por el `GazeFollower`, y las cajas alineadas a los ejes daban falso positivo en casi
    todo. Tres errores de medición míos, en fila, antes de que el medidor dijera la verdad.
- Los marcadores `!` ya no se acumulan: el verde de una zona resuelta no se apagaba nunca y, al
  girar el vehículo, acababa cruzándose con el punto que toca mirar ahora.
- **Los 2 EditMode que fallan no son de aquí:** `PF_Module00_UI` trae componentes `Localize` con
  `LocalizeOnAwake`, que reescriben el título bilingüe del panel de idioma —y el test del módulo
  0 avisa de eso textualmente—. Es trabajo en curso de la rama del tutorial; no se tocó.
- 286/288 EditMode y 4/4 PlayMode.

### 2026-10-07 (noche) — tanda de UI sobre lo que Charlie vio corriendo

Charlie recorrió el módulo entero y mandó doce capturas. Salieron once problemas, y **dos
causas explicaban la mitad**.

- **La UI la atravesaba el 3D.** Todos los canvas del módulo 1 son world space, así que se
  ordenaban por profundidad junto con la geometría: el video salía detrás de un objeto y las
  esferas del vehículo perforaban el reporte. La capa `UI` pasa a dibujarla una **cámara de
  overlay en el stack de URP**, que comparte transform con la principal. Elegido así y no con
  `ScreenSpaceOverlay` porque esto también vale en VR. Verificado con captura real.
  **Falta:** el HUD se quedó en `Default` — la cámara se crea al principio del builder y el HUD
  se construye después, así que la asignación de capa no lo alcanza. Moverla al final disparó
  otra regresión (la fuente del `BeginButton`) y se revirtió.
- **La lista lateral partía el texto en trozos de seis letras.** Medido, no supuesto: la
  plantilla de fila mide 100×100 y la etiqueta se estira con 44 por la izquierda y 90 por la
  derecha → **−34 de ancho**. En runtime el binding forzaba el panel a 250, la fila quedaba en
  188 y la etiqueta en 54 px. Panel a 420 y corrido a la derecha. Arreglado y verificado.
- **La leyenda del reporte** estaba escrita a mano en inglés en `UIModules.Module01.cs` y nunca
  pasaba por I2. Ahora se localiza por nombre.
- **El vehículo giratorio**: cada zona declara su giro en `VehicleProfileSO`, por vehículo, y el
  director deja de teletransportar en la sección de vehículo. Queda **tras una guarda**: solo
  actúa si la escena tiene el ancla `vehicle`, que aún no existe.
- Cámara inicial bajada de 1.65 a 1.45.
- **Plantilla de guion para el cliente** en `Docs/Guion_Modulo1_RideSafe_PLANTILLA.docx`, con la
  estructura del guion de Excavaciones y cada hueco atado a su clave de I2.

**Dos errores míos que costaron tiempo y conviene no repetir:**
1. Diagnostiqué el ancho de la lista leyendo **píxeles de pantalla en vez de unidades de
   canvas**, concluí que el `sizeDelta` del binding no se aplicaba y **reverté el arreglo que
   era correcto**. El volcado de rects lo resolvió en un minuto.
2. `Capture()` solo comprobaba que el PNG existiera: si el borrado fallaba daba verde sobre la
   captura de la corrida anterior. Estuve mirando evidencia caducada. Ahora exige escritura
   nueva.

### 2026-10-07 (tarde, 3) — I2: el export estaba a medias y la importación salió mal

- **El export cubría 38 claves de 61.** Las 23 que faltaban son la orientación, la lista
  lateral, el reporte y los botones: viven en `PresentationTexts.txt`, no en código, y por eso
  la lista hecha a mano no las vio. Ahora el export sale del asset por la API de I2.
- Subida a Drive la hoja completa, `RideSafe Module1 terminos COMPLETO (desde I2) 2026-10-07`.
  La hoja original `I2Loc RideSafe Localization` **no está en este Drive**, ni propia ni
  compartida, así que no se pudo alinear.
- **La importación de vuelta rompió la categoría entera**: prefijo duplicado en las 61 e inglés
  retraducido desde el español en 36. Reparado desde el export commiteado; el asset volvió al
  byte con `git checkout` para no meter 650 líneas de ruido en un archivo que luego hay que
  fusionar con el trabajo de Cindy. Las dos trampas quedaron escritas en §9.
- 145 EditMode y 3 PlayMode en verde después de la reparación.

### 2026-10-07 (tarde, 2) — decisiones de contenido de Charlie

- **El scooter se congela** hasta nuevo aviso. El tramo 1 queda entregado; el 2 no se empieza.
- **El casco estaba al revés.** El bueno es `df_g_helmet_01`. Intercambiados modelo y etiqueta
  de escena para `helmet_ok` / `helmet_damaged`.
- **Ropa suelta apagada** por falta de modelo. La zona `clothing` se queda sin respuesta
  equivocada: declarado como excepción con motivo en `Module01DataTests.ZonesWithoutRisk`.
- **Logo y tagline descartados**: el logo vive en la puerta del garaje.
- Voz y videos esperan a que los textos queden bien en el Drive.
- **`Module01SceneEntities`**: etiquetar entidades, crear las que falten y podar huérfanas, ahora
  sobre la escena abierta. Era el primer trozo de «partir el constructor destructivo», y es lo
  que permitió cambiar los cascos sin regenerar la escena. Lo usará también el garaje v6.
- 145 EditMode y 3 PlayMode en verde tras todo esto.

### 2026-10-07 (tarde) — el patinete empieza a significar algo

- **Tramo 1 del scooter, con TDD.** 9 tests nuevos de EditMode sobre la selección de vehículo y
  el escenario, 3 sobre los assets, 1 de PlayMode que entra pulsando Begin con `escooter` y
  afirma que el director usa su perfil, que el patinete se ve y que la bici queda apagada. El
  test de la e-bike afirma lo simétrico. **145 EditMode y 3 PlayMode en verde**, PlayMode dos
  corridas seguidas.
- Los dos perfiles comparten las 3 `ZoneSO`: **cero términos nuevos de I2**, como se decidió.
- Los `SequenceId` del vehículo ahora llevan el vehículo, por el estado compartido del runner.
- **Me cargué la escena y la restauré.** Corrí `Montar escena jugable`, que no edita v5 sino que
  la regenera desde la escena de lógica: se perdieron 28 GameObjects y toda la presentación del
  2026-10-07. `git checkout` del `.unity` y el cableado rehecho con un comando nuevo que trabaja
  sobre la escena abierta. La trampa quedó escrita en §9 y en el comentario de la herramienta.

### 2026-10-07 — la presentación del módulo 1, con la marca del módulo 0

- **Toda la presentación de escena pasa por el kit del atlas.** Los 7 botones (popup,
  navegación, go/no-go) son pastillas `Button_Primary/Secondary_*` con Hover y Pressed, en
  Inter-SemiBold; los paneles son `Panel_Modal_Dark` / `Panel_Modal_Light` 9-sliced. Antes eran
  `Image` de color plano con la fuente por defecto de TMP (que en este proyecto es **AmmoText**,
  no LiberationSans).
- `Module01PresentationBuilder` **movido** a `Module01/Scene/Editor/` para alcanzar el kit;
  `RideSafeUIPrefabBuilder.CreateKit()` extraído para compartir atlas y fuentes.
- El popup se agranda en px en vez de escalar su canvas (está a 0.0015 y el kit a 0.002), así el
  `ItemStage` y el pedestal no se mueven. Los botones del kit se adaptan solos; los paneles no,
  su multiplier va dividido.
- **Arreglado el 3D encima del popup**: el pedestal bajó a 26 cm de diámetro y el popup se
  deriva del borde proyectado del disco. Falló el test con el bug (440 > 385) y pasa con el
  arreglo. **Queda abierto**: el popup termina a −28.3° con el borde de pantalla en −30°; la
  salida medida es alejar el montaje de 1.12/1.15 m a ~1.40 m.
- Los tres botones del go/no-go quedaron **secundarios**: en una evaluación, un pill coral en
  `RoadReady` sugiere la respuesta antes de que el aprendiz lea el texto.
- Tests nuevos que guardan las reglas: `AssertOnBrand` (todo botón con pastilla, estados y
  fuente del kit) y `AssertPedestalClearsPopup`, más que la pregunta nunca salga vacía ni
  empiece por `Module1/`.

### 2026-10-06 — presentación conectada y módulo 0 parametrizable

- **Borrada la segunda copia de `PF_Module00_UI`**, que venía desde `faee601` y hacía que el
  Welcome no se apagara al pulsar Begin. Guard `RequireUnique` en el builder.
- Panel Welcome con solo el botón Begin: el logo vive en la puerta del garaje.
- `Module00SettingsSO` + `SO_Module00_TestSettings` (es-CO / bogota / ebike / subtítulos on).
  Con el asset en `Module01Experience._quickStart`, Begin entra directo al módulo 1; sin él
  corre el onboarding completo. Se acabaron los `"es-CO"` y `"ebike"` escritos en código.
- **Vehicle y Jurisdiction ya publican la elección en `Module01Context`**; antes se tiraban
  con `_ =>` y solo `StartWithDefaults` ponía `vehicle`. Elegir escooter en el módulo 0 corría
  las secuencias de la e-bike.
- `VideoSurface.texture` cableada de verdad, y el video se prepara antes de arrancar.
- El test de PlayMode entra **pulsando Begin** y exige que quede un solo UI de onboarding.
- Por la tarde: diseñado el scooter (§10). Sin aprobar y sin empezar. De paso quedó
  desmentido el «cero líneas de código» de la spec, y corregido lo de los 5 puntos
  «sobre el ebike real», que nunca estuvieron sobre nada.

### 2026-10-05 / 06

- Rama `dev` creada; la reestructura `DriveSafe → _Project` commiteada en 5 commits.
- Merge de `main`: integra TaskSequence y el tutorial de Cindy. `PF_Module00_UI` fusionado
  con UnityYAMLMerge salió corrupto; se tomó la versión de `main` entera.
- Arregladas las rutas del generador de prefabs: cierra el pendiente del "generador roto".
- Módulo 1 construido con TDD en 11 tareas: catálogo, graduador, entidad 3D, binding,
  contexto, inspector, recorrido, reporte, greybox, secuencias, go/no-go y director.
- Importado `garage v5.unitypackage` sin duplicar carpetas.
- Cambiados los 7 elementos con modelo real; perfil apuntando a `ebike.fbx`.
- Montada `RideSafe_Garaje_v5` con un script idempotente.
- 2 tests de PlayMode sobre la escena real: el módulo recorre las 6 zonas y reporta.
- Auditado el cableado de presentación: nada de lo visible está conectado.

### 2026-09-22

- Revisión de escena, navegación de módulos 00 y 01, 35 textos con I2, XR para Link,
  subtítulos con revelado por palabra, narración, pantalla de video, modos VR/PC,
  MeshColliders del garaje.
