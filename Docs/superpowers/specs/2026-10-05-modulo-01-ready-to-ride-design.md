# Módulo 1 — Elementos de seguridad y verificación del vehículo

> Spec de diseño. Fecha: 2026-10-05. Rama: `dev`.
> Estado: pendiente de revisión.

---

## 1. Qué se construye y por qué

El módulo 1 responde dos preguntas antes de que el aprendiz se mueva: **qué debo
preparar o evitar antes de rodar**, y **qué partes de este vehículo debo
diagnosticar antes de decidir que está listo**.

No hay recorrido, ni frenado, ni evaluación de conducción. Es selección,
reconocimiento, diagnóstico y retroalimentación explicativa.

**Objetivo de esta iteración:** el módulo jugable de punta a punta **en modo PC
con mouse**, con un solo vehículo, y con el multimedia como marcador de posición.
El VR y el segundo vehículo entran después sin rediseñar nada.

---

## 2. Qué documento manda

Hay dos documentos y se contradicen. Para este módulo manda **RideSafe_VR
Module Narratives v2.0**, por tres razones: es posterior, es el único que
describe el módulo con detalle suficiente para construirlo, y es el que el
equipo ya siguió al generar `PF_Module01_UI`.

Contradicciones resueltas a favor de la narrativa:

| Tema | GDD v0.9 | Narrativa v2.0 | Decisión |
|---|---|---|---|
| Transferencia cambiada | La exige | La elimina (§4.10) | **Fuera.** Diseñada como fase 2 |
| Duración | 2:00 | 3–4 min | 3–4 min |
| Familiarización de controles | Antes del módulo 1 | Después (§4.11) | Después; fuera de este alcance |
| HUD de velocidad | Persistente | Prohibido en baseline | No aplica al módulo 1 |

La decisión **go / no-go** está en ambos documentos y entra: es la fila 8 de los
dos checklists de la narrativa, marcada *universal critical decision*.

**Nota abierta:** la narrativa declara como canónico un
`RideSafe_VR_GDD_v2.0_Evidence_Based.md` que el equipo no ha compartido. Si
aparece y contradice esto, esta spec se revisa.

---

## 3. Arquitectura

### 3.1 El motor ya existe

`TaskSequence` es el motor. No se construye nada nuevo ahí.

- `TaskSequenceSO` — la secuencia como dato: id, requisitos de contexto,
  políticas de reinicio y salto, y la lista de pasos.
- `TaskStepData` — por paso: `stepId`, `entityId`, `instructionKey`
  (clave de localización, **nunca texto literal**), `audioKey` resuelto aparte,
  `ITaskValidator`, modo de completado y timeout.
- `TaskSequenceRunner` — recorre las fases
  `Prepare → Present → WaitForInteraction → Validate → Feedback → Complete → Cleanup`.

### 3.2 Modularidad entre vehículos: resuelta por contexto

`ContextRequirement` compara una clave de contexto contra un valor esperado, y su
propio tooltip nombra `vehicle` como ejemplo. Por tanto:

- Una `TaskSequenceSO` por vehículo, con requisito `vehicle == ebike` o
  `vehicle == escooter`.
- El módulo 0 ya captura la elección; solo hay que publicarla en el contexto.
- **Cero condicionales en código.** Añadir el segundo vehículo es crear un asset.

Esta regla aplica a todos los módulos, no solo al 1.

### 3.3 Enlace por nombre, nunca overrides en el prefab

`Module00TutorialBinding` estableció el patrón y el módulo 1 lo sigue sin
excepción: los componentes se localizan en runtime por **id de panel y nombre de
objeto**, los que escribe el generador. Nada se cablea como override en el
prefab, porque `RideSafeUIPrefabBuilder` lo regenera con ids nuevos y los
overrides desaparecen en silencio.

Cada binding expone un `FindMissing()` que un test en EditMode ejecuta contra el
prefab, de modo que una regeneración que rompa el contrato falle en el test y no
en la escena.

### 3.4 Localización

Se usa la costura existente: `Cachacos.ILocalizationProvider` con
`I2LocalizationProvider` como implementación, resuelta por `ServiceLocator`. Eso
evita que `RideSafe.UI` dependa de I2, que no tiene asmdef.

Todo texto nuevo entra como término I2 bajo la categoría `Module1/`. El código no
contiene cadenas visibles para el usuario.

`MenuRuntimeTextLocalizer`, nuestro apaño anterior, queda obsoleto frente a
`UIText`. Se unifica fuera de esta spec.

### 3.5 La única pieza de interacción nueva

`AutoHandUITaskEntity` ya cubre uGUI world-space traduciendo eventos del
EventSystem a *focus / select / confirm*. Como el modo PC también usa
EventSystem, **sirve igual con mouse y con el puntero de AutoHand**.

Falta su hermana para objetos 3D: una entidad sobre `Collider` que implemente las
mismas interfaces a partir de los eventos del EventSystem, habilitada por un
`PhysicsRaycaster` en la cámara. Mismo contrato, mismos validadores, y funciona
en PC y en VR sin ramificar.

---

## 4. Recorrido

El aprendiz **nunca se mueve por su cuenta**. Entre zonas: fade out → el rig se
reposiciona en el ancla de la siguiente → fade in → arranca la línea de voz. El
mismo mecanismo sirve para la entrada al garaje y para el salto al vehículo.

```
Módulo 0 (afuera)  →  puerta + fade  →  Orientación
   → Zona 1 Cabeza → Zona 2 Ropa y calzado → Zona 3 Carga
   → Revisar mis elecciones  →  Comparación A  →  Video A
   → fade al vehículo
   → Zona 4 Cockpit → Zona 5 Ruedas y estructura → Zona 6 Visibilidad
   → Decisión go/no-go  →  Comparación B  →  Video B
   → Reporte  →  fin del módulo
```

En cada zona: el objeto sale al frente rotando suavemente, un popup arriba con su
nombre, otro abajo con la confirmación y Sí/No. Al confirmar, entra en la lista
lateral, que está siempre visible y solo muestra lo que el aprendiz eligió.

**No** devuelve el objeto a su sitio sin registrar nada, y la zona sigue abierta.
Un elemento ya añadido se puede quitar desde la lista mientras no se haya
enviado la sección: `ChecklistView` ya expone `RemoveItem` y `onItemRemoved`, y
el panel `Preparation` se generó con las filas removibles. Volver atrás nunca
borra las elecciones previas.

**Nada revela si va bien antes de enviar.** Sin contador de aciertos, sin check
verde, sin checklist completo a la vista. El color nunca es la única señal.

---

## 5. Datos

### 5.1 Catálogo de elementos personales

Ocho elementos (§4.5.1 de la narrativa), repartidos en tres zonas. Cada uno lleva
su categoría real, no un booleano:

| Zona | Elemento | Categoría |
|---|---|---|
| 1 Cabeza | Casco íntegro, de la talla correcta, nivelado y abrochado | Núcleo |
| 1 | Casco agrietado, mal ajustado o sin abrochar | Inapropiado |
| 1 | Gafas | Opcional |
| 2 Ropa y calzado | Calzado cerrado y ropa contenida | Núcleo |
| 2 | Cordones sueltos o tela que alcanza la transmisión | Inapropiado |
| 2 | Capa reflectiva o brillante | Según la condición |
| 3 Carga | Morral o canasta que no bloquea visión ni controles | Núcleo |
| 3 | Objeto en la mano o colgado del manubrio | Inapropiado |

**Lo opcional no penaliza**, ni al elegirlo ni al omitirlo. La capa reflectiva es
correcta solo si la condición presentada es de noche, baja visibilidad o lluvia.

**Condición de esta escena: día claro.** La reflectiva queda como aceptable pero
no exigida, y la retroalimentación explica de qué depende. Cambiar esa condición
a lluvia es precisamente la variable que pedirá la transferencia de la fase 2.

### 5.2 Perfil de vehículo

Un asset por vehículo con sus puntos de interés, agrupados en tres zonas, y con
su clasificación de criticidad. Cubre los dominios V1–V6 de la narrativa:
frenos, llantas y ruedas, dirección y estructura portante, luces y reflectivos,
estado eléctrico y controles, y respuesta ante una falla.

El modelo 3D se referencia desde el perfil. Mientras 3D entrega, el perfil apunta
a primitivas con colliders nombrados; sustituirlo no toca lógica.

---

## 6. Puntuación y reporte

Se evalúan dos envíos independientes. El reporte separa, sin mezclar:

- Elementos núcleo acertados
- Elementos núcleo omitidos
- Opcionales y condicionales, etiquetados **sin penalización falsa**
- Inapropiados seleccionados, cada uno con su mecanismo de riesgo
- Componentes requeridos seleccionados y omitidos
- Componentes no esenciales añadidos
- La decisión go/no-go

`ComparisonView` ya pinta los cuatro estados con icono, texto y forma además de
color.

---

## 7. UI: qué se reutiliza

Los seis paneles de `PF_Module01_UI` ya existen y cubren el flujo:

| Panel | Uso |
|---|---|
| `Orientation` | Explicación inicial y ensayo con objetos neutros |
| `Preparation` | Lista lateral persistente (`ChecklistView`) |
| `ReviewChoices` | Cierre de la sección A con doble confirmación |
| `Diagnostic` | Lista del chequeo del vehículo |
| `Comparison` | Tableros comparativos A y B |
| `Explanation` | Video de revisión (`VideoSurface` + `VideoScreen`) |

Nuevo: el inspector rotatorio con sus dos popups, y el panel de decisión go/no-go.

---

## 8. Fuera de alcance hoy

- Los dos videos de revisión. Placeholder con `VideoTest.mp4`; el contenido lo
  produce el equipo.
- El segundo vehículo. Entra como asset cuando 3D lo entregue.
- La transferencia cambiada. Fase 2.
- El gate de montaje y familiarización de controles. Es posterior al módulo 1 y
  es VR puro.
- Telemetría y analítica completas.
- Modo VR verificado con gafas.

---

## 9. Verificación

El módulo se considera terminado cuando, **corriendo en modo PC**:

1. El flujo completo se recorre de principio a fin sin intervención manual.
2. La consola queda en cero errores.
3. Cambiar el idioma en el módulo 0 cambia todos los textos del módulo 1.
4. Ningún texto visible está escrito en código.
5. Elegir un elemento opcional no produce penalización en el reporte.
6. El reporte distingue las cuatro categorías.
7. Los tests de EditMode de binding pasan contra el prefab regenerado.
8. Cambiar el vehículo en el contexto carga otra secuencia sin tocar código.

---

## 10. Decisiones abiertas

1. ~~Qué vehículo es el primero y con qué modelo se construye.~~
   **Resuelto (2026-10-05): e-bike.** Se construye sobre un cubo con colliders
   nombrados por punto de interés. El perfil del vehículo referencia el modelo,
   así que sustituirlo por el entregable de 3D no toca lógica ni datos.
2. Quién genera los audios de las líneas nuevas con `GenerateAudioFIle`.
3. Si `MenuRuntimeTextLocalizer` se retira ya o después.
4. Cuánto tiempo conviven las dos versiones de Unity.
