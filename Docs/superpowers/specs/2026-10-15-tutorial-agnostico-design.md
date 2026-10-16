# Tutorial agnóstico: ayuda de controles para cualquier módulo

**Fecha:** 2026-10-15 · **Escena objetivo:** `Assets/_Project/Scene/RideSafe_Garaje_v5.unity` · **Estado:** diseño aprobado en conversación, pendiente de revisión escrita

## 1. Qué es y qué no es

El tutorial es un **apoyo**: cuando el usuario necesita saber *cómo* interactuar, muestra el control
Touch Plus con el botón que debe presionar, resalta dónde actuar y, solo la primera vez, una frase corta.

- **Agnóstico a los módulos.** No tiene pantallas propias ni secuencias de pasos, y no conoce el flujo
  de ningún módulo. Solo conoce **gestos** (p. ej. «seleccionar con el gatillo»).
- **Los módulos no se modifican.** Ni el módulo 0 ni el módulo 1 cambian por existir el tutorial. Un
  módulo *puede* llamarlo con una línea cuando algo no se detecta desde fuera, pero no lo necesita.
- **Se rehace desde cero.** Del tutorial actual (`Assets/_Project/Systems/Tutorial`, basado en
  `TaskSequence`) solo se reutilizan los modelos Touch Plus y sus poses. Ese código no se borra en este
  trabajo (Garaje_Test depende de él); su retirada es una limpieza posterior.

**No forma parte de este trabajo:**
- Cambios al módulo 0 (orden, tipografía, título bilingüe, tarjetas EN/ES de Garaje_Test). El orden
  definido es el que ya tiene la v5: Welcome → Language → Jurisdiction → Vehicle → Comfort.
- Práctica Point/Select previa al módulo 0.
- Gestos distintos de «seleccionar con el gatillo» (p. ej. la puerta de familiarización con el
  manillar antes del módulo 2). El diseño los admite como datos; no se construyen ahora.

## 2. Comportamiento

| Situación | Qué se muestra |
|---|---|
| **Primera vez de cada tipo de interacción** en la sesión | Control + resaltado + texto corto |
| **Atasco:** 10 s sin ningún clic con objetivos visibles | Control + resaltado, sin texto |
| Llamada explícita `TutorialHelp.Show(...)` | Lo que pida la llamada |
| Observador detenido (`Stop`) | Nada automático; las llamadas explícitas sí funcionan |

**Tipos de interacción hoy en la v5** (todo es apuntar y presionar el gatillo):
- **Interfaz:** botones, tarjetas y toggles de un canvas (p. ej. `Begin` en Welcome).
- **Objeto 3D:** objetos con collider que reciben clic (EPP y puntos del vehículo del módulo 1).

**Aprendido:** un tipo queda aprendido con el primer **clic real** del usuario sobre un objetivo de ese
tipo mientras el observador está activo. Dura **toda la sesión** (también al repetir un módulo) y se
reinicia al cerrar la aplicación: las gafas se comparten y una apertura nueva es un usuario nuevo. No se
guarda nada en el dispositivo. El atasco ayuda **siempre**, aunque el tipo ya esté aprendido.

**Mano:** el control mostrado es el de la **mano dominante** elegida en Comfort; antes de esa elección,
la derecha.

**Evaluación:** cuando hay varias opciones (p. ej. elegir EPP), se resaltan **todas por igual**; el
tutorial nunca distingue la correcta. Con un solo objetivo posible, se resalta ese.

## 3. Componentes

Código nuevo en `Assets/_Project/Systems/TutorialHelp/`, espacio de nombres `RideSafe.Help`. No puede ser
`RideSafe.TutorialHelp`: desde un módulo en `RideSafe.*`, `TutorialHelp.Stop()` resolvería al namespace y no
a la clase.

| Pieza | Responsabilidad | Depende de |
|---|---|---|
| `TutorialGestureSO` | Dato de un gesto: control del Touch Plus que se ilumina (gatillo, grip, A/B, X/Y…), animación, clave I2 del texto corto opcional, mano (dominante o fija) | — |
| `TutorialHelp` (estático) | Puerta de entrada pública: `Show`, `Stop`, `Resume`, `Pause`. Resuelve el gestor por `ServiceLocator`; si no hay tutorial en la escena, **no hace nada** | `Cachacos.ServiceLocator` |
| `TutorialHelpManager` | Gestor, uno por escena. Aplica las reglas (primera vez, aprendido, atasco, mano, detenido/activo) y ordena a la parte visual. Lógica separada en clases C# sin `MonoBehaviour` para poder probarla sin escena | núcleo |
| Observador (`TargetScanner`, `ClickWitness`, `QuietDetector`, `HandTracker`) | Busca objetivos visibles y su tipo, detecta clics en el objetivo, pausas por video o narración, y la mano dominante; informa al gestor | uGUI, `ComfortSettingsPanel` |
| `GestureHintView` | Touch Plus 3D animado, abajo y a un lado de la mirada, con fondo oscuro | prefabs Touch Plus existentes |
| `TargetPulse` / `TargetHalo` | Resaltado 2D (titileo) y 3D (halo Mint) | uGUI / renderers |
| `HintCaption` | Texto corto localizado junto al control, nunca sobre la pantalla del módulo | I2 |
| `TutorialCue`, `TutorialPause` | Componentes sin código: mostrar un gesto mientras un objeto está activo / detener el observador mientras está activo | `TutorialHelp` |

**Ensamblados:**
- `TutorialHelp`: todo el runtime. **No depende de AutoHand**, porque el clic se detecta en el objetivo
  (ver §5.4). Referencia `Tutorial` y `Tutorial.Presentation` solo para los modelos Touch Plus y sus poses.
- `TutorialHelp.Editor`: montaje de la escena y sonda de diagnóstico.
- `TutorialHelp.Tests`: pruebas de EditMode.
- Los términos de I2 se añaden con un script en `Assets/_Project/Localization/Editor/`, porque I2 no tiene
  asmdef, como ya hace `Module01DemoSetup`.

**Gesto inicial:** un asset `SO_Gesture_Select` (gatillo de la mano dominante) con dos claves de texto,
una para interfaz y otra para objeto 3D.

## 4. API para módulos (caso mixto)

```csharp
// Mostrar ayuda de un gesto sobre unos objetivos; ocultarla con Dispose()
IDisposable ayuda = TutorialHelp.Show(gesto, objetivos);
ayuda.Dispose();

// Detener el observador en una parte que no necesita tutorial, y reactivarlo después
TutorialHelp.Stop();
TutorialHelp.Resume();

// Atajo de bloque: Stop al entrar, Resume al salir
using (TutorialHelp.Pause()) { ... }
```

- `Stop()` oculta lo visible y deja de buscar objetivos: sin ayuda de primera vez ni de atasco, y sin
  coste de CPU.
- `Resume()` reactiva el observador y reinicia el contador de atasco.
- Es un **interruptor simple**: gana la última llamada.
- `Show(...)` funciona aunque el observador esté detenido.
- Sin tutorial en la escena (tests, otras escenas) todas las llamadas son inocuas.

**Sin código:**
- `TutorialCue` (gesto + objetivos): muestra la ayuda mientras el objeto que lo lleva está activo.
- `TutorialPause`: llama a `Stop()` al activarse y a `Resume()` al desactivarse.

## 5. Detección (observador)

1. **Objetivos.** Se buscan cada 0,2 s, no en cada fotograma.
   - De interfaz: `Selectable` activos, `IsInteractable()` y con el canvas visible. Un botón desactivado no cuenta.
   - Objetos 3D: con `Collider` activo y un componente que implemente `IPointerClickHandler`. Esta lista se refresca cada 1 s.
   - Solo cuenta lo que el usuario puede ver: dentro de unos 60° alrededor de la mirada.
   - Se excluyen las manos, el rayo de AutoHand y los objetos del propio tutorial.
2. **Tipo.** Si el objetivo está bajo un `Canvas`, es de interfaz; si no, es un objeto 3D.
3. **Primera vez.** Cuando aparecen objetivos de un tipo no aprendido, se espera 1 s a que termine la
   transición y se muestra la ayuda. Se oculta con el primer clic real de ese tipo.
4. **Clic real.** Se detecta **en el propio objetivo**, no en el módulo de entrada. A cada objetivo visto
   se le añade en tiempo de ejecución un `ClickWitness`: un componente invisible (`HideFlags.DontSave`,
   nunca se guarda) que recibe el mismo clic que el botón o el objeto. Así funciona igual con AutoHand en
   las gafas, con el ratón en PC (`InputSystemUIInputModule`) y con los clics simulados de las pruebas,
   sin tocar AutoHand ni el código del módulo. Se retiran al cerrar la escena. *(Ajuste del 2026-10-15:
   leer el `AutoInputModule` no veía los clics de PC ni de las pruebas.)*
5. **Atasco.** Con objetivos visibles y 10 s sin ningún clic, se muestran el control y el resaltado.
   Desaparecen con el siguiente clic, y cualquier clic reinicia el contador.
6. **El contador de atasco no corre:**
   - durante un `VideoPlayer` en reproducción;
   - con un `AudioSource` no-bucle de más de 2 s sonando (narración);
   - sin objetivos visibles (fades, teletransportes);
   - con el observador detenido.

   Con video o narración en curso **tampoco se muestra la ayuda de primera vez**: espera a que termine,
   para no hablar encima de la voz. *(Ajuste del 2026-10-15.)*

   Solo cuentan los canvas en espacio de mundo, que son los de la experiencia en VR. Un canvas en pantalla
   (overlay) no tiene posición en la escena a la que apuntar.
7. **Mano dominante.** Se escucha `ComfortSettingsPanel.onStart` (UI compartida, no código de módulo) y
   se usa `ComfortSettings.Hand` durante el resto de la sesión.

## 6. Visual

- **Control:** el Touch Plus 3D de la mano dominante, con el botón del gesto iluminado y una animación
  de presionar y soltar. Va abajo y a un lado de la línea de mirada, sin tapar la pantalla, sobre el
  fondo oscuro del atlas (`Panel_Dark_Base`).
- **Resaltado 3D:** halo Mint (`#A9E2D4`) alrededor del objeto.
- **Resaltado 2D:** titileo suave del propio elemento.
  - El color oscila blanco → gris claro (≈85 %) → blanco, en unos 1,6 s y con curva sinusoidal; las
    opciones múltiples titilan al unísono.
  - Se aplica en la capa de render (`CanvasRenderer`), no en el color guardado, y se restaura exacto al
    terminar.
  - No interfiere con los estados del botón, que en este proyecto cambian el sprite (SpriteSwap), no el
    color.
  - El tono final se ajusta viendo la captura.
- **Texto corto:** panel pequeño junto al control, en inglés y español vía I2, solo la primera vez de
  cada tipo. Nunca repite lo que ya dice la pantalla del módulo.

## 7. Cambios en la escena v5

- **Se añade:** un objeto raíz `TutorialHelp` con el gestor, el observador y la parte visual.
- **Se retira:** el objeto `Tutorial` desactivado, con su segunda copia de `PF_Module00_UI` y su
  secuencia de onboarding.
- **No se toca:** ningún objeto del módulo 0 ni del módulo 1, el rig de AutoHand (manos, `UIPointer`),
  XR ni la iluminación.
- **Modo PC:** para probar se usa el `DesktopMode` existente. La simulación de entrada que haga falta
  vive en el código de pruebas o del editor, no en la experiencia.

## 8. Pruebas

1. **EditMode, sin escena** (núcleo):
   - reglas: primera vez por tipo, aprendido en la sesión, atasco a 10 s, reinicio del contador;
   - interruptor: Stop/Resume/Pause, y Show con el observador detenido;
   - pausas del contador por video, narración o ausencia de objetivos;
   - filtro de objetivos: interactuable, visible y en el campo de visión;
   - visual y mano: restauración exacta del color tras el titileo, y mano dominante por defecto y tras Comfort;
   - llamadas inocuas sin gestor.
2. **Contrato de la v5:**
   - existe un solo `TutorialHelp` y se inicia;
   - el objeto `Tutorial` viejo ya no está;
   - ningún componente de los módulos 0 y 1 referencia al tutorial.
3. **Regresión:** los 6 tests de PlayMode del módulo 1 y el resto de EditMode quedan como estaban.
4. **Recorrido en PC, con capturas:**
   - Welcome: ayuda de primera vez sobre `Begin`;
   - Language → Jurisdiction → Vehicle, sin ayuda (tipo ya aprendido);
   - Comfort con mano izquierda;
   - primer EPP: ayuda 3D con el control izquierdo;
   - 10 s quieto: atasco con resaltado de todas las opciones.
5. **Video:** con el recorrido en PC verificado, grabación de la ventana de Unity con ffmpeg.

## 9. Riesgos y cómo se cubren

| Riesgo | Cobertura |
|---|---|
| La heurística de «espera acción» falla en alguna pantalla | `TutorialHelp.Stop()` / `TutorialPause` en esa sección; los tests de contrato lo detectan |
| Un audio largo que no es narración pausa el atasco | Aceptado; si molesta, `Stop()` local o ajustar el umbral |
| Coste en Quest de buscar objetivos | Búsqueda cada 0,2 s, lista 3D refrescada cada 1 s, nada mientras está detenido |
| El titileo resta contraste al texto | Tono inicial suave (≈85 %), ajustado con captura |
| Versiones de Unity distintas en el equipo (6000.4.9f1 / 6000.5.4f1) | El cambio de escena se hace en 6000.4.9f1; acordar una sola versión antes de integrar |
