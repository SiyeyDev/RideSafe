# Foco al revisar un punto del vehículo — diseño

Fecha: 2026-10-23 · Estado: implementado (validado en PC) · Pidió: Cindy (PO)

> **Actualización 2026-10-26 (Cindy):** el foco se extiende a las **mesas de EPP**, «así como se
> hizo con las partes de la e-bike y de la patineta». Mismo componente y mismo radio: desde el
> punto de vista de cada estación el objeto más cercano de su mesa queda a 3,08 m (protección),
> 2,60 m (control) y 3,37 m (preparación), fuera del radio de 1,6 m
> (`FocusDomeSceneTests.EveryTableObject_StaysOutsideTheBubble_AtItsStation`). Donde abajo dice
> «solo en la sección del vehículo» o «Mesas de EPP: fuera de alcance», ya no aplica.

## Objetivo

Al abrir un punto de revisión de la e-bike o de la patineta (popup + pieza en el pedestal), el
entorno —garaje, muebles **y el propio vehículo**— se oscurece, y solo quedan a plena luz el popup,
el pedestal con su pieza y la interfaz. Al responder «Sí» o «No» todo vuelve a su estado. Es el
mismo efecto que el video explicativo, pero sin tapar el pedestal. Tiene que funcionar en VR.

### Lo que dijo la PO

- Se activa solo con un punto abierto; vuelve al estado original al elegir «Sí» o «No».
- Se oscurece todo, incluido el vehículo; quedan visibles el popup, el pedestal y la interfaz.
- Solo en la sección del vehículo (e-bike o patineta), no en las mesas de EPP.
- Oscurecer, sin desenfoque (eligió la opción A frente a desaturar y a desenfoque real).
- La migración del Módulo 1 a la cámara del visor queda como trabajo aparte.

### Por qué no se reutiliza el oscurecido del video

`TheaterMode` es un panel negro en la capa `UI` que pinta la `UI Camera` superpuesta por encima de
todo el 3D. Eso tapa también el pedestal, y además solo funciona porque esa cámara está apilada en
la cámara de PC: la del visor lo pinta todo en una pasada, sin cámara superpuesta.

### Por qué no se desenfoca

En el Quest el proyecto usa `Mobile_RPAsset`, sin textura de profundidad ni copia de la imagen. Un
desenfoque exige las dos más una pasada por ojo cada fotograma, y el fondo borroso en VR incomoda
(el ojo intenta enfocar y no puede).

## Enfoque: una burbuja oscura alrededor de la cabeza

Una esfera centrada en la cabeza del aprendiz, vista desde dentro, con un material negro
semitransparente.

- **Lo que queda fuera, oscurecido:** todo lo que está más lejos que su radio (el vehículo, los
  muebles y el garaje). La esfera se dibuja delante de ello y lo oscurece.
- **Lo que queda dentro, a plena luz:** lo más cercano, es decir, el pedestal, su pieza y el popup.
  - El pedestal y la pieza son opacos: escriben profundidad y la pared de la esfera, más lejana, no
    pasa la prueba de profundidad donde están ellos.
  - El popup y la interfaz son transparentes y se dibujan después de la esfera.

Es geometría real de la escena: correcta en estéreo, independiente de qué cámara pinta qué, sin
textura de profundidad. Cuesta una capa transparente a pantalla completa, lo mismo que el oscurecido
del video, y solo mientras hay un punto abierto.

**Efecto visible conocido:** el suelo se oscurece a partir del radio, así que se ve un círculo
oscuro en el suelo por debajo del pedestal. Queda bajo la línea de mirada. Si molesta, se suaviza
con dos o tres esferas de oscuridad creciente (fuera de alcance salvo que la PO lo pida).

## Piezas

### `FocusDome` (`Assets/_Project/UI/Runtime/FocusDome.cs`, namespace `RideSafe.UI`)

Componente reutilizable, junto a `TheaterMode`, para que los módulos 2–5 puedan usarlo.

- **API**, la misma forma que `TheaterMode`:
  - `SetOn(bool)`, con fundido.
  - `SetOnImmediate(bool)`.
  - `IsOn` y `CurrentAlpha`.
  - `ConfigureForTests(...)`.
- **Campos serializados:**
  - `_renderer`, el `MeshRenderer` de la esfera.
  - `_head` (`Transform`); si está vacío usa `Camera.main` en cada `LateUpdate`.
  - `_radius`, en metros.
  - `_dimAmount`, la opacidad, por defecto `.88` como el video.
  - `_fadeSeconds`, por defecto `.45` como el video.
- **Seguimiento:** coloca la esfera en la posición de la cabeza en `LateUpdate`; la rotación da
  igual. La escala es `2 × _radius`, porque la esfera de Unity mide 1 m de diámetro.
- **Opacidad:** la aplica por `MaterialPropertyBlock` (`_BaseColor.a`), sin instanciar el material.
- **Apagado:** con opacidad 0 apaga el renderer, así no cuesta nada mientras no se usa.

#### Fundido (pedido de la PO: nunca de golpe)

Encender y apagar es siempre un fundido del alpha del material, nunca un salto.

- **Encender:** el renderer se activa con alpha 0 y el alpha sube hasta `_dimAmount` (`.88`) en
  `_fadeSeconds` (`.45` s).
- **Apagar:** el alpha baja de su valor actual a 0 en el mismo tiempo, y solo al llegar a 0 se apaga
  el renderer. El oscurecido no desaparece de golpe.
- **Curva:** suave en los extremos (*smoothstep*), para que el cambio de luz no se note brusco en el
  visor.
- **Si se interrumpe** —se responde «Sí» o «No» mientras todavía se está oscureciendo—: el fundido
  de salida arranca desde el alpha que hubiera en ese momento. No salta al máximo ni a 0.
- **Tiempo no escalado**, como `TheaterMode`, para que una pausa del juego no lo congele a medias.
- **Solo `SetOnImmediate`** cambia sin fundido. Se reserva para montar la escena y para los tests.

### Material `M_FocusDome` (`Assets/_Project/UI/Materials/M_FocusDome.mat`, carpeta nueva)

- **Shader:** `Universal Render Pipeline/Unlit`, sin shader propio, ya compatible con VR
  (*single-pass instanced*). El color base es negro.
- **Ajustes:** Surface Type *Transparent*, Blending *Alpha* y Render Face *Back*, para que se vea desde
  dentro.
- **Render queue `2950`:** después de lo opaco y antes de la cola transparente estándar (3000), donde
  están los canvas del popup y de la interfaz. Es lo mínimo que permite URP, que recalcula la cola con
  la prioridad del material (`_QueueOffset`, de −50 a 50). Si fuera 3000, la esfera, con su centro en
  la cámara, se ordenaría la última entre los transparentes y oscurecería el popup.

### Integración (`Module01Experience`)

- **Campo nuevo:** `[SerializeField] private FocusDome _focus;`.
- **Al abrir un punto** (`_inspector.Presenting`): si `_director.InVehicleSection`, llama a
  `_focus.SetOn(true)`.
- **Al cerrarlo** (`_inspector.Accepted`, `_inspector.Declined` o `_inspector.Dismissed`): llama a
  `_focus.SetOn(false)`.
- **Sin `_focus` asignado:** no hace nada, igual que con `_theater`.

### Montaje en la escena v5

`Module01SceneComposition` crea la esfera en sitio y de forma idempotente, como el resto de la
composición, sin regenerar prefabs.

- **Ubicación:** un objeto `FocusDome` bajo `Module01_Presentation`, con la malla esfera de Unity,
  `M_FocusDome`, sin collider y sin sombras.
- **Cableado:** lo conecta a `Module01Experience._focus`.

### Radio: medido, no supuesto

El radio debe quedar entre el punto más lejano del pedestal y del popup y el punto más cercano del
vehículo en cada zona.

- **El caso crítico es la cabina:** con la bici de frente, su rueda delantera se acerca bastante más
  que los 2,7 m de su centro.
- **Cómo se fija:** el radio se mide en la escena con la posición del aprendiz en `anchor_vehicle`
  y los giros de cada zona. Queda en el componente, y un test lo vigila.

## Fuera de alcance

- Mover la presentación del Módulo 1 (popup, pedestal, interfaz y video) a la cámara del visor.
  Hoy cuelga de `DesktopMode/Desktop Camera`, que el modo VR apaga. Hasta hacerlo, el foco se valida
  en PC.
- Desenfoque, desaturación y suavizado del borde en el suelo.
- Mesas de EPP.

## Pruebas

- **EditMode, `FocusDomeTests`:**
  - El fundido sube y baja la opacidad por pasos intermedios, sin saltos: a mitad de tiempo está
    entre 0 y `_dimAmount`.
  - Apagar a mitad de un encendido arranca desde el alpha de ese momento.
  - El renderer sigue encendido durante el fundido de salida y se apaga al llegar a 0.
  - Sigue a la cabeza.
  - `SetOnImmediate` no espera.
- **EditMode, geometría contra la escena v5:**
  - En las tres zonas del vehículo de cada perfil (e-bike y patineta), con su giro aplicado, el
    pedestal y el popup quedan dentro del radio.
  - Todo el vehículo queda fuera.
- **EditMode, material:** queue entre 2500 y 3000 (2950), cara trasera y superficie transparente.
- **PlayMode, recorrido del M1 (`Module01SliceTests`):**
  - Al abrir un punto del vehículo, el foco se enciende.
  - Tras «Sí» o «No», se apaga.
  - En las mesas de EPP nunca se enciende.
- **Visual en PC:** capturas de cada zona del vehículo con un punto abierto, revisadas antes de dar
  el trabajo por terminado.

## Criterios de aceptación

1. Con un punto del vehículo abierto, el garaje y el vehículo se ven oscurecidos. El popup, el
   pedestal y su pieza se ven a plena luz.
2. Entra y sale con un fundido del alpha (0,45 s, curva suave), nunca de golpe, incluso si se
   responde «Sí» o «No» mientras todavía se está oscureciendo.
3. En las mesas de EPP no cambia nada.
4. La técnica no depende de la cámara de interfaz superpuesta. Funcionará en el visor en cuanto la
   presentación del Módulo 1 cuelgue de la cámara del visor.
5. Pasan las pruebas existentes y las nuevas.
