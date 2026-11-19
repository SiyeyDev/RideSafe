# Plan de trabajo — reunión del 11 de noviembre de 2026

Fuente: `ridesafe_reu_11_nov.pdf` (quién hace qué). Alcance pedido por Charlie el 2026-11-17: todo lo
de Carlos y, de Daniel, solo dejar en las mesas los EPP del documento, separados y sin encimarse.
Se trabajó con Unity en `-batchmode` (sin Editor abierto). Detalle técnico: `Docs/memory.md`,
bitácora del 2026-11-17.

Leyenda: **hecho** · **base** = código sin cablear porque la escena aún no existe ·
**pendiente** = necesita a Charlie, al cliente o a otra persona.

## Módulo 1

| # | Tarea (PDF) | Cómo quedó | Estado |
|---|---|---|---|
| 1 | Resumen de EPP: mal elegidos, condicionales, correctos | `ReportPresenter.ReviewOrder` | hecho |
| 2 | Resumen del vehículo con el render | una tarjeta por punto: el vehículo en gris con la pieza en coral, y ✓/X | hecho |
| 3 | Quitar el resumen general | cada sección cierra con su resumen; el final es el cierre (5) | hecho |
| 4 | Si aprueba tras el vídeo, felicitación en vez del cuadro | EPP: «¡Felicitaciones! Seleccionaste de manera correcta». Vehículo: va directo el cierre (5), sin panel intermedio (hoja «Feedback Modulo 1», 8:57-9:09) | hecho; el «cuadro» era el «¿qué decides?» (confirmado en la grabación, 28:42) |
| 5 | Cierre del módulo 1 con premio | escarapela + «¡Felicitaciones! Completaste el módulo 1» + «Ya conoces los EPP que siempre debes usar…» + «Continuar al módulo 2» | hecho; la escarapela es provisional |
| 6 | Textos del documento «Feedback módulo 1», con locución | es la hoja de Google «Textos Modulo 1» del cliente (se ve en la grabación, 0:56 y 47:00). La pestaña «Feedback Modulo 1» ya está aplicada (1:06, 8:57-9:09); la pestaña «Modulo1», con las traducciones corregidas, no está en nuestro Drive | **pendiente**: que el cliente comparta la hoja |
| 7 | Voz en off en la bienvenida: cómo usar el control | texto en I2 y reproductor cableado (`M01_WelcomeController`) | **pendiente**: generar el audio (ElevenLabs) |
| 8 | Instrucción hablada al empezar los EPP | texto de la hoja (1:06) en I2: «Selecciona los elementos y accesorios para verlos en detalle…»; reproductor cableado (`M01_SelectionInstruction`) | **pendiente**: generar el audio |
| 9 | Música lofi muy suave | `BackgroundMusic` en bucle a 0,08 | **pendiente**: la pista |
| 10 | Nombres cortos y únicos | los de la columna C del Excel del cliente del 2026-11-13, sin inventar ninguno (`EPP_Nomenclatura.md`); el inglés es traducción nuestra | hecho; **pendiente**: que el cliente revise el inglés, y regenerar la voz de los 32 nombres |
| 11 | Inspección con la patineta | bici + Transmisión; patineta igual sin ella y sin plataforma | hecho; la transmisión usa `pedales` hasta que 3D separe cadena y corona |

## Módulo 2

| # | Tarea | Cómo quedó | Estado |
|---|---|---|---|
| 12 | Flechas con color según la velocidad | `RouteArrowTint` sobre el material de Daniel; modo neutro para la prueba | base |
| 13 | Al fallar: vídeo y vuelta al checkpoint | `RouteCheckpoints` (práctica / prueba) | base |

## De Daniel (pedido expreso)

| # | Tarea | Cómo quedó | Estado |
|---|---|---|---|
| D1 | Solo los EPP del documento | 32 en catálogo y escena; fuera los 31 P | hecho |
| D3 | Frenos: manubrio completo, resaltar solo las manetas | `frenos.fbx` (Daniel, 17-11) va al podio de los frenos de la bici y en él se delinean solo `freno_de`/`freno_iz` | hecho; falta la animación de las manetas |
| D4 | Llantas: solo la llanta, no rines ni radios | bici: `caucho` y `rueda_tracera1` | hecho en la bici; la patineta espera a que Daniel separe sus llantas |
| D5 | Display en el manubrio | bici: `display` (Daniel, 17-11) y `pantalla` | hecho en la bici; la patineta espera el suyo; falta la batería |
| D6 | Resaltado «blanco/azuloso, limpio y tecnológico» (Excel 11-13) | contorno blanco con halo azul (`InspectionOutline.shader`) | hecho |
| D2 | Separados y con el mismo espacio | una fila con hueco igual en cada mueble; sin solapes entre EPP (gafas O07 a la mesa del centro) | hecho |
