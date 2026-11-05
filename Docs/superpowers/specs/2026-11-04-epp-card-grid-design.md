# Cuadrícula de tarjetas de EPP en «Revisa tus elecciones» y «Evaluación de tus elecciones»

Fecha: 2026-11-04 · Pedido por: PO (Cindy) · Estado: aprobado en chat, en implementación

## Objetivo

Los dos paneles de la sección de EPP del Módulo 1 dejan de pintar filas de texto y pasan a una
cuadrícula de tarjetas con la imagen del EPP como protagonista, siguiendo la diagramación de las
capturas de referencia de la PO. Solo cambia la presentación: el flujo, los botones y el cálculo
de la nota se quedan como están.

## Decisiones de la PO

| Tema | Decisión |
|---|---|
| Imágenes | Renders propios de los modelos 3D del garaje (no las fotos de marca del xlsx: su «Guía visual» exige licencia). |
| Alcance | Solo la sección de EPP. El vehículo («Mi revisión») y el reporte final siguen con filas. |
| Revisar | Tarjeta con imagen y nombre, **sin** ícono. Botones «Seguir eligiendo» y «Enviar elecciones», tal cual. |
| Evaluación | Tarjeta con imagen, nombre e ícono arriba a la derecha (✓ verde / – amarillo / X roja, los de `StatusSemaphore`). Botón «Ver explicación», tal cual. |
| Puntaje | Píldora «Puntaje: N» bajo el título, N = nota de la sección (`SelectionScore.Percent`, fórmulas del xlsx con el ajuste del 2026-10-29). Sin «/ 100», sin bono, sin veredicto. |
| Textos | Solo el título; se quitan subtítulos y la nota bajo la lista. Títulos: «Revisa tus elecciones» y «Evaluación de tus elecciones». |
| Scroll | Se mantiene: la cuadrícula se desplaza dentro de una ventana con tope de alto. |

## Datos

Ya existen y no se rehacen: los 63 `SafetyItemSO` llevan el ID del xlsx, peso, grupo de peligro y
reglas (`Module01Library`), y cada EPP 3D (`WorldObjectTaskEntity`) apunta a su SO por `_itemId`.
Lo que falta es el render: el campo `SafetyItemSO.Thumbnail` (vacío en todos).

## Componentes

### Runtime (`RideSafe.UI`)

- **`ItemCard`**: en el prefab `PF_EppCard`. `Bind(ComparisonEntry, StatusGlyphs)`: imagen
  (`entry.Image`; sin imagen, un marcador neutro), nombre (`entry.Title`, hasta 2 líneas
  centradas) e ícono de estado (solo si llegan glifos).
- **`CardGrid`**: en el contenedor de la cuadrícula (`GridLayoutGroup` de 3 columnas dentro del
  `ScrollRect`). `Show(entries, glyphs)` borra e instancia una tarjeta por entrada y ajusta la
  ventana al contenido hasta su tope. Una sola implementación para los dos paneles.
- **`ComparisonView`**: nuevo `SetCards(title, score, entries)`: modo tarjetas con ícono y píldora
  de puntaje; apaga la lista de filas, el fondo de la columna y la nota. `SetSingleList` y
  `SetContent` vuelven al modo filas (vehículo y final), así que el mismo panel sirve a los dos.

### Runtime (`RideSafe.Module01`)

- **`Module01Experience`**: Revisar construye `ComparisonEntry` con nombre traducido y
  `Thumbnail` y llama al `CardGrid` del panel; la evaluación de EPP llama a `SetCards` con
  `M1/Evaluation_Title` y `M1/Score_Label`.
- **`ItemInspector`**: la copia de las mallas visibles se extrae a `VisibleMeshCopy` para que el
  fotógrafo de EPP use exactamente la misma regla que el pedestal.

### Editor

- **`EppThumbnailRenderer`** (menú `RideSafe → Módulo 1 → Fotografiar EPP`): por cada SO de EPP
  toma el modelo con el orden del pedestal (`InspectPrefab` → copia del objeto de la escena v5 →
  `DisplayPrefab`), lo fotografía en una escena de previsualización aislada en vista 3/4 desde la
  parada donde el aprendiz lo ve, con fondo transparente (doble render negro/blanco), lo guarda en
  `Assets/_Project/Module01/Art/Thumbnails/RS_M1_{ID}_{itemId}.png` como Sprite y lo asigna al SO.
  No toca la escena ni las luces. Se vuelve a correr cuando el arte cambie.
- **`UICardGrid`** (paso de composición en sitio, llamado desde `UIComposition.ComposeModule01`):
  construye `PF_EppCard`, la cuadrícula y su ventana de scroll en los dos paneles, la píldora de
  puntaje, quita los subtítulos y reconfigura `GrowWithContent` de Revisar. Idempotente.
- **`Module01UiTerms`**: `M1/Evaluation_Title` y `M1/Score_Label`.

## Casos borde

- SO sin render: la tarjeta muestra el marcador y el nombre; el fotógrafo lista en consola lo que
  no pudo fotografiar.
- Muchas tarjetas: scroll; título, píldora y botón quedan fuera de la ventana.
- Sin selección: el estado vacío existente.
- Cambio de idioma: nombres y títulos se resuelven con I2 al pintar, como hoy.

## Pruebas

EditMode nuevas: `CardGrid` crea N tarjetas; Revisar sin ícono y Evaluación con el ícono de su
estado; la píldora muestra el número; una tarjeta sin imagen no falla; `SetSingleList` vuelve a
filas. Se ajustan las existentes que miran filas o subtítulos. Las suites las corre la PO.
