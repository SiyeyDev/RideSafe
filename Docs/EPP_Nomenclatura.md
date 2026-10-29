# EPP — nomenclatura única

> Referencia para hablar todos de lo mismo: PO, arte y código.
> Fuente: `Docs/RideSafe_VR_equipment_image_reference_library_2026-10-19.xlsx`.
> Generado y verificado contra el proyecto el 2026-10-29.

## La regla

**El nombre manda. El ID solo vale si se dice de qué pestaña es.**

El Excel trae el mismo elemento con **dos IDs distintos** según la pestaña, así que un ID a secas
es ambiguo. El nombre de la **columna C es idéntico en las dos pestañas**, y es el que el juego ya
usa palabra por palabra.

Para hablar en la reunión, lo corto y sin ambigüedad es el **`ItemId` del juego** (`helmet_ok`,
`cargo_secured`): es único, es el que sale en el reporte y en los tests, y no cambia entre hojas.

## Por qué el ID no sirve solo

Las dos pestañas numeran los opcionales distinto. **12 de los 14 coinciden en nombre y difieren en
ID** (solo O01 y O02 coinciden). La de puntuación deja huecos: O03, O05, O06, O10, O17.

| Nombre | ID en «Sistema de puntuación» | ID en «Biblioteca» |
|---|---|---|
| Guantes completos… | **O04** | O03 |
| Gafas transparentes resistentes a impacto | **O07** | O04 |
| Gafas solares con protección UV… | **O08** | O05 |
| Mochila cerrada de dos tirantes… | **O09** | O06 |
| Candado con soporte o ubicación de transporte segura | **O11** | O07 |
| Kit compacto para reparar pinchazos… | **O12** | O08 |
| Multiherramienta compacta y compatible | **O13** | O09 |
| Soporte universal de aluminio para teléfono… | **O14** | O10 |
| Timbre compatible | **O15** | O11 |
| Chaqueta impermeable ajustada | **O16** | O12 |
| Botella sellada en portabotella compatible | **O18** | O13 |
| Alforja impermeable y reflectiva… | **O19** | O14 |

**El juego usa los IDs de «Sistema de puntuación»** (la columna en negrita). Los esenciales
(E01–E05), los peligros (H01–H13) y el neutral (N01) coinciden en las dos pestañas. H05 no existe
en ninguna de las dos; N01 solo está en la puntuación y en «Props no evaluables».

**Trampa de lectura:** «O07» dicho a secas puede ser las gafas transparentes (puntuación, lo que
usa el juego) o el candado (biblioteca). Son dos elementos que ni se parecen.

## «Mesa» tiene dos significados

- **Mesa 1/2/3** (columna P del Excel, «Mesa / grupo de exhibición») es un **grupo temático**:
  1 Protección personal, 2 Visibilidad y control, 3 Preparación del trayecto. En el código es
  `Entry.Table` (`protection` / `control` / `readiness`). **Coincide al 100 por ciento con la hoja.**
- **La parada** es el **mueble físico** que el jugador visita. Son cuatro, decididas por la PO el
  2026-10-27, y **no tienen nada que ver con la columna P**: mesa de la entrada, mesa del centro,
  mesa del fondo y mostrador de la ropa.

Un elemento de «Mesa 3 — Preparación» puede estar físicamente en la mesa de la entrada. Al hablar
de ubicación hay que decir **«grupo»** o **«parada»**, nunca «mesa» a secas.

## Los 29 elementos que no están en el Excel

Llevan prefijo **P** justamente para que no se confundan con una fila del cliente. Son los que la
PO añadió (coderas, rodilleras) y lo que el artista modeló y la hoja no evalúa. Su nombre es corto
y propio, no de columna C: «Gafas deportivas» es `P06`, no una fila del Excel.

## El cuadro completo

61 elementos. «ID puntuación» es el que usa el juego; «ID biblioteca» está solo para leer el Excel
sin equivocarse.

| Nombre canónico (col. C) | ID puntuación | ID biblioteca | `ItemId` del juego | Tipo | Peso | Grupo | Mesa (col. P) | Parada |
|---|---|---|---|---|---|---|---|---|
| Casco certificado, íntegro, talla correcta y abrochado | E01 | E01 | `helmet_ok` | Esencial | +40 | — | 1 Protección | Entrada |
| Luz delantera blanca funcional | E02 | E02 | `light_front` | Bono | +1 | — | 2 Visib./control | Fondo |
| Luz trasera roja funcional | E03 | E03 | `light_rear` | Bono | +1 | — | 2 Visib./control | Entrada |
| Calzado cerrado, seguro y con suela de buen agarre | E04 | E04 | `shoes_ok` | Esencial | +20 | — | 1 Protección | Entrada |
| Ropa ceñida con puños, cordones y tiras asegurados | E05 | E05 | `trousers_fitted` | Esencial | +5 | — | 3 Preparación | Entrada |
| Prenda superior fluorescente para el día | O01 | O01 | `hiviz_day` | Bono | +2 | — | 2 Visib./control | Fondo |
| Chaleco o chaqueta retroreflectiva ajustada | O02 | O02 | `reflective` | Bono | +3 | — | 2 Visib./control | Entrada |
| Guantes completos que preservan agarre y acceso a controles | O04 | O03 | `gloves` | Bono | +2 | — | 1 Protección | Entrada |
| Gafas transparentes resistentes a impacto | O07 | O04 | `glasses` | Bono | +1 | — | 1 Protección | Centro |
| Gafas solares con protección UV adecuadas para sol y deslumbramiento | O08 | O05 | `sunglasses` | Bono | +1 | — | 1 Protección | Centro |
| Mochila cerrada de dos tirantes, ligera y bien ajustada *(solo escooter)* | O09 | O06 | `backpack` | Bono | +2 | — | 3 Preparación | Centro |
| Candado con soporte o ubicación de transporte segura | O11 | O07 | `cargo_secured` | Bono | +2 | — | 3 Preparación | Centro |
| Kit compacto para reparar pinchazos con contenido identificable *(solo ebike)* | O12 | O08 | `flat_kit` | Bono | +2 | — | 3 Preparación | Centro |
| Multiherramienta compacta y compatible | O13 | O09 | `multitool` | Bono | +1 | — | 3 Preparación | Centro |
| Soporte universal de aluminio para teléfono y navegación pasiva | O14 | O10 | `phone_mount` | Bono | +1 | — | 2 Visib./control | Entrada |
| Timbre compatible | O15 | O11 | `bell` | Bono | +2 | — | 2 Visib./control | Centro |
| Chaqueta impermeable ajustada | O16 | O12 | `rain_jacket` | Bono | +2 | — | 3 Preparación | Entrada |
| Botella sellada en portabotella compatible | O18 | O13 | `bottle` | Bono | +1 | — | 3 Preparación | Centro |
| Alforja impermeable y reflectiva instalada en portaequipajes *(solo ebike)* | O19 | O14 | `pannier` | Bono | +3 | — | 3 Preparación | Centro |
| Casco con correa de barbilla cortada, desprendida o faltante | H01 | H01 | `helmet_strap` | Peligro | −30 | helmet_integrity | 1 Protección | Ropa |
| Casco agrietado, aplastado, talla evidentemente incorrecta o de novedad | H02 | H02 | `helmet_damaged` | Peligro | −30 | helmet_integrity | 1 Protección | Fondo |
| Audífonos grandes que ocluyen ambos oídos | H03 | H03 | `headphones` | Peligro | −20 | auditory_occlusion | 2 Visib./control | Centro |
| Chanclas, slides o sandalias abiertas y sueltas | H04 | H04 | `sandals` | Peligro | −15 | footwear_control | 1 Protección | Entrada |
| Poncho-tarp extragrande con laterales abiertos y exceso de material | H06 | H06 | `poncho` | Peligro | −20 | loose_rainwear | 3 Preparación | Fondo |
| Gafas con lente severamente rayada, agrietada, deformada o empañada | H07 | H07 | `glasses_damaged` | Peligro | −20 | visual_obstruction | 1 Protección | Entrada |
| Mitones voluminosos que impiden operar controles | H08 | H08 | `mittens` | Peligro | −15 | control_access | 2 Visib./control | Centro |
| Tote abierto de una correa con tira larga sin asegurar | H09 | H09 | `tote` | Peligro | −15 | unstable_carry | 3 Preparación | Entrada |
| Paraguas de mano | H10 | H10 | `umbrella` | Peligro | −30 | one_hand_wind | 3 Preparación | Centro |
| Vaso de café o bebida para llevar en la mano | H11 | H11 | `drink` | Peligro | −20 | one_hand_drink | 2 Visib./control | Centro |
| Gorra de visera seleccionada en lugar del casco *(sustituye a `helmet_ok`)* | H12 | H12 | `cap` | Peligro | −20 | helmet_substitution | 1 Protección | Entrada |
| AirPods | H13 | H13 | `airpods` | Peligro | −20 | auditory_occlusion | 2 Visib./control | Centro |
| Objeto en la mano | N01 | — | `cargo_handheld` | Bono | 0 | — | 2 Visib./control | Fondo |
| *(sin nombre en I2)* | — | — | `elbow_pads` | Bono | +2 | — | 1 Protección | Fondo |
| *(sin nombre en I2)* | — | — | `knee_pads` | Bono | +2 | — | 1 Protección | Fondo |
| *(sin nombre en I2)* | — | — | `flashlight` | Bono | 0 | — | 2 Visib./control | Entrada |
| *(sin nombre en I2)* | — | — | `trousers_standing` | Bono | 0 | — | 3 Preparación | Entrada |
| *(sin nombre en I2)* | — | — | `snorkel` | Bono | 0 | — | 1 Protección | Centro |
| *(sin nombre en I2)* | — | — | `sport_glasses` | Bono | 0 | — | 1 Protección | Centro |
| *(sin nombre en I2)* | — | — | `work_vest` | Bono | 0 | — | 2 Visib./control | Fondo |
| *(sin nombre en I2)* | — | — | `work_goggles` | Bono | 0 | — | 1 Protección | Fondo |
| *(sin nombre en I2)* | — | — | `spare_red_light` | Bono | 0 | — | 2 Visib./control | Fondo |
| *(sin nombre en I2)* | — | — | `loose_padlock` | Bono | 0 | — | 3 Preparación | Fondo |
| *(sin nombre en I2)* | — | — | `cable_lock` | Bono | 0 | — | 3 Preparación | Fondo |
| *(sin nombre en I2)* | — | — | `damaged_cable_lock` | Bono | 0 | — | 3 Preparación | Fondo |
| *(sin nombre en I2)* | — | — | `work_gloves` | Bono | 0 | — | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `latex_gloves` | Bono | 0 | — | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `fabric_gloves` | Bono | 0 | — | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `face_mask` | Bono | 0 | — | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `folded_jeans` | Bono | 0 | — | 3 Preparación | Ropa |
| *(sin nombre en I2)* | — | — | `trousers_tech` | Bono | 0 | — | 3 Preparación | Ropa |
| *(sin nombre en I2)* | — | — | `jeans` | Bono | 0 | — | 3 Preparación | Ropa |
| *(sin nombre en I2)* | — | — | `low_rise_trousers` | Bono | 0 | — | 3 Preparación | Ropa |
| *(sin nombre en I2)* | — | — | `sneakers` | Bono | 0 | — | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `boots` | Bono | 0 | — | 1 Protección | Ropa |
| Ropa o cordones sueltos | — | — | `clothing_loose` | Peligro | −15 | loose_clothing | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `neck_scarf` | Peligro | −15 | loose_clothing | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `trousers_loose` | Peligro | −15 | loose_clothing | 3 Preparación | Ropa |
| *(sin nombre en I2)* | — | — | `wide_trousers` | Peligro | −15 | loose_clothing | 3 Preparación | Ropa |
| *(sin nombre en I2)* | — | — | `loose_laces` | Peligro | −15 | loose_clothing | 1 Protección | Ropa |
| *(sin nombre en I2)* | — | — | `old_headphones` | Peligro | −20 | auditory_occlusion | 2 Visib./control | Fondo |
| *(sin nombre en I2)* | — | — | `shower_slippers` | Peligro | −15 | footwear_control | 1 Protección | Ropa |

## Lo único que no cuadra

**N01.** El Excel lo llama **«Teléfono inteligente»**; el juego dice **«Objeto en la mano»**
(`cargo_handheld`, `M1/Item_CargoHandheld`). Es el único nombre del juego que no es literal la
columna C. Hay que decidir cuál queda.
