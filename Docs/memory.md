# RideSafe — estado del proyecto

> Documento vivo. Se actualiza a medida que avanza el trabajo.
> Última actualización: 2026-10-07 (tarde)

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

El merge de `main` en `dev` ya se hizo: integra el tutorial y TaskSequence de Cindy.
**Avisarle antes de integrar `feat/garaje-v5`**, porque ella trabaja sobre `RideSafe_Garaje.unity`.

---

## 4. Qué funciona hoy

### Módulo 0 — verificado corriendo
Navegación completa, 35 textos localizados, cambio de idioma en vivo, narración con
subtítulos palabra por palabra, video final, colliders del garaje.

### Módulo 1 — lógica y presentación, las dos probadas corriendo

Los 3 tests de PlayMode corren sobre la escena real y **entran pulsando Begin**, no por atajo.
Verdes dos corridas seguidas el 2026-10-07, junto con 145 de EditMode.

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

Los tres recorren el **módulo 0 real** (Language → Jurisdiction → Vehicle → Comfort) y ninguno
escribe en la escena: el interruptor es `Module01Experience._quickStart`, que hoy va **vacío**
para que al abrir la escena a mano se vean las cinco pantallas. Si alguien se lo asigna, los
tests se marcan *ignorados* diciendo por qué, en vez de fallar sin explicación.

- `El_modulo_recorre...` — recorrido completo con subtítulos encendidos
- `Rechazar_el_casco...` — rechaza el casco bueno y lo reporta como omisión crítica
- `Elegir_patinete...` — pulsa la tarjeta del patinete y afirma que el director usa ese perfil,
  que el patinete se ve y que la bici queda apagada

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
| `VehicleProfileSO` | vehículo, su modelo y sus zonas de inspección |
| `SelectionLedger` / `SelectionReport` / `SelectionGrader` | puntuación pura, sin Unity |
| `ReadinessDecision` | go/no-go contra lo encontrado |
| `WorldObjectTaskEntity` | entidad 3D vía EventSystem; sirve con mouse y con AutoHand |
| `Module01Binding` | enlaza `PF_Module01_UI` por nombre, con `FindMissing` |
| `ReportPresenter` | pinta el reporte, repinta al cambiar idioma |
| `ItemInspector` | presenta el objeto y confirma |
| `ZoneRunner` | conecta entidad, inspector y libro por zona |
| `GuidedTour` | fade y reposición del rig |
| `VehicleStage` | enciende la malla del vehículo elegido y apaga la otra |
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

### Catálogo de EPP — 7 elementos en 3 zonas

| Zona | Elemento | Categoría | Modelo |
|---|---|---|---|
| head | Casco apto | Núcleo | `df_g_helmet_01.fbx` |
| head | Casco no apto | Inapropiado | `casco 1.fbx` |
| head | Gafas | Opcional | `gafas.fbx` |
| clothing | Calzado | Núcleo | `botas.fbx` |
| clothing | Chaleco reflectivo | Según condición | `chaleco refectivo.fbx` |
| load | Carga asegurada | Núcleo | `guaya candado.fbx` |
| load | Objeto en mano | Inapropiado | `MobilePhone_01.fbx` |

**Cuál casco es cuál lo confirmó Charlie el 2026-10-07: el bueno es `df_g_helmet_01`.** Hasta
entonces estaba al revés y el módulo enseñaba lo contrario. La zona `clothing` se quedó sin
elemento de riesgo al apagar la ropa suelta; ver §8.

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

- [x] ~~**El scooter**~~ — **CONGELADO el 2026-10-07 por decisión de Charlie, hasta nuevo aviso.**
      El tramo 1 queda hecho y afirmado —elegir patinete usa su perfil, se ve el patinete y la
      bici queda apagada—. Lo que no se empieza es el tramo 2: punto único de exhibición, anclas en órbita y los 5 puntos colgando de
      cada malla. Ver §10.
      **Ojo:** sus 3 `TaskSequenceSO` existen y filtran bien por `vehicle == escooter`, pero
      —igual que las de la e-bike— **siguen sin pasos y nadie las ejecuta**: el módulo lo mueve
      `Module01Director`, no `TaskSequence`. Eso sigue pendiente para los dos vehículos.
- [x] ~~Volver a encender `LogoPlate` y `Tagline`~~ — **descartado el 2026-10-07.** El logo vive
      en la puerta del garaje y Charlie no piensa volver a encenderlos. Siguen apagados como
      override de la instancia en la escena.

### Contenido

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

- [ ] **Partir `Module01SceneBuilder` en comandos no destructivos**, como se hizo con
      `Module01VehicleWiring`. Hoy etiquetar entidades, crear las que faltan y colocar anclas
      solo se puede haciendo `Build()`, que regenera la escena y borra la presentación. Bloquea
      añadir EPP nuevos y recibir el garaje v6 sin rehacerlo todo.
- [ ] Integrar `feat/garaje-v5` en `dev` (avisar a Cindy)
- [ ] Rehornear luz y occlusion en la escena nueva
- [ ] Reponer MeshColliders si el entorno cambió
- [ ] 370k triángulos sigue por encima del presupuesto de Quest
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
| `ebike` | `(-0.99, 0.04, -0.27)`, bounds 0.66 × 1.11 × 1.97, **malla única sin hijos** |
| `scooter` | `(1.70, 0.06, -0.30)`, bounds 0.55 × 1.35 × 1.27, **malla única sin hijos** |
| `scooter.fbx` | `Assets/_Project/Models/CITY/MODELADOS 3d/scooter.fbx` |
| anclas EPP | `z=-1.70`, x = -2 / 0 / 2 |
| anclas vehículo | `z=2.30`, x = -2 / -0.5 / 1, todas mirando a 180° |
| los 5 puntos | esferas en fila en `y=1.30, z=0.80`, bajo `Module01_Spawned`, **sin tocar ningún vehículo** |

Los ids de entidad son `module01.{zoneId}.{itemId}`: **no llevan vehículo**, así que una sola
entidad sirve a los dos. Por eso ambos perfiles pueden apuntar a las mismas 3 zonas.

---

## 11. Bitácora

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
