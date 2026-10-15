# RideSafe — estado del proyecto

> Documento vivo. Se actualiza a medida que avanza el trabajo.
> Última actualización: 2026-10-14
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

- **`main`** — rama compartida. Cindy trabaja aquí.
- **`dev`** — reestructura `DriveSafe → _Project` y módulo 1 completo en lógica. Pusheada.
- **`feat/garaje-v5`** — `dev` + el paquete de arte de 1.7 GB + la escena nueva. **Sin mezclar.**
- **`feat/garaje-v6`** — la rama viva. Sale de `feat/garaje-v5` tras el merge del tutorial, e
  incluye el garaje v6 y las tareas de la reunión del 2026-10-12. **Sin mezclar.**

El merge de `main` en `dev` ya se hizo: integra el tutorial y TaskSequence de Cindy.
**Avisar a Cindy antes de integrar `feat/garaje-v6`**, porque ella trabaja sobre
`RideSafe_Garaje.unity`.

**La escena sigue llamándose `RideSafe_Garaje_v5.unity` pero su mundo ya es el v6.** El nombre
se conservó a propósito: cambiarlo habría roto las referencias de la lista de build, el montador
de presentación y los tests, a cambio de nada.

---

## 4. Qué funciona hoy

### Módulo 0 — verificado corriendo
Navegación completa, 35 textos localizados, cambio de idioma en vivo, narración con
subtítulos palabra por palabra, video final, colliders del garaje.

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

`Assets/_Project/Tools/Editor/HeadlessTestRunner.cs` corre los tests sin abrir la ventana;
deja el resultado en `Temp/ridesafe-tests.txt`.

---

## 6. Contenido

### Catálogo de EPP — 11 elementos en 3 zonas

| Zona | Elemento | Categoría | Modelo |
|---|---|---|---|
| head | Casco apto | Núcleo | `df_g_helmet_01.fbx` |
| head | Casco no apto | Inapropiado | `casco 1.fbx` |
| head | Gafas | Opcional | `gafas.fbx` |
| clothing | Calzado | Núcleo | `botas.fbx` |
| clothing | Chaleco reflectivo | Según condición | `chaleco refectivo.fbx` |
| clothing | Ropa o cordones sueltos | Inapropiado | `bufanda 01.fbx` |
| clothing | Pantalón ajustado | Núcleo | `jean ajustado.fbx` |
| clothing | Pantalón ancho | Inapropiado | `pantalon ancho.fbx` |
| clothing | Pantalón deportivo | Opcional | `pant low.fbx` |
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

- [ ] **Los 3 términos de I2 de los pantalones.** Hoy el panel enseña la clave cruda
      (`Module1/Item_TrousersFitted`). Son `Module1/Item_TrousersFitted`, `_TrousersLoose` y
      `_TrousersTech`, en inglés y español. Los pone Charlie en el Drive; el test de PlayMode
      avisa cuando estén para vaciar `Module01SliceTests.SinTraducirTodavia`.
- [ ] **Pedirle a Daniel las piezas que faltan de la e-bike**: maneta de freno, batería y faro.
      Cada una que llegue es una fila más en `Module01VehicleStaging.Parts`, sin tocar código, y
      un `!` menos. ~~El patinete despiezado~~ **llegó el 2026-10-14**, y con él la maneta de
      freno que la bici no tiene. Del patinete faltaría el faro; la batería va dentro de la
      plataforma y seguramente no tiene sentido sacarla.
- [ ] **Pantalón reflectivo o corta-viento**, que pidió Charlie. No existe modelo. Entrará como
      «según condición», igual que el chaleco. **No bloquea esta build**: `jean ajustado` ya es
      el pantalón correcto (núcleo) y la zona ya enseña algo sin él.
- [ ] **Pushear `feat/garaje-v6`**: lleva 3 commits propios sin subir.
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
- [ ] Panel Vehicle del módulo 0: título y subtítulo se solapan, y la barra de confirmación
      se pisa consigo misma.
- [ ] Integrar `feat/garaje-v6` en `dev` (avisar a Cindy)
- [ ] Rehornear luz y occlusion en la escena; el horneado es por escena y no viaja con el
      trasplante del mundo
- [ ] **Reponer colliders: ni los armarios ni los maniquíes del v6 tienen ninguno.** Además de
      lo obvio, esto hace que toda comprobación de oclusión por raycast mienta (ver §9)
- [ ] **670.903 triángulos**, casi el doble del presupuesto de Quest. El 39 % son tres maniquíes
      en pose T y 22 troncos de árbol
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
