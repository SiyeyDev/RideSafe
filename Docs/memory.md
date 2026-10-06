# RideSafe — estado del proyecto

> Documento vivo. Se actualiza a medida que avanza el trabajo.
> Última actualización: 2026-09-22

---

## 1. Qué es

Experiencia VR de formación en movilidad (bicicleta eléctrica / patinete) para **Meta Quest 2**.
Hoy se prueba por **Oculus Link** desde el Editor; el build de Android todavía no es el objetivo.

- Unity **6000.5.4f1**, **URP**, AutoHand para manos e interacción, **I2 Localization** para textos.
- Escena principal: `Assets/_Project/Scene/RideSafe_Garaje.unity`
- Idiomas: **English (en)** y **Español (es-CO)**.

---

## 2. Qué funciona hoy

Verificado corriendo en Play (modo PC), no solo revisado en el inspector:

| Pieza | Estado |
|---|---|
| Navegación del módulo 0 | ✅ Welcome → Language → Jurisdiction → Vehicle → Comfort, con sus Back |
| Localización I2 del módulo 0 | ✅ 35 textos, cambio de idioma en vivo |
| Cambio de idioma desde el menú | ✅ marcar "Español" cambia I2 a es-CO al instante |
| Línea de confirmación | ✅ "E-bike Seleccionado" (término + nombre de opción localizados) |
| Switch On/Off de subtítulos | ✅ localizado |
| Narración | ✅ `M01_Welcome` → `M01_Video` en orden |
| Subtítulos | ✅ se escriben palabra por palabra al ritmo del audio |
| Video final | ✅ arranca al terminar la voz y se apaga solo a los 10 s |
| Tamaño de texto del slider | ✅ llega al subtítulo |
| Piso del garaje | ✅ el jugador ya no se cae |

**Sin verificar todavía:** el rayo de las manos con controles reales. Necesita las gafas puestas.

---

## 3. Cómo correr

Menú **`RideSafe > Demo`**:

- **`Modo VR (gafas)`** — activa el XRPlayer. Abre el Link y dale Play.
- **`Modo PC (mouse)`** — apaga el XRPlayer, activa una cámara fija + EventSystem. Se maneja con
  el mouse. La cámara no gira: sirve para demo de UI, no para mirar alrededor.

Los dos modos **no pueden convivir**: el `HandCanvasPointer` de AutoHand le asigna su propia cámara
a todos los canvas world-space de la escena, y le robaría la puntería al mouse.

---

## 4. Mapa de la escena

```
RideSafe_Garaje
├── Main Camera ................. apagada (sobraba en VR: 2º AudioListener + render extra)
├── Directional Light / luces ... 7 Area Lights baked + 3 lightmaps
├── enviroment .................. garaje; suelo/paredes/puertas con MeshCollider
├── Ch20_nonPBR ................. personaje, apagado
├── XRPlayer .................... rig de AutoHand (trigger = click, grip = agarrar)
├── DesktopMode ................. cámara fija + EventSystem + DesktopCursor (modo PC)
├── PF_Module00_UI .............. menú de configuración (activo al arrancar)
├── PF_Module01_UI .............. módulo 1, apagado y sin localizar todavía
├── PF_Subtitles ................ se enciende solo mientras hay voz
├── PF_VideoScreen .............. se enciende solo mientras hay video
└── Narration ................... AudioSource + NarrationPlayer
```

**Cadena del módulo 0:** `Comenzar Módulo 1` → apaga `PF_Module00_UI` + `NarrationPlayer.Play()`
→ voz con subtítulos → al terminar → `VideoScreen.Play()` → el video se apaga solo.

---

## 5. Código propio

### `Assets/_Project/UI/Runtime/` (asmdef `RideSafe.UI`)
| Script | Qué hace |
|---|---|
| `ModuleUI` | raíz de un módulo; `Show(panelId)` enseña un panel y esconde el resto |
| `ChoicePanel` | panel de opciones con toggles, Continue bloqueado hasta elegir |
| `ChecklistView`, `ComparisonView`, `ExplanationView`, `CardView`, `ProgressView` | vistas del módulo 1 |
| `ComfortSettingsPanel` | postura, subtítulos, tamaño de texto, mano dominante |
| `SelectableStyle` | estados visuales de botones y toggles |
| `SubtitleView` | revela el texto siguiendo los tiempos por palabra del clip |
| `GazeFollower` | lazy follow: quieto mientras lo estés mirando, vuelve si te giras |
| `VideoScreen` | pantalla de video que solo existe mientras reproduce |
| `ComfortSettingsBinder` | pasa las opciones de confort al subtítulo |

### `Assets/_Project/Localization/` y `Demo/` (sin asmdef, van a Assembly-CSharp)
| Script | Qué hace |
|---|---|
| `LanguageChoiceBinder` | el panel de idioma cambia el idioma de I2 |
| `MenuRuntimeTextLocalizer` | localiza los textos que el código escribe en runtime |
| `DesktopCursor` | muestra el puntero mientras el rig de PC está activo |

### `Assets/_Project/Narration/`
| Script | Qué hace |
|---|---|
| `NarrationPlayer` | reproduce las líneas en orden y mueve los subtítulos con el reloj del clip |

---

## 6. Contenido y datos

### Audio + subtítulos
`Resources/Sounds/<módulo>/<idioma>/<key>` — pareja **`.mp3` + `.json`**, generada por tu herramienta
de audio. El JSON trae **tiempos por palabra**, que es lo que permite que el texto caiga sobre la voz.
El idioma se resuelve desde I2 en runtime, así que el menú de idioma también cambia la voz. Si falta
la carpeta de un idioma, avisa y cae al de respaldo en vez de quedarse mudo.

### Localización
Fuente: `Assets/Resources/I2Languages.asset`, categoría `Menu`.

- Los textos fijos van con componente `Localize`.
- Los que **escribe el código** (línea de confirmación, On/Off del switch) no pueden llevar `Localize`
  porque la vista los pisaría: van por `MenuRuntimeTextLocalizer`.

---

## 7. Pendientes

### Contenido
- [ ] **Términos que faltan** en el módulo 0: `Prompt` ("confirm to continue?"), `Change`, `Confirm`
      de la barra de Vehicle; el tagline y el botón de Welcome; el body del panel Language.
- [ ] `Menu_SubtitulosTitle` tiene "Subtitles" también en la columna española.
- [ ] **`Menu_StartCountry` / `Menu_Country1` / `Menu_Country2` no se usan**: no hay panel de país
      en la UI. Decidir si falta crearlo o si esos términos sobran.
- [ ] Término vacío `Menu/` en la fuente de I2 (fila de más en el Excel).
- [ ] **Módulo 1 sin localizar**: 74 textos esperando términos.

### Técnico
- [ ] **El generador de prefabs está roto**: `RideSafeUIPrefabBuilder` apunta a `Assets/DriveSafe/UI/...`
      y esa carpeta ahora es `_Project`. Hay que arreglar las rutas antes de volver a usarlo.
- [ ] La escena y `_Project` **no están commiteados** en git.
- [ ] `RideSafe_Garaje` no está en Build Settings (solo `SampleScene`).
- [ ] Ningún nivel de calidad asigna RP asset: en Android se usaría `PC_RPAsset`. Existe
      `Mobile_RPAsset` sin asignar. MSAA en None en ambos.
- [ ] ~648k triángulos visibles. Presupuesto típico de Quest standalone: 150–350k.
- [ ] Lógica de negocio: los módulos navegan, pero nada guarda las decisiones del usuario todavía.
      Los sistemas `TaskSequence` / `CommandSystem` siguen sin conectarse.

---

## 8. Trampas conocidas

- **Regenerar los prefabs borra el cableado a mano.** Toda la navegación y los `Localize` viven en
  los prefabs; `RideSafeUIPrefabBuilder` los recrea desde cero. Avisar antes de regenerar.
- **I2 no trae asmdef**, así que compila en Assembly-CSharp. Un asmdef no puede referenciar la
  assembly predefinida, o sea que `RideSafe.UI` **no puede usar `I2.Loc`**. Todo puente con I2 tiene
  que vivir en una carpeta sin asmdef.
- **`UnityEvent<T>` genérico no se serializa.** Por eso `ChoicePanel.onContinue` y
  `ComfortSettingsPanel.onStart` no aparecen en el Inspector y hay que cablearlos por código. Los
  no genéricos (`onBack`, `Button.onClick`, `NarrationPlayer.onFinished`) sí.
- **`CursorManager` esconde el cursor en cada arranque** desde un `RuntimeInitializeOnLoadMethod`.
  Correcto en VR, fatal en modo PC: por eso existe `DesktopCursor`.
- **El proyecto está en Input System nuevo únicamente.** El modo escritorio de AutoHand
  (`HandDesktopControllerLink`, `KeyboardHand`) usa la API vieja de `Input` y tiraría excepciones.
- **Los atlas de UI necesitan Mesh Type: Full Rect** para que el 9-slice no se deforme. Ya están así.
- **Errores de Odin en consola** (`MissingFieldException: UIElementsUtility...`): son del Editor
  dibujando inspectores, no del runtime. Vienen de antes y no bloquean Play.

---

## 9. Bitácora

### 2026-09-22
- Revisión completa de la escena: 0 scripts faltantes, consola limpia.
- Cableada la navegación de los módulos 00 y 01 (15 transiciones) dentro de los prefabs.
- Conectados 35 textos del módulo 0 con I2; el panel de idioma ahora cambia el idioma de verdad.
- Localizados los textos que escribe el código: línea de confirmación y On/Off del switch.
- XR configurado para PC (OpenXR + perfiles Meta Touch) para poder probar por Link.
- Creados los subtítulos (`PF_Subtitles`) con revelado por palabra y lazy follow.
- Creada la narración (`Narration`) que encadena `M01_Welcome` → `M01_Video`.
- Creada la pantalla de video (`PF_VideoScreen`), que arranca al terminar la voz.
- Añadidos los modos de demo VR / PC y el cursor de escritorio.
- Puestos MeshColliders al suelo, paredes y puertas del garaje: el jugador ya no se cae.
- Corregido: el tamaño de texto del slider no llegaba al subtítulo (el auto-sizing de TMP lo pisaba).
- Corregido: el video se precarga al arrancar la narración, entra sin espera.
