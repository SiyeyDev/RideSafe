# Tutorial agnóstico: plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Un tutorial independiente de los módulos que, en `RideSafe_Garaje_v5`, muestra el control Touch Plus con el botón que hay que presionar, resalta dónde actuar y, la primera vez, da una frase corta. No modifica ningún módulo.

**Architecture:** Un gestor por escena (`TutorialHelpManager`) envuelve una lógica sin Unity (`HelpRules`, `HelpDirector`), que decide qué mostrar a partir de lo que encuentra un observador:
- `TargetScanner` busca los objetivos visibles;
- `ClickWitness` detecta los clics en el propio objetivo;
- `QuietDetector` pausa la ayuda durante videos y narración;
- `HandTracker` sigue la mano dominante elegida en Comfort.

La parte visual son `GestureHintView` (el Touch Plus 3D) y `TargetHighlighter` (titileo 2D y halo 3D). Los módulos solo pueden llamarlo, opcionalmente, mediante la fachada estática `TutorialHelp`.

**Tech Stack:** Unity 6000.4.9f1, URP, uGUI, TextMeshPro, I2 Localization, NUnit (Unity Test Framework), puente del editor `Temp/ridesafe-cmd.txt`.

**Spec:** `Docs/superpowers/specs/2026-10-15-tutorial-agnostico-design.md`

## Global Constraints

- No se modifica código ni assets de los módulos 0 y 1. Los únicos cambios en la escena v5 son retirar el objeto raíz `Tutorial` (inactivo) y añadir el objeto raíz `TutorialHelp`.
- No se tocan el rig de AutoHand (manos, `UIPointer`, `AutoInputModule`), XR/OpenXR, ni la iluminación ni el horneado.
- Editor: **6000.4.9f1**. No se cambian `ProjectSettings` ni `ProjectVersion.txt`.
- **No se hace commit** hasta que el usuario lo pida. Cada tarea termina en un punto de control sin commit.
- Código nuevo en `Assets/_Project/Systems/TutorialHelp/` (ensamblados `TutorialHelp*`), espacio de nombres **`RideSafe.Help`**, con comentarios en inglés (como el resto de `Systems/`). El namespace **no** puede ser `RideSafe.TutorialHelp`: desde el código de un módulo en `RideSafe.*`, el nombre `TutorialHelp` resolvería al namespace y no a la clase, y `TutorialHelp.Stop()` no compilaría.
- Atasco: **10 s** sin clic. Espera antes de la primera vez: **1 s**. Búsqueda de objetivos cada **0,2 s**; la lista de objetos 3D se refresca cada **1 s**.
- Campo de visión: hasta **35°** desde la mirada (cono de ~70°). Distancia máxima: **8 m**, la longitud del rayo de AutoHand.
- Narración: un `AudioSource` sonando, no-bucle, con clip de **más de 2 s**.
- Titileo 2D: blanco → gris **0,85** → blanco, en **1,6 s**, con curva coseno. Se aplica en `CanvasRenderer` y se restaura exacto.
- Halo 3D: Mint `#A9E2D4`. Texto corto solo la primera vez de cada tipo, en `es-CO` y `en` vía I2.
- Mano: la dominante de `ComfortSettingsPanel.onStart`; la derecha antes de elegirla.
- Lo aprendido dura la sesión; no se guarda nada en el dispositivo.

## Review Focus

1. **Clics durante una pausa no enseñan:** si la primera interacción 3D ocurre con el observador detenido, la ayuda de primera vez debe salir en la siguiente parte observada. Lo cubre `Stopped_ShowsNothing_AndIgnoresClicks` (Task 1).
2. **Varias opciones resaltadas por igual:** en atasco, todas las opciones visibles reciben el mismo resaltado, nunca una sola. Lo cubren `Stuck_HighlightsEveryVisibleTarget_WithoutText` (Task 2) y `SeveralGraphics_PulseInStep` (Task 4).
3. **Llamadas sin tutorial en la escena:** un módulo o un test que llama `TutorialHelp.*` sin gestor no debe fallar. Lo cubre `WithoutTutorial_CallsAreHarmless` (Task 2).
4. **El color del botón vuelve exacto** aunque la ayuda se corte a mitad de un titileo, incluido un `CanvasRenderer` que no era blanco. Lo cubre `Pulse_DimsTheGraphic_AndRestoresItExactly` (Task 4).
5. **Gesto sin ese botón en el modelo:** si un gesto pide un control que el Touch Plus no tiene, la vista se oculta y avisa, sin excepción. Lo cubre `Show_WithAMissingButton_HidesAndWarns` (Task 5).

## Cómo correr las cosas (para todas las tareas)

El editor de Unity debe estar abierto con el proyecto. Los comandos se envían con el script `ucmd.sh` del scratchpad:

```bash
S="C:/Users/ccscc/AppData/Local/Temp/claude/c--Users-ccscc-Documents-Trabajo-Freelance-RideSafe/39d163a1-44d0-46e2-aec1-c704db46eb11/scratchpad"
"$S/ucmd.sh" refresh 300                                   # importar y compilar
tail -n 300 "$LOCALAPPDATA/Unity/Editor/Editor.log" | grep -a "error CS" | sort -u   # errores de compilación
"$S/ucmd.sh" "editmode TutorialHelp.Tests" 590             # tests del tutorial
"$S/ucmd.sh" "exec Namespace.Type.Method" 300              # ejecutar un método estático sin parámetros
```

`editmode` escribe `passed=N failed=M ...` y una línea `FAIL <test>` por cada fallo.

---

### Task 1: Ensamblados y reglas de ayuda

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/TutorialHelp.asmdef`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/AssemblyInfo.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/InteractionKind.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpDecision.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpRules.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/TutorialHelp.Tests.asmdef`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/HelpRulesTests.cs`

**Interfaces:**
- Produces: `enum InteractionKind { Interface, Object3D }`; `enum HelpMode { None, FirstTime, Stuck }`; `readonly struct HelpDecision(HelpMode mode, InteractionKind kind)` con `Mode`, `Kind`, `HelpDecision.None`; `sealed class HelpRules` con `SettleSeconds = 1f`, `StuckSeconds = 10f`, `bool Observing`, `bool IsLearned(InteractionKind)`, `void Stop()`, `void Resume()`, `void Clicked(InteractionKind)`, `HelpDecision Tick(float deltaTime, bool interfaceVisible, bool objectVisible, bool quiet)`.

- [ ] **Step 1: Crear los asmdef y AssemblyInfo**

`Assets/_Project/Systems/TutorialHelp/Runtime/TutorialHelp.asmdef`:
```json
{
    "name": "TutorialHelp",
    "rootNamespace": "RideSafe.Help",
    "references": [
        "Tutorial",
        "Tutorial.Presentation",
        "Cachacos",
        "RideSafe.UI",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/AssemblyInfo.cs`:
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TutorialHelp.Tests")]
[assembly: InternalsVisibleTo("TutorialHelp.Editor")]
```

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/TutorialHelp.Tests.asmdef`:
```json
{
    "name": "TutorialHelp.Tests",
    "rootNamespace": "RideSafe.Help.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "TutorialHelp",
        "Tutorial",
        "Tutorial.Presentation",
        "Cachacos",
        "RideSafe.UI",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Escribir los tests que fallan**

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/HelpRulesTests.cs`:
```csharp
using NUnit.Framework;

namespace RideSafe.Help.Tests
{
    public class HelpRulesTests
    {
        private static HelpDecision Run(HelpRules rules, float seconds, bool ui, bool obj, bool quiet = false)
        {
            const float step = 0.1f;
            HelpDecision decision = HelpDecision.None;
            int steps = UnityEngine.Mathf.RoundToInt(seconds / step);
            for (int i = 0; i < steps; i++)
                decision = rules.Tick(step, ui, obj, quiet);
            return decision;
        }

        [Test]
        public void NoTargets_NoHelp()
        {
            Assert.AreEqual(HelpMode.None, Run(new HelpRules(), 20f, false, false).Mode);
        }

        [Test]
        public void FirstInterfaceTarget_WaitsToSettle_ThenTeaches()
        {
            HelpRules rules = new HelpRules();
            Assert.AreEqual(HelpMode.None, Run(rules, 0.5f, true, false).Mode, "the screen is still arriving");
            HelpDecision decision = Run(rules, 0.6f, true, false);
            Assert.AreEqual(HelpMode.FirstTime, decision.Mode);
            Assert.AreEqual(InteractionKind.Interface, decision.Kind);
        }

        [Test]
        public void AClick_LearnsTheKind_ForTheSession()
        {
            HelpRules rules = new HelpRules();
            Run(rules, 2f, true, false);
            rules.Clicked(InteractionKind.Interface);
            Assert.IsTrue(rules.IsLearned(InteractionKind.Interface));
            Assert.AreEqual(HelpMode.None, Run(rules, 5f, true, false).Mode);
        }

        [Test]
        public void ObjectKind_IsTaughtSeparately()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            HelpDecision decision = Run(rules, 1.2f, true, true);
            Assert.AreEqual(HelpMode.FirstTime, decision.Mode);
            Assert.AreEqual(InteractionKind.Object3D, decision.Kind);
        }

        [Test]
        public void TenSecondsWithoutAClick_IsStuck_EvenWhenLearned()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Assert.AreEqual(HelpMode.None, Run(rules, 9.8f, true, false).Mode);
            Assert.AreEqual(HelpMode.Stuck, Run(rules, 0.3f, true, false).Mode);
        }

        [Test]
        public void AnyClick_RestartsTheStuckCount()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 9f, true, false);
            rules.Clicked(InteractionKind.Interface);
            Assert.AreEqual(HelpMode.None, Run(rules, 9f, true, false).Mode);
        }

        [Test]
        public void Quiet_FreezesTheCount_AndShowsNothing()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 6f, true, false);
            Assert.AreEqual(HelpMode.None, Run(rules, 30f, true, false, quiet: true).Mode);
            Assert.AreEqual(HelpMode.Stuck, Run(rules, 4.2f, true, false).Mode, "6 s before + 4.2 s after the narration");
        }

        [Test]
        public void Quiet_DelaysTheFirstTimeHelp()
        {
            HelpRules rules = new HelpRules();
            Assert.AreEqual(HelpMode.None, Run(rules, 5f, true, false, quiet: true).Mode);
            Assert.AreEqual(HelpMode.FirstTime, Run(rules, 1.1f, true, false).Mode);
        }

        [Test]
        public void LosingAllTargets_RestartsTheCount()
        {
            HelpRules rules = new HelpRules();
            rules.Clicked(InteractionKind.Interface);
            Run(rules, 9f, true, false);
            Run(rules, 0.2f, false, false);
            Assert.AreEqual(HelpMode.None, Run(rules, 9f, true, false).Mode);
        }

        [Test]
        public void Stopped_ShowsNothing_AndIgnoresClicks()
        {
            HelpRules rules = new HelpRules();
            rules.Stop();
            Assert.AreEqual(HelpMode.None, Run(rules, 20f, true, true).Mode);
            rules.Clicked(InteractionKind.Object3D);
            Assert.IsFalse(rules.IsLearned(InteractionKind.Object3D), "a click during a pause teaches nothing");
            rules.Resume();
            rules.Clicked(InteractionKind.Interface);
            HelpDecision decision = Run(rules, 1.2f, false, true);
            Assert.AreEqual(HelpMode.FirstTime, decision.Mode);
            Assert.AreEqual(InteractionKind.Object3D, decision.Kind);
        }

        [Test]
        public void Stop_IsASimpleSwitch()
        {
            HelpRules rules = new HelpRules();
            rules.Stop();
            rules.Stop();
            rules.Resume();
            Assert.IsTrue(rules.Observing);
        }
    }
}
```

- [ ] **Step 3: Compilar y comprobar que falla**

Run: `"$S/ucmd.sh" refresh 300`, y después revisa el log con el `grep "error CS"`.
Expected: `error CS0246` por `HelpRules`, `HelpDecision`, `HelpMode` e `InteractionKind`, que aún no existen.

- [ ] **Step 4: Implementar**

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/InteractionKind.cs`:
```csharp
namespace RideSafe.Help
{
    /// <summary>What the learner has to point at: a control on a canvas or an object in the world.</summary>
    public enum InteractionKind
    {
        Interface = 0,
        Object3D = 1
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpDecision.cs`:
```csharp
using System;

namespace RideSafe.Help
{
    public enum HelpMode
    {
        None = 0,
        /// <summary>First time this kind of interaction appears in the session: controller, highlight and text.</summary>
        FirstTime = 1,
        /// <summary>No click for a while: controller and highlight, no text.</summary>
        Stuck = 2
    }

    /// <summary>What the automatic help should show right now.</summary>
    public readonly struct HelpDecision : IEquatable<HelpDecision>
    {
        public static readonly HelpDecision None = new HelpDecision(HelpMode.None, InteractionKind.Interface);

        public HelpDecision(HelpMode mode, InteractionKind kind)
        {
            Mode = mode;
            Kind = kind;
        }

        public HelpMode Mode { get; }
        public InteractionKind Kind { get; }

        public bool Equals(HelpDecision other) => Mode == other.Mode && (Mode == HelpMode.None || Kind == other.Kind);
        public override bool Equals(object obj) => obj is HelpDecision other && Equals(other);
        public override int GetHashCode() => Mode == HelpMode.None ? 0 : (int)Mode * 31 + (int)Kind;
        public override string ToString() => Mode == HelpMode.None ? "None" : Mode + "(" + Kind + ")";
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpRules.cs`:
```csharp
using System.Collections.Generic;

namespace RideSafe.Help
{
    /// <summary>
    /// When the automatic help appears. The first time a kind of interaction is on screen it
    /// teaches (after the screen settles); a click on that kind learns it for the session.
    /// Ten seconds without any click is "stuck", learned or not. Narration or video ("quiet")
    /// freezes the count and shows nothing. Stopped: nothing, and clicks teach nothing.
    /// </summary>
    public sealed class HelpRules
    {
        public const float SettleSeconds = 1f;
        public const float StuckSeconds = 10f;

        private readonly HashSet<InteractionKind> _learned = new HashSet<InteractionKind>();
        private float _settled;
        private float _idle;

        public bool Observing { get; private set; } = true;

        public bool IsLearned(InteractionKind kind) => _learned.Contains(kind);

        public void Stop()
        {
            Observing = false;
            _settled = 0f;
            _idle = 0f;
        }

        public void Resume()
        {
            Observing = true;
            _settled = 0f;
            _idle = 0f;
        }

        public void Clicked(InteractionKind kind)
        {
            if (!Observing)
                return;
            _learned.Add(kind);
            _idle = 0f;
        }

        public HelpDecision Tick(float deltaTime, bool interfaceVisible, bool objectVisible, bool quiet)
        {
            if (!Observing || (!interfaceVisible && !objectVisible))
            {
                _settled = 0f;
                _idle = 0f;
                return HelpDecision.None;
            }
            if (quiet)
                return HelpDecision.None;

            _settled += deltaTime;
            _idle += deltaTime;
            if (_settled >= SettleSeconds)
            {
                if (interfaceVisible && !IsLearned(InteractionKind.Interface))
                    return new HelpDecision(HelpMode.FirstTime, InteractionKind.Interface);
                if (objectVisible && !IsLearned(InteractionKind.Object3D))
                    return new HelpDecision(HelpMode.FirstTime, InteractionKind.Object3D);
            }
            if (_idle >= StuckSeconds)
                return new HelpDecision(HelpMode.Stuck, interfaceVisible ? InteractionKind.Interface : InteractionKind.Object3D);
            return HelpDecision.None;
        }
    }
}
```

- [ ] **Step 5: Correr los tests**

Run: `"$S/ucmd.sh" refresh 300`, y luego `"$S/ucmd.sh" "editmode TutorialHelp.Tests" 590`.
Expected: `passed=11 failed=0`.

- [ ] **Step 6: Punto de control (sin commit).** Revisa con `git status` que solo aparecen archivos nuevos bajo `Systems/TutorialHelp/`.

---

### Task 2: Gesto, gestor lógico y fachada `TutorialHelp`

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/TutorialGestureSO.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpTextKeys.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpTarget.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/IHelpViews.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpDirector.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Api/ITutorialHelpService.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Api/TutorialHelp.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/HelpDirectorTests.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/TutorialHelpFacadeTests.cs`

**Interfaces:**
- Consumes: `HelpRules`, `HelpDecision`, `InteractionKind` (Task 1).
- Produces:
  - `TutorialGestureSO`: `ControllerControl Control`, `HandChoice Hand` (`enum HandChoice { Dominant, Left, Right }`), `string TextKeyFor(InteractionKind)`, `internal void Configure(ControllerControl, HandChoice, string interfaceKey, string objectKey)`.
  - `HelpTextKeys.SelectInterface = "TutorialHelp/SelectInterface"` y `HelpTextKeys.SelectObject = "TutorialHelp/SelectObject"`.
  - `readonly struct HelpTarget(Transform, InteractionKind, Graphic)` con `Transform`, `Kind`, `Graphic`, `Vector3 Position`, `static HelpTarget From(Transform)`.
  - `interface IGestureHintView { void Show(TutorialGestureSO, XRNodeRole, string text, Vector3? focus); void Hide(); bool IsVisible { get; } }`.
  - `interface ITargetHighlighter { void Highlight(IReadOnlyList<HelpTarget>); void Clear(); }`.
  - `interface ITutorialHelpService { IDisposable Show(TutorialGestureSO, IReadOnlyList<Transform>); void Stop(); void Resume(); bool Observing { get; } }`.
  - `HelpDirector : ITutorialHelpService`: ctor `(TutorialGestureSO, IGestureHintView, ITargetHighlighter, Func<string,string> translate)`, `DominantHand Hand`, `HelpDecision Decision`, `bool IsLearned(InteractionKind)`, `void Clicked(InteractionKind)`, `void Update(float, IReadOnlyList<HelpTarget>, bool quiet)`.
  - `static class TutorialHelp`: `Show(TutorialGestureSO, params Transform[])`, `Stop()`, `Resume()`, `Pause()`, `bool IsAvailable`, `bool IsObserving`, `internal Register(ITutorialHelpService)` y `internal Unregister(ITutorialHelpService)`.

- [ ] **Step 1: Escribir los tests que fallan**

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/HelpDirectorTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    internal sealed class FakeHintView : IGestureHintView
    {
        public TutorialGestureSO Gesture;
        public XRNodeRole Hand;
        public string Text;
        public bool Visible;
        public void Show(TutorialGestureSO gesture, XRNodeRole hand, string text, Vector3? focus)
        {
            Gesture = gesture;
            Hand = hand;
            Text = text;
            Visible = true;
        }
        public void Hide() => Visible = false;
        public bool IsVisible => Visible;
    }

    internal sealed class FakeHighlighter : ITargetHighlighter
    {
        public readonly List<HelpTarget> Current = new List<HelpTarget>();
        public void Highlight(IReadOnlyList<HelpTarget> targets)
        {
            Current.Clear();
            Current.AddRange(targets);
        }
        public void Clear() => Current.Clear();
    }

    public class HelpDirectorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private TutorialGestureSO _gesture;
        private FakeHintView _view;
        private FakeHighlighter _highlighter;
        private HelpDirector _director;

        [SetUp]
        public void SetUp()
        {
            _gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "k.ui", "k.obj");
            _view = new FakeHintView();
            _highlighter = new FakeHighlighter();
            _director = new HelpDirector(_gesture, _view, _highlighter, key => "T:" + key);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
            Object.DestroyImmediate(_gesture);
        }

        private HelpTarget Target(InteractionKind kind)
        {
            GameObject go = new GameObject(kind.ToString());
            _objects.Add(go);
            return new HelpTarget(go.transform, kind, null);
        }

        private void Run(float seconds, params HelpTarget[] visible)
        {
            int steps = Mathf.RoundToInt(seconds / 0.1f);
            for (int i = 0; i < steps; i++)
                _director.Update(0.1f, visible, false);
        }

        [Test]
        public void FirstTime_ShowsControllerHighlightAndText()
        {
            HelpTarget a = Target(InteractionKind.Interface), b = Target(InteractionKind.Interface);
            Run(1.2f, a, b);
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual("T:k.ui", _view.Text);
            Assert.AreEqual(XRNodeRole.RightHand, _view.Hand, "right until Comfort says otherwise");
            Assert.AreEqual(2, _highlighter.Current.Count, "every option, alike");
        }

        [Test]
        public void FirstTime_HighlightsOnlyThatKind()
        {
            HelpTarget ui = Target(InteractionKind.Interface), obj = Target(InteractionKind.Object3D);
            _director.Clicked(InteractionKind.Interface);
            Run(1.2f, ui, obj);
            Assert.AreEqual("T:k.obj", _view.Text);
            Assert.AreEqual(1, _highlighter.Current.Count);
            Assert.AreEqual(obj, _highlighter.Current[0]);
        }

        [Test]
        public void Stuck_HighlightsEveryVisibleTarget_WithoutText()
        {
            HelpTarget ui = Target(InteractionKind.Interface), obj = Target(InteractionKind.Object3D);
            _director.Clicked(InteractionKind.Interface);
            _director.Clicked(InteractionKind.Object3D);
            Run(10.2f, ui, obj);
            Assert.AreEqual(HelpMode.Stuck, _director.Decision.Mode);
            Assert.IsTrue(_view.Visible);
            Assert.IsNull(_view.Text);
            Assert.AreEqual(2, _highlighter.Current.Count);
        }

        [Test]
        public void ADominantLeftHand_ShowsTheLeftController()
        {
            _director.Hand = DominantHand.Left;
            Run(1.2f, Target(InteractionKind.Interface));
            Assert.AreEqual(XRNodeRole.LeftHand, _view.Hand);
        }

        [Test]
        public void AClick_HidesTheFirstTimeHelp()
        {
            HelpTarget ui = Target(InteractionKind.Interface);
            Run(1.2f, ui);
            _director.Clicked(InteractionKind.Interface);
            Run(0.1f, ui);
            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(0, _highlighter.Current.Count);
        }

        [Test]
        public void Stop_HidesTheAutomaticHelp()
        {
            HelpTarget ui = Target(InteractionKind.Interface);
            Run(1.2f, ui);
            _director.Stop();
            Run(0.1f);
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void ExplicitShow_WorksWhileStopped_AndHidesOnDispose()
        {
            HelpTarget handlebar = Target(InteractionKind.Object3D);
            _director.Stop();
            System.IDisposable help = _director.Show(_gesture, new[] { handlebar.Transform });
            Assert.IsTrue(_view.Visible);
            Assert.AreEqual(1, _highlighter.Current.Count);
            Assert.IsNull(_view.Text, "an explicit call shows no text unless the module adds its own");
            help.Dispose();
            Assert.IsFalse(_view.Visible);
            help.Dispose();
        }

        [Test]
        public void ExplicitShow_WithoutAGesture_UsesTheDefaultOne()
        {
            _director.Show(null, new Transform[0]);
            Assert.AreSame(_gesture, _view.Gesture);
        }
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/TutorialHelpFacadeTests.cs`:
```csharp
using NUnit.Framework;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    public class TutorialHelpFacadeTests
    {
        private TutorialGestureSO _gesture;
        private HelpDirector _director;

        [SetUp]
        public void SetUp()
        {
            _gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            _director = new HelpDirector(_gesture, new FakeHintView(), new FakeHighlighter(), key => key);
        }

        [TearDown]
        public void TearDown()
        {
            TutorialHelp.Unregister(_director);
            Object.DestroyImmediate(_gesture);
        }

        [Test]
        public void WithoutTutorial_CallsAreHarmless()
        {
            Assert.IsFalse(TutorialHelp.IsAvailable);
            Assert.DoesNotThrow(() =>
            {
                TutorialHelp.Stop();
                TutorialHelp.Resume();
                using (TutorialHelp.Pause()) { }
                TutorialHelp.Show(null).Dispose();
            });
            Assert.IsFalse(TutorialHelp.IsObserving);
        }

        [Test]
        public void StopAndResume_ReachTheTutorial()
        {
            TutorialHelp.Register(_director);
            TutorialHelp.Stop();
            Assert.IsFalse(_director.Observing);
            TutorialHelp.Resume();
            Assert.IsTrue(_director.Observing);
        }

        [Test]
        public void Pause_StopsForTheBlock_ThenResumes()
        {
            TutorialHelp.Register(_director);
            using (TutorialHelp.Pause())
                Assert.IsFalse(TutorialHelp.IsObserving);
            Assert.IsTrue(TutorialHelp.IsObserving);
        }

        [Test]
        public void Unregister_LeavesTheCallsHarmlessAgain()
        {
            TutorialHelp.Register(_director);
            TutorialHelp.Unregister(_director);
            Assert.IsFalse(TutorialHelp.IsAvailable);
        }
    }
}
```

- [ ] **Step 2: Compilar y comprobar que falla**

Run: `"$S/ucmd.sh" refresh 300`, y luego el `grep "error CS"`.
Expected: `CS0246` por `TutorialGestureSO`, `HelpDirector`, `HelpTarget`, `IGestureHintView`, `ITargetHighlighter` y `TutorialHelp`.

- [ ] **Step 3: Implementar**

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/TutorialGestureSO.cs`:
```csharp
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// One gesture the tutorial can show: which Touch Plus control lights up and is pressed,
    /// on which hand, and the short text for the first time. A new gesture is a new asset.
    /// </summary>
    [CreateAssetMenu(menuName = "RideSafe/Tutorial Help/Gesture", fileName = "SO_Gesture_")]
    public class TutorialGestureSO : ScriptableObject
    {
        public enum HandChoice
        {
            Dominant = 0,
            Left = 1,
            Right = 2
        }

        [SerializeField] private ControllerControl _control = ControllerControl.Trigger;
        [SerializeField] private HandChoice _hand = HandChoice.Dominant;
        [Tooltip("I2 term shown the first time on canvas controls. Empty: no text.")]
        [SerializeField] private string _interfaceTextKey = HelpTextKeys.SelectInterface;
        [Tooltip("I2 term shown the first time on objects in the world. Empty: no text.")]
        [SerializeField] private string _objectTextKey = HelpTextKeys.SelectObject;

        public ControllerControl Control => _control;
        public HandChoice Hand => _hand;

        public string TextKeyFor(InteractionKind kind) =>
            kind == InteractionKind.Interface ? _interfaceTextKey : _objectTextKey;

        internal void Configure(ControllerControl control, HandChoice hand, string interfaceKey, string objectKey)
        {
            _control = control;
            _hand = hand;
            _interfaceTextKey = interfaceKey;
            _objectTextKey = objectKey;
        }
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpTextKeys.cs`:
```csharp
namespace RideSafe.Help
{
    /// <summary>I2 terms of the short first-time texts (added by Localization/Editor/TutorialHelpTerms).</summary>
    public static class HelpTextKeys
    {
        public const string SelectInterface = "TutorialHelp/SelectInterface";
        public const string SelectObject = "TutorialHelp/SelectObject";
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpTarget.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>Something the learner can point at and select, as the tutorial sees it.</summary>
    public readonly struct HelpTarget : IEquatable<HelpTarget>
    {
        public HelpTarget(Transform transform, InteractionKind kind, Graphic graphic)
        {
            Transform = transform;
            Kind = kind;
            Graphic = graphic;
        }

        public Transform Transform { get; }
        public InteractionKind Kind { get; }
        /// <summary>The image that pulses (canvas controls only).</summary>
        public Graphic Graphic { get; }

        public Vector3 Position
        {
            get
            {
                if (Transform == null)
                    return Vector3.zero;
                if (Kind == InteractionKind.Object3D)
                {
                    Collider collider = Transform.GetComponentInChildren<Collider>(false);
                    if (collider != null)
                        return collider.bounds.center;
                }
                return Transform.position;
            }
        }

        /// <summary>Classifies a transform a module passed in: on a canvas it is a control, otherwise an object.</summary>
        public static HelpTarget From(Transform transform)
        {
            if (transform != null && transform.GetComponentInParent<Canvas>() != null)
            {
                Selectable selectable = transform.GetComponent<Selectable>();
                Graphic graphic = selectable != null && selectable.targetGraphic != null
                    ? selectable.targetGraphic
                    : transform.GetComponent<Graphic>();
                return new HelpTarget(transform, InteractionKind.Interface, graphic);
            }
            return new HelpTarget(transform, InteractionKind.Object3D, null);
        }

        public bool Equals(HelpTarget other) => Transform == other.Transform;
        public override bool Equals(object obj) => obj is HelpTarget other && Equals(other);
        public override int GetHashCode() => Transform != null ? Transform.GetHashCode() : 0;
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/IHelpViews.cs`:
```csharp
using System.Collections.Generic;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>The Touch Plus controller with the gesture's control lit and pressed.</summary>
    public interface IGestureHintView
    {
        /// <summary>Idempotent: called every frame while help is on; only a change restarts the animation.</summary>
        void Show(TutorialGestureSO gesture, XRNodeRole hand, string text, Vector3? focus);
        void Hide();
        bool IsVisible { get; }
    }

    /// <summary>Marks where to act: soft pulse on canvas controls, mint halo on objects.</summary>
    public interface ITargetHighlighter
    {
        /// <summary>Highlights exactly these targets; any other one highlighted before is restored.</summary>
        void Highlight(IReadOnlyList<HelpTarget> targets);
        void Clear();
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Api/ITutorialHelpService.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>What a scene's tutorial offers to modules (through <see cref="TutorialHelp"/>).</summary>
    public interface ITutorialHelpService
    {
        IDisposable Show(TutorialGestureSO gesture, IReadOnlyList<Transform> targets);
        void Stop();
        void Resume();
        bool Observing { get; }
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Core/HelpDirector.cs`:
```csharp
using System;
using System.Collections.Generic;
using RideSafe.Tutorial;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// Decides what the views show, without Unity's frame loop: the automatic help from
    /// <see cref="HelpRules"/> and the explicit requests from modules (the latest wins, and
    /// they work while observation is stopped).
    /// </summary>
    public sealed class HelpDirector : ITutorialHelpService
    {
        private readonly HelpRules _rules = new HelpRules();
        private readonly TutorialGestureSO _gesture;
        private readonly IGestureHintView _view;
        private readonly ITargetHighlighter _highlighter;
        private readonly Func<string, string> _translate;
        private readonly List<Request> _requests = new List<Request>();
        private readonly List<HelpTarget> _visible = new List<HelpTarget>();
        private readonly List<HelpTarget> _shown = new List<HelpTarget>();

        public HelpDirector(TutorialGestureSO gesture, IGestureHintView view, ITargetHighlighter highlighter,
                            Func<string, string> translate)
        {
            _gesture = gesture;
            _view = view;
            _highlighter = highlighter;
            _translate = translate;
        }

        public DominantHand Hand { get; set; } = DominantHand.Right;
        public HelpDecision Decision { get; private set; } = HelpDecision.None;
        public bool Observing => _rules.Observing;
        public bool IsLearned(InteractionKind kind) => _rules.IsLearned(kind);

        public void Clicked(InteractionKind kind) => _rules.Clicked(kind);

        public void Update(float deltaTime, IReadOnlyList<HelpTarget> visible, bool quiet)
        {
            _visible.Clear();
            bool ui = false, obj = false;
            foreach (HelpTarget target in visible)
            {
                _visible.Add(target);
                if (target.Kind == InteractionKind.Interface)
                    ui = true;
                else
                    obj = true;
            }
            Decision = _rules.Tick(deltaTime, ui, obj, quiet);
            Render();
        }

        public IDisposable Show(TutorialGestureSO gesture, IReadOnlyList<Transform> targets)
        {
            Request request = new Request(this, gesture != null ? gesture : _gesture, targets);
            _requests.Add(request);
            Render();
            return request;
        }

        public void Stop()
        {
            _rules.Stop();
            Decision = HelpDecision.None;
            Render();
        }

        public void Resume() => _rules.Resume();

        private void Render()
        {
            if (_requests.Count > 0)
            {
                Request request = _requests[_requests.Count - 1];
                Present(request.Gesture, request.Targets, null);
                return;
            }
            if (Decision.Mode == HelpMode.None)
            {
                _view?.Hide();
                _highlighter?.Clear();
                return;
            }

            _shown.Clear();
            foreach (HelpTarget target in _visible)
            {
                if (Decision.Mode == HelpMode.Stuck || target.Kind == Decision.Kind)
                    _shown.Add(target);
            }
            string text = Decision.Mode == HelpMode.FirstTime ? Translate(_gesture.TextKeyFor(Decision.Kind)) : null;
            Present(_gesture, _shown, text);
        }

        private void Present(TutorialGestureSO gesture, IReadOnlyList<HelpTarget> targets, string text)
        {
            _view?.Show(gesture, HandFor(gesture), text, Centroid(targets));
            _highlighter?.Highlight(targets);
        }

        private string Translate(string key)
        {
            if (string.IsNullOrEmpty(key) || _translate == null)
                return null;
            string text = _translate(key);
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private XRNodeRole HandFor(TutorialGestureSO gesture)
        {
            switch (gesture != null ? gesture.Hand : TutorialGestureSO.HandChoice.Dominant)
            {
                case TutorialGestureSO.HandChoice.Left: return XRNodeRole.LeftHand;
                case TutorialGestureSO.HandChoice.Right: return XRNodeRole.RightHand;
                default: return Hand == DominantHand.Left ? XRNodeRole.LeftHand : XRNodeRole.RightHand;
            }
        }

        private static Vector3? Centroid(IReadOnlyList<HelpTarget> targets)
        {
            if (targets == null || targets.Count == 0)
                return null;
            Vector3 sum = Vector3.zero;
            foreach (HelpTarget target in targets)
                sum += target.Position;
            return sum / targets.Count;
        }

        private void Remove(Request request)
        {
            if (_requests.Remove(request))
                Render();
        }

        private sealed class Request : IDisposable
        {
            private HelpDirector _owner;

            public Request(HelpDirector owner, TutorialGestureSO gesture, IReadOnlyList<Transform> targets)
            {
                _owner = owner;
                Gesture = gesture;
                Targets = new List<HelpTarget>();
                if (targets == null)
                    return;
                foreach (Transform target in targets)
                {
                    if (target != null)
                        Targets.Add(HelpTarget.From(target));
                }
            }

            public TutorialGestureSO Gesture { get; }
            public List<HelpTarget> Targets { get; }

            public void Dispose()
            {
                HelpDirector owner = _owner;
                _owner = null;
                owner?.Remove(this);
            }
        }
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Api/TutorialHelp.cs`:
```csharp
using System;
using System.Collections.Generic;
using Cachacos;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// The only door modules use, and only when the automatic help cannot see what they need.
    /// With no tutorial in the scene (tests, other scenes) every call does nothing, so a module
    /// never fails because of the tutorial.
    /// <code>
    /// IDisposable help = TutorialHelp.Show(gesture, handlebar);  ...  help.Dispose();
    /// TutorialHelp.Stop();  ...  TutorialHelp.Resume();
    /// using (TutorialHelp.Pause()) { ... }
    /// </code>
    /// </summary>
    public static class TutorialHelp
    {
        private static ITutorialHelpService _service;

        public static bool IsAvailable => Service != null;
        public static bool IsObserving => Service?.Observing ?? false;

        /// <summary>Shows the gesture on these targets until the returned handle is disposed. Works while stopped.</summary>
        public static IDisposable Show(TutorialGestureSO gesture, params Transform[] targets) =>
            Service?.Show(gesture, targets ?? Array.Empty<Transform>()) ?? NoHelp.Instance;

        /// <summary>From here on the tutorial does not observe (no first-time or stuck help).</summary>
        public static void Stop() => Service?.Stop();

        /// <summary>The tutorial observes again; the stuck count starts over.</summary>
        public static void Resume() => Service?.Resume();

        /// <summary>Stop now, Resume when the returned handle is disposed (for a using block).</summary>
        public static IDisposable Pause()
        {
            ITutorialHelpService service = Service;
            if (service == null)
                return NoHelp.Instance;
            service.Stop();
            return new Resumer(service);
        }

        internal static void Register(ITutorialHelpService service)
        {
            _service = service;
            ServiceLocator.Instance.RegisterService(service);
        }

        internal static void Unregister(ITutorialHelpService service)
        {
            if (!ReferenceEquals(_service, service))
                return;
            _service = null;
            ServiceLocator.Instance.TryDeregisterService(service);
        }

        private static ITutorialHelpService Service
        {
            get
            {
                if (_service is UnityEngine.Object unityObject && unityObject == null)
                    _service = null; // destroyed with its scene
                return _service;
            }
        }

        private sealed class NoHelp : IDisposable
        {
            public static readonly NoHelp Instance = new NoHelp();
            public void Dispose() { }
        }

        private sealed class Resumer : IDisposable
        {
            private ITutorialHelpService _service;
            public Resumer(ITutorialHelpService service) => _service = service;

            public void Dispose()
            {
                ITutorialHelpService service = _service;
                _service = null;
                service?.Resume();
            }
        }
    }
}
```

Nota: la fachada guarda el servicio en `_service` al registrarse y también lo registra en el `ServiceLocator`, como pide la spec. No llama a `RequestService` en cada uso, porque ese método escribe un `Debug.Log` cada vez que encuentra el servicio. `RegisterService<T>` se invoca con `T = ITutorialHelpService` por inferencia del parámetro.

- [ ] **Step 4: Correr los tests**

Run: `"$S/ucmd.sh" refresh 300`, y luego `"$S/ucmd.sh" "editmode TutorialHelp.Tests" 590`.
Expected: `passed=23 failed=0`.

- [ ] **Step 5: Punto de control (sin commit).**

---

### Task 3: Observador (objetivos, clics, pausas y mano)

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Observation/TargetScanner.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Observation/ClickWitness.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Observation/QuietDetector.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Observation/HandTracker.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/ObservationTests.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/FakeClickable.cs` (en su propio archivo: Unity solo encuentra un MonoBehaviour si vive en un archivo con su nombre)

**Interfaces:**
- Consumes: `HelpTarget`, `InteractionKind` (Task 2).
- Produces:
  - `TargetScanner`: `MaxAngleFromGaze = 35f`, `MaxDistance = 8f`, `ObjectRefreshSeconds = 1f`, `void Scan(Pose viewer, float elapsed, Transform ignore, List<HelpTarget> results)`, `static bool InView(Pose, Vector3)`, `static bool IsUsable(Selectable)`.
  - `ClickWitness : MonoBehaviour, IPointerClickHandler`: `InteractionKind Kind`, `event Action<ClickWitness> Clicked`, `static ClickWitness Attach(GameObject, InteractionKind)`.
  - `QuietDetector`: `NarrationMinSeconds = 2f`, `bool IsQuiet(float deltaTime)`, `static bool IsNarration(bool playing, bool loop, float clipSeconds)`.
  - `HandTracker : IDisposable`: `DominantHand Hand`, `void Listen(IEnumerable<ComfortSettingsPanel>)`.

- [ ] **Step 1: Escribir los tests que fallan**

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/FakeClickable.cs`:
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace RideSafe.Help.Tests
{
    /// <summary>Stands in for a module's clickable 3D object (like the EPP or vehicle points).</summary>
    public sealed class FakeClickable : MonoBehaviour, IPointerClickHandler
    {
        public int Clicks;
        public void OnPointerClick(PointerEventData eventData) => Clicks++;
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/ObservationTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RideSafe.Help.Tests
{
    public class ObservationTests
    {
        private static readonly Pose Viewer = new Pose(Vector3.zero, Quaternion.identity); // looking along +Z
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private Button WorldButton(Vector3 position, RenderMode mode = RenderMode.WorldSpace)
        {
            GameObject canvasGo = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            canvasGo.GetComponent<Canvas>().renderMode = mode;
            canvasGo.transform.position = position;
            GameObject buttonGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonGo.GetComponent<Image>();
            return button;
        }

        private List<HelpTarget> Scan(Transform ignore = null)
        {
            List<HelpTarget> results = new List<HelpTarget>();
            new TargetScanner().Scan(Viewer, 2f, ignore, results);
            return results;
        }

        [Test]
        public void Scanner_FindsAnInteractableButtonInView()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            List<HelpTarget> found = Scan();
            HelpTarget target = found.Find(t => t.Transform == button.transform);
            Assert.AreEqual(InteractionKind.Interface, target.Kind);
            Assert.AreSame(button.targetGraphic, target.Graphic);
        }

        [Test]
        public void Scanner_SkipsADisabledButton()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            button.interactable = false;
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform), "Continue before a choice is not a target");
        }

        [Test]
        public void Scanner_SkipsAButtonBehindTheLearner()
        {
            Button button = WorldButton(new Vector3(0f, 0f, -2f));
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void Scanner_SkipsAHiddenCanvasGroup()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            button.transform.parent.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void Scanner_SkipsScreenSpaceCanvases()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f), RenderMode.ScreenSpaceOverlay);
            Assert.IsFalse(Scan().Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void Scanner_FindsAClickableObject_AsObject3D()
        {
            GameObject cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            cube.transform.position = new Vector3(0.3f, 0f, 3f);
            cube.AddComponent<FakeClickable>();
            HelpTarget target = Scan().Find(t => t.Transform == cube.transform);
            Assert.AreEqual(cube.transform, target.Transform);
            Assert.AreEqual(InteractionKind.Object3D, target.Kind);
        }

        [Test]
        public void Scanner_SkipsWhatIsUnderTheTutorial()
        {
            GameObject tutorial = Track(new GameObject("TutorialHelp"));
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            button.transform.parent.SetParent(tutorial.transform, true);
            Assert.IsFalse(Scan(tutorial.transform).Exists(t => t.Transform == button.transform));
        }

        [Test]
        public void InView_RespectsAngleAndDistance()
        {
            Assert.IsTrue(TargetScanner.InView(Viewer, new Vector3(0f, 0f, 3f)));
            Assert.IsFalse(TargetScanner.InView(Viewer, new Vector3(0f, 0f, 9f)), "beyond the AutoHand ray");
            Assert.IsFalse(TargetScanner.InView(Viewer, new Vector3(3f, 0f, 1f)), "far off to the side");
        }

        [Test]
        public void Witness_HearsTheSameClickAsTheButton()
        {
            Button button = WorldButton(new Vector3(0f, 0f, 2f));
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);
            ClickWitness witness = ClickWitness.Attach(button.gameObject, InteractionKind.Interface);
            ClickWitness heard = null;
            witness.Clicked += w => heard = w;

            ExecuteEvents.Execute(button.gameObject, new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                                  ExecuteEvents.pointerClickHandler);

            Assert.AreEqual(1, clicks, "the module's own click still happens");
            Assert.AreSame(witness, heard);
            Assert.AreEqual(InteractionKind.Interface, heard.Kind);
            Assert.IsTrue((witness.hideFlags & HideFlags.DontSave) != 0, "never saved into the scene");
        }

        [Test]
        public void Witness_AttachIsIdempotent()
        {
            GameObject cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            ClickWitness first = ClickWitness.Attach(cube, InteractionKind.Object3D);
            Assert.AreSame(first, ClickWitness.Attach(cube, InteractionKind.Object3D));
            Assert.AreEqual(1, cube.GetComponents<ClickWitness>().Length);
        }

        [Test]
        public void Narration_IsALongNonLoopingSound()
        {
            Assert.IsTrue(QuietDetector.IsNarration(true, false, 3f));
            Assert.IsFalse(QuietDetector.IsNarration(true, true, 30f), "ambient loop");
            Assert.IsFalse(QuietDetector.IsNarration(true, false, 0.5f), "click sound");
            Assert.IsFalse(QuietDetector.IsNarration(false, false, 3f), "not playing");
        }

        [Test]
        public void HandTracker_IsRightUntilComfortSaysOtherwise()
        {
            GameObject panelGo = Track(new GameObject("Comfort"));
            ComfortSettingsPanel panel = panelGo.AddComponent<ComfortSettingsPanel>();
            HandTracker tracker = new HandTracker();
            tracker.Listen(new[] { panel });
            Assert.AreEqual(DominantHand.Right, tracker.Hand);
            panel.onStart.Invoke(new ComfortSettings { Hand = DominantHand.Left });
            Assert.AreEqual(DominantHand.Left, tracker.Hand);
            tracker.Dispose();
            panel.onStart.Invoke(new ComfortSettings { Hand = DominantHand.Right });
            Assert.AreEqual(DominantHand.Left, tracker.Hand, "stops listening once disposed");
        }
    }
}
```

- [ ] **Step 2: Compilar y comprobar que falla.**
Expected: `CS0246` por `TargetScanner`, `ClickWitness`, `QuietDetector` y `HandTracker`.

- [ ] **Step 3: Implementar**

`Assets/_Project/Systems/TutorialHelp/Runtime/Observation/TargetScanner.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// What the learner could select right now, without knowing any module: interactable
    /// controls on world-space canvases, and objects with a collider that receive clicks, all
    /// within view and ray reach. Objects are listed once a second; controls on every scan.
    /// </summary>
    public sealed class TargetScanner
    {
        public const float MaxAngleFromGaze = 35f;
        public const float MaxDistance = 8f; // AutoHand's HandCanvasPointer.raycastLength
        public const float ObjectRefreshSeconds = 1f;

        private readonly List<MonoBehaviour> _clickables = new List<MonoBehaviour>();
        private readonly HashSet<Transform> _seen = new HashSet<Transform>();
        private float _sinceRefresh = float.MaxValue;

        public void Scan(Pose viewer, float elapsed, Transform ignore, List<HelpTarget> results)
        {
            results.Clear();
            _seen.Clear();

            foreach (Selectable selectable in Selectable.allSelectablesArray)
            {
                if (!IsUsable(selectable) || IsUnder(selectable.transform, ignore) || !InView(viewer, selectable.transform.position))
                    continue;
                if (_seen.Add(selectable.transform))
                    results.Add(new HelpTarget(selectable.transform, InteractionKind.Interface, selectable.targetGraphic));
            }

            _sinceRefresh += elapsed;
            if (_sinceRefresh >= ObjectRefreshSeconds)
            {
                _sinceRefresh = 0f;
                RefreshClickables();
            }
            foreach (MonoBehaviour clickable in _clickables)
            {
                if (clickable == null || !clickable.isActiveAndEnabled || IsUnder(clickable.transform, ignore))
                    continue;
                Collider collider = clickable.GetComponentInChildren<Collider>(false);
                if (collider == null || !collider.enabled || !InView(viewer, collider.bounds.center))
                    continue;
                if (_seen.Add(clickable.transform))
                    results.Add(new HelpTarget(clickable.transform, InteractionKind.Object3D, null));
            }
        }

        public static bool InView(Pose viewer, Vector3 point)
        {
            Vector3 toPoint = point - viewer.position;
            float distance = toPoint.magnitude;
            if (distance < 0.05f || distance > MaxDistance)
                return false;
            return Vector3.Angle(viewer.rotation * Vector3.forward, toPoint) <= MaxAngleFromGaze;
        }

        public static bool IsUsable(Selectable selectable)
        {
            if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsInteractable())
                return false;
            Canvas canvas = selectable.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled || canvas.rootCanvas.renderMode != RenderMode.WorldSpace)
                return false;
            return GroupAlpha(selectable.transform) > 0.01f;
        }

        private static float GroupAlpha(Transform transform)
        {
            float alpha = 1f;
            for (Transform t = transform; t != null; t = t.parent)
            {
                CanvasGroup group = t.GetComponent<CanvasGroup>();
                if (group == null || !group.enabled)
                    continue;
                alpha *= group.alpha;
                if (group.ignoreParentGroups)
                    break;
            }
            return alpha;
        }

        private static bool IsUnder(Transform transform, Transform root) => root != null && transform.IsChildOf(root);

        private void RefreshClickables()
        {
            _clickables.Clear();
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (behaviour is IPointerClickHandler && !(behaviour is Selectable) && !(behaviour is ClickWitness) &&
                    behaviour.GetComponentInParent<Canvas>() == null)
                    _clickables.Add(behaviour);
            }
        }
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Observation/ClickWitness.cs`:
```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RideSafe.Help
{
    /// <summary>
    /// Added at runtime next to a target's own click handler, so it receives the same click
    /// (uGUI calls every IPointerClickHandler on the object) from AutoHand, the mouse or a
    /// test. Never saved, never shown, never changes what the module does with the click.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class ClickWitness : MonoBehaviour, IPointerClickHandler
    {
        public InteractionKind Kind { get; private set; }
        public event Action<ClickWitness> Clicked;

        public static ClickWitness Attach(GameObject target, InteractionKind kind)
        {
            ClickWitness witness = target.GetComponent<ClickWitness>();
            if (witness == null)
            {
                witness = target.AddComponent<ClickWitness>();
                witness.hideFlags = HideFlags.DontSave | HideFlags.HideInInspector;
            }
            witness.Kind = kind;
            return witness;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Observation/QuietDetector.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace RideSafe.Help
{
    /// <summary>
    /// A video playing or a voice speaking: the learner is listening, not stuck. Narration is
    /// any playing, non-looping sound longer than <see cref="NarrationMinSeconds"/>; ambient
    /// loops and short clicks do not count. Sources are listed once a second.
    /// </summary>
    public sealed class QuietDetector
    {
        public const float NarrationMinSeconds = 2f;
        private const float RefreshSeconds = 1f;

        private readonly List<VideoPlayer> _videos = new List<VideoPlayer>();
        private readonly List<AudioSource> _sounds = new List<AudioSource>();
        private float _sinceRefresh = float.MaxValue;

        public bool IsQuiet(float deltaTime)
        {
            _sinceRefresh += deltaTime;
            if (_sinceRefresh >= RefreshSeconds)
            {
                _sinceRefresh = 0f;
                _videos.Clear();
                _videos.AddRange(Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
                _sounds.Clear();
                _sounds.AddRange(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            }
            foreach (VideoPlayer video in _videos)
            {
                if (video != null && video.isActiveAndEnabled && video.isPlaying)
                    return true;
            }
            foreach (AudioSource sound in _sounds)
            {
                if (sound != null && sound.isActiveAndEnabled &&
                    IsNarration(sound.isPlaying, sound.loop, sound.clip != null ? sound.clip.length : 0f))
                    return true;
            }
            return false;
        }

        public static bool IsNarration(bool playing, bool loop, float clipSeconds) =>
            playing && !loop && clipSeconds > NarrationMinSeconds;
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Observation/HandTracker.cs`:
```csharp
using System;
using System.Collections.Generic;
using RideSafe.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// The dominant hand chosen on the comfort screen (shared UI, no module code), heard from
    /// <see cref="ComfortSettingsPanel.onStart"/>. Right until then.
    /// </summary>
    public sealed class HandTracker : IDisposable
    {
        private readonly List<ComfortSettingsPanel> _panels = new List<ComfortSettingsPanel>();

        public DominantHand Hand { get; private set; } = DominantHand.Right;

        public void Listen(IEnumerable<ComfortSettingsPanel> panels)
        {
            foreach (ComfortSettingsPanel panel in panels)
            {
                if (panel == null || _panels.Contains(panel))
                    continue;
                panel.onStart.AddListener(OnStart);
                _panels.Add(panel);
            }
        }

        public void Dispose()
        {
            foreach (ComfortSettingsPanel panel in _panels)
            {
                if (panel != null)
                    panel.onStart.RemoveListener(OnStart);
            }
            _panels.Clear();
        }

        private void OnStart(ComfortSettings settings) => Hand = settings.Hand;
    }
}
```

- [ ] **Step 4: Correr los tests.** Expected: `passed=35 failed=0`.

- [ ] **Step 5: Punto de control (sin commit).**

---

### Task 4: Resaltado (titileo 2D y halo 3D)

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Art/TutorialHalo.shader`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Presentation/TargetHighlighter.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/TargetHighlighterTests.cs`

**Interfaces:**
- Consumes: `ITargetHighlighter`, `HelpTarget` (Task 2).
- Produces: `TargetHighlighter : MonoBehaviour, ITargetHighlighter` con `HaloName = "TutorialHalo"`, `int HighlightedCount`, `void Tick(float)`, `internal void Configure(Material halo)`; el shader `"RideSafe/Tutorial Halo"` con `_OutlineColor` y `_OutlineWidth`.

- [ ] **Step 1: Escribir los tests que fallan**

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/TargetHighlighterTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.Help.Tests
{
    public class TargetHighlighterTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private TargetHighlighter _highlighter;

        [SetUp]
        public void SetUp()
        {
            GameObject go = new GameObject("Highlighter");
            _objects.Add(go);
            _highlighter = go.AddComponent<TargetHighlighter>();
            Material halo = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _objects.Add(halo);
            _highlighter.Configure(halo);
        }

        [TearDown]
        public void TearDown()
        {
            _highlighter.Clear();
            foreach (Object o in _objects)
                Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private Image CanvasImage(Color rendererColor)
        {
            GameObject canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _objects.Add(canvas);
            GameObject go = new GameObject("Card", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            Image image = go.GetComponent<Image>();
            image.canvasRenderer.SetColor(rendererColor);
            return image;
        }

        private static HelpTarget Ui(Image image) => new HelpTarget(image.transform, InteractionKind.Interface, image);

        [Test]
        public void Pulse_DimsTheGraphic_AndRestoresItExactly()
        {
            Color original = new Color(1f, 0.9f, 0.8f, 1f);
            Image image = CanvasImage(original);
            _highlighter.Highlight(new[] { Ui(image) });
            _highlighter.Tick(0.8f); // half of the 1.6 s cycle: darkest
            Color dimmed = image.canvasRenderer.GetColor();
            Assert.AreEqual(original.r * 0.85f, dimmed.r, 0.01f);
            Assert.AreEqual(original.a, dimmed.a, 1e-4f, "never fades the control out");

            _highlighter.Tick(0.3f); // cut mid-pulse
            _highlighter.Clear();
            Assert.AreEqual(original, image.canvasRenderer.GetColor());
        }

        [Test]
        public void SeveralGraphics_PulseInStep()
        {
            Image a = CanvasImage(Color.white), b = CanvasImage(Color.white);
            _highlighter.Highlight(new[] { Ui(a), Ui(b) });
            _highlighter.Tick(0.5f);
            Assert.AreEqual(a.canvasRenderer.GetColor(), b.canvasRenderer.GetColor());
            Assert.AreEqual(2, _highlighter.HighlightedCount);
        }

        [Test]
        public void Highlight_RestoresTargetsNoLongerWanted()
        {
            Image a = CanvasImage(Color.white), b = CanvasImage(Color.white);
            _highlighter.Highlight(new[] { Ui(a), Ui(b) });
            _highlighter.Tick(0.8f);
            _highlighter.Highlight(new[] { Ui(b) });
            Assert.AreEqual(Color.white, a.canvasRenderer.GetColor());
            Assert.AreEqual(1, _highlighter.HighlightedCount);
        }

        [Test]
        public void Halo_AddsAHullUnderEachMesh_AndRemovesIt()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(cube);
            _highlighter.Highlight(new[] { new HelpTarget(cube.transform, InteractionKind.Object3D, null) });
            Transform halo = cube.transform.Find(TargetHighlighter.HaloName);
            Assert.IsNotNull(halo);
            Assert.AreSame(cube.GetComponent<MeshFilter>().sharedMesh, halo.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsTrue((halo.gameObject.hideFlags & HideFlags.DontSave) != 0);

            _highlighter.Highlight(new[] { new HelpTarget(cube.transform, InteractionKind.Object3D, null) });
            Assert.AreEqual(1, cube.GetComponentsInChildren<MeshFilter>().Length - 1, "highlighting twice adds no second hull");

            _highlighter.Clear();
            Assert.IsNull(cube.transform.Find(TargetHighlighter.HaloName));
        }
    }
}
```

- [ ] **Step 2: Compilar y comprobar que falla.** Expected: `CS0246` por `TargetHighlighter`.

- [ ] **Step 3: Implementar**

`Assets/_Project/Systems/TutorialHelp/Art/TutorialHalo.shader`. Es una copia propia del casco invertido de `Module01/Art/InspectionOutline.shader`: el tutorial no depende del arte de un módulo.
```hlsl
// Halo of the tutorial's 3D target, by inverted hull: back faces pushed out along the normal,
// so what shows around the silhouette is the line. Own copy of the module 1 technique: the
// tutorial must not depend on a module's art.
//
// SRP Batcher: properties stay OUTSIDE UnityPerMaterial on purpose so per-renderer
// MaterialPropertyBlock colours (the pulse) reach the shader.
Shader "RideSafe/Tutorial Halo"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (0.663, 0.886, 0.831, 0.9)
        _OutlineWidth ("Width in metres", Range(0.001, 0.08)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "TutorialHalo"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.positionCS = TransformWorldToHClip(positionWS + normalWS * _OutlineWidth);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb, _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Presentation/TargetHighlighter.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// Canvas controls pulse softly (white, slightly darker, white) on their CanvasRenderer,
    /// so the module's own colour and sprite states are untouched and restored exactly. Objects
    /// get a mint inverted-hull halo whose opacity pulses in step. All targets pulse together.
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Target Highlighter")]
    public sealed class TargetHighlighter : MonoBehaviour, ITargetHighlighter
    {
        public const string HaloName = "TutorialHalo";
        private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");

        [SerializeField] private Material _haloMaterial;
        [SerializeField] private Color _haloColor = new Color32(0xA9, 0xE2, 0xD4, 0xFF);
        [Tooltip("Darkest tone of the pulse on canvas controls (1 = no change).")]
        [SerializeField, Range(0.5f, 1f)] private float _dimTone = 0.85f;
        [SerializeField, Min(0.2f)] private float _periodSeconds = 1.6f;

        private readonly Dictionary<Transform, Graphic> _pulsing = new Dictionary<Transform, Graphic>();
        private readonly Dictionary<Graphic, Color> _original = new Dictionary<Graphic, Color>();
        private readonly Dictionary<Transform, List<MeshRenderer>> _halos = new Dictionary<Transform, List<MeshRenderer>>();
        private readonly HashSet<Transform> _wanted = new HashSet<Transform>();
        private readonly List<Transform> _drop = new List<Transform>();
        private MaterialPropertyBlock _block;
        private float _time;

        public int HighlightedCount => _pulsing.Count + _halos.Count;

        internal void Configure(Material halo) => _haloMaterial = halo;

        public void Highlight(IReadOnlyList<HelpTarget> targets)
        {
            if (HighlightedCount == 0)
                _time = 0f; // a new highlight starts from the control's own colour

            _wanted.Clear();
            foreach (HelpTarget target in targets)
            {
                if (target.Transform == null)
                    continue;
                _wanted.Add(target.Transform);
                if (target.Kind == InteractionKind.Interface)
                {
                    if (target.Graphic != null && !_pulsing.ContainsKey(target.Transform))
                    {
                        _pulsing[target.Transform] = target.Graphic;
                        _original[target.Graphic] = target.Graphic.canvasRenderer.GetColor();
                    }
                }
                else if (!_halos.ContainsKey(target.Transform))
                {
                    _halos[target.Transform] = CreateHalos(target.Transform);
                }
            }

            _drop.Clear();
            foreach (Transform t in _pulsing.Keys)
                if (!_wanted.Contains(t))
                    _drop.Add(t);
            foreach (Transform t in _halos.Keys)
                if (!_wanted.Contains(t))
                    _drop.Add(t);
            foreach (Transform t in _drop)
                Release(t);
        }

        public void Clear()
        {
            _drop.Clear();
            _drop.AddRange(_pulsing.Keys);
            _drop.AddRange(_halos.Keys);
            foreach (Transform t in _drop)
                Release(t);
        }

        public void Tick(float deltaTime)
        {
            if (HighlightedCount == 0)
                return;
            _time += deltaTime;
            float k = 0.5f - 0.5f * Mathf.Cos(_time / _periodSeconds * Mathf.PI * 2f);
            float tone = Mathf.Lerp(1f, _dimTone, k);
            Color dim = new Color(tone, tone, tone, 1f);
            foreach (KeyValuePair<Transform, Graphic> pair in _pulsing)
            {
                if (pair.Value != null)
                    pair.Value.canvasRenderer.SetColor(_original[pair.Value] * dim);
            }

            if (_block == null)
                _block = new MaterialPropertyBlock(); // not allowed in a field initializer
            Color halo = _haloColor;
            halo.a = Mathf.Lerp(0.9f, 0.45f, k);
            foreach (List<MeshRenderer> renderers in _halos.Values)
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    if (renderer == null)
                        continue;
                    _block.Clear();
                    _block.SetColor(ColorId, halo);
                    renderer.SetPropertyBlock(_block);
                }
            }
        }

        private void Update() => Tick(Time.deltaTime);
        private void OnDisable() => Clear();

        private void Release(Transform target)
        {
            if (_pulsing.TryGetValue(target, out Graphic graphic))
            {
                if (graphic != null && _original.TryGetValue(graphic, out Color original))
                    graphic.canvasRenderer.SetColor(original);
                if (graphic != null)
                    _original.Remove(graphic);
                _pulsing.Remove(target);
            }
            if (_halos.TryGetValue(target, out List<MeshRenderer> renderers))
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    if (renderer != null)
                        Discard(renderer.gameObject);
                }
                _halos.Remove(target);
            }
        }

        private List<MeshRenderer> CreateHalos(Transform target)
        {
            List<MeshRenderer> halos = new List<MeshRenderer>();
            if (_haloMaterial == null)
                return halos;
            foreach (MeshFilter filter in target.GetComponentsInChildren<MeshFilter>(false))
            {
                MeshRenderer source = filter.GetComponent<MeshRenderer>();
                if (filter.name == HaloName || filter.sharedMesh == null || source == null || !source.enabled)
                    continue;
                GameObject go = new GameObject(HaloName, typeof(MeshFilter), typeof(MeshRenderer))
                {
                    hideFlags = HideFlags.DontSave
                };
                go.transform.SetParent(filter.transform, false);
                go.layer = filter.gameObject.layer;
                go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = _haloMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                halos.Add(renderer);
            }
            return halos;
        }

        private static void Discard(Object o)
        {
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }
    }
}
```

- [ ] **Step 4: Correr los tests.** Expected: `passed=39 failed=0`.

- [ ] **Step 5: Punto de control (sin commit).**

---

### Task 5: Control Touch Plus (`GestureHintView`)

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Presentation/GestureHintView.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/GestureHintViewTests.cs`

**Interfaces:**
- Consumes: `IGestureHintView`, `TutorialGestureSO` (Task 2); `ControllerModelVisual.TryGetButton`, `ControllerModelVisual.PoseFor`, `ControllerButtonVisual.Begin/Apply/End/PressSeconds` (en `RideSafe.Tutorial.Presentation`, ya existentes).
- Produces: `GestureHintView : MonoBehaviour, IGestureHintView` con `string CaptionText`, `XRNodeRole ShownHand`, `void Place(Pose viewer)`, `void Tick(float)`, `static float PressAt(float time, float lead, float press, float pause)`, `internal void Configure(ControllerModelVisual left, ControllerModelVisual right, TMP_Text caption)`.

- [ ] **Step 1: Escribir los tests que fallan**

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/GestureHintViewTests.cs`:
```csharp
using System.Text.RegularExpressions;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.Help.Tests
{
    public class GestureHintViewTests
    {
        private GameObject _root;
        private GestureHintView _view;
        private ControllerModelVisual _left, _right;
        private TMP_Text _caption;
        private TutorialGestureSO _trigger;

        private static ControllerModelVisual Model(Transform parent, XRNodeRole hand)
        {
            GameObject go = new GameObject(hand.ToString());
            go.transform.SetParent(parent, false);
            ControllerModelVisual model = go.AddComponent<ControllerModelVisual>();
            SerializedObject serialized = new SerializedObject(model);
            serialized.FindProperty("_hand").enumValueIndex = (int)hand;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject trigger = new GameObject("Trigger");
            trigger.transform.SetParent(go.transform, false);
            trigger.AddComponent<ControllerButtonVisual>(); // Trigger by default
            return model;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Gesture Hint");
            _view = _root.AddComponent<GestureHintView>();
            _left = Model(_root.transform, XRNodeRole.LeftHand);
            _right = Model(_root.transform, XRNodeRole.RightHand);
            GameObject caption = new GameObject("Caption", typeof(TextMeshPro));
            caption.transform.SetParent(_root.transform, false);
            _caption = caption.GetComponent<TMP_Text>();
            _view.Configure(_left, _right, _caption);
            _trigger = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _trigger.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            _view.Hide();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_trigger);
        }

        [Test]
        public void Show_UsesTheRequestedHand_AndHidesTheOther()
        {
            _view.Show(_trigger, XRNodeRole.LeftHand, null, null);
            Assert.IsTrue(_view.IsVisible);
            Assert.IsTrue(_left.gameObject.activeSelf);
            Assert.IsFalse(_right.gameObject.activeSelf);
            Assert.AreEqual(XRNodeRole.LeftHand, _view.ShownHand);
        }

        [Test]
        public void Caption_OnlyWhenThereIsText()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, "Apunta y presiona el gatillo", null);
            Assert.IsTrue(_caption.gameObject.activeSelf);
            Assert.AreEqual("Apunta y presiona el gatillo", _view.CaptionText);
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            Assert.IsFalse(_caption.gameObject.activeSelf);
        }

        [Test]
        public void Hide_DeactivatesTheView()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            _view.Hide();
            Assert.IsFalse(_view.IsVisible);
            Assert.IsFalse(_root.activeSelf);
        }

        [Test]
        public void Show_WithAMissingButton_HidesAndWarns()
        {
            TutorialGestureSO grip = ScriptableObject.CreateInstance<TutorialGestureSO>();
            grip.Configure(ControllerControl.Grip, TutorialGestureSO.HandChoice.Dominant, "", "");
            LogAssert.Expect(LogType.Warning, new Regex("Grip"));
            _view.Show(grip, XRNodeRole.RightHand, null, null);
            Assert.IsFalse(_view.IsVisible);
            Object.DestroyImmediate(grip);
        }

        [Test]
        public void Place_PutsTheControllerBelowAndBesideTheGaze_FacingTheLearner()
        {
            _view.Show(_trigger, XRNodeRole.RightHand, null, null);
            _view.Place(new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity));
            Vector3 p = _root.transform.position;
            Assert.Less(p.y, 1.6f, "below eye level");
            Assert.Greater(p.z, 0.3f, "in front");
            Assert.Greater(Vector3.Dot(_root.transform.forward, new Vector3(0f, 1.6f, 0f) - p), 0f, "faces the learner");
        }

        [Test]
        public void PressAt_HighlightsThenPressesThenRests()
        {
            Assert.AreEqual(0f, GestureHintView.PressAt(0.2f, 0.4f, 0.25f, 0.6f));
            Assert.AreEqual(1f, GestureHintView.PressAt(0.65f, 0.4f, 0.25f, 0.6f), 1e-3f);
            Assert.AreEqual(0f, GestureHintView.PressAt(1.2f, 0.4f, 0.25f, 0.6f));
        }
    }
}
```

- [ ] **Step 2: Compilar y comprobar que falla.** Expected: `CS0246` por `GestureHintView`.

- [ ] **Step 3: Implementar**

`Assets/_Project/Systems/TutorialHelp/Runtime/Presentation/GestureHintView.cs`:
```csharp
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// The Touch Plus of the hand to use, turned to show the gesture's control, which glows and
    /// is pressed a few times, then stays lit. Sits below and beside the gaze, world-locked
    /// with a dead zone so it does not ride on small head movements. Optional short text.
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Gesture Hint View")]
    public sealed class GestureHintView : MonoBehaviour, IGestureHintView
    {
        [SerializeField] private ControllerModelVisual _left;
        [SerializeField] private ControllerModelVisual _right;
        [SerializeField] private TMP_Text _caption;

        [Header("Placement (head-relative)")]
        [SerializeField, Min(0.2f)] private float _distance = 0.6f;
        [SerializeField] private Vector2 _offset = new Vector2(0.2f, -0.27f);
        [SerializeField, Range(0f, 45f)] private float _maxYawFromGaze = 15f;
        [SerializeField, Min(0.1f)] private float _scale = 2.2f;
        [SerializeField, Range(0f, 90f)] private float _refollowAngle = 25f;
        [SerializeField, Min(0f)] private float _refollowDistance = 0.3f;
        [SerializeField, Min(0.01f)] private float _followSeconds = 0.5f;

        [Header("Animation")]
        [SerializeField] private Color _glowColor = new Color32(0xA9, 0xE2, 0xD4, 0xFF);
        [SerializeField, Min(0f)] private float _leadSeconds = 0.4f;
        [SerializeField, Min(0f)] private float _pauseSeconds = 0.6f;
        [SerializeField, Min(0.1f)] private float _pulseSeconds = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _minGlow = 0.45f;
        [SerializeField, Min(0)] private int _maxDemonstrations = 3;

        private ControllerButtonVisual _button;
        private TutorialGestureSO _gesture;
        private string _text;
        private Vector3? _focus;
        private bool _placed;
        private Pose _placedViewer, _followFrom, _followTo;
        private float _followTime = float.MaxValue;
        private float _time;
        private MaterialPropertyBlock _block;

        public bool IsVisible => gameObject.activeSelf && _button != null;
        public XRNodeRole ShownHand { get; private set; } = XRNodeRole.RightHand;
        public string CaptionText => _caption != null && _caption.gameObject.activeSelf ? _caption.text : null;

        internal void Configure(ControllerModelVisual left, ControllerModelVisual right, TMP_Text caption)
        {
            _left = left;
            _right = right;
            _caption = caption;
        }

        public void Show(TutorialGestureSO gesture, XRNodeRole hand, string text, Vector3? focus)
        {
            _focus = focus;
            if (_button != null && gesture == _gesture && hand == ShownHand && text == _text)
                return;

            ControllerModelVisual model = hand == XRNodeRole.LeftHand ? _left : _right;
            if (gesture == null || model == null || !model.TryGetButton(gesture.Control, out ControllerButtonVisual button))
            {
                Debug.LogWarning("[TutorialHelp] The " + hand + " controller has no " +
                                 (gesture != null ? gesture.Control.ToString() : "gesture") + " to show.", this);
                Hide();
                return;
            }

            EndButton();
            if (_left != null) _left.gameObject.SetActive(model == _left);
            if (_right != null) _right.gameObject.SetActive(model == _right);
            Pose pose = model.PoseFor(gesture.Control);
            model.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);

            _button = button;
            _button.Begin();
            _gesture = gesture;
            ShownHand = hand;
            _text = text;
            if (_caption != null)
            {
                _caption.text = text ?? string.Empty;
                _caption.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
            _time = 0f;
            if (!gameObject.activeSelf)
            {
                _placed = false;
                gameObject.SetActive(true);
            }
            Tick(0f);
        }

        public void Hide()
        {
            EndButton();
            _gesture = null;
            _text = null;
            _placed = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            Camera camera = Camera.main;
            if (camera != null)
                Place(new Pose(camera.transform.position, camera.transform.rotation));
            Tick(Time.deltaTime);
        }

        private void OnDisable() => EndButton();

        private void EndButton()
        {
            if (_button != null)
                _button.End();
            _button = null;
        }

        public void Place(Pose viewer)
        {
            if (_placed)
            {
                float turned = Vector3.Angle(Flat(_placedViewer.rotation * Vector3.forward), Flat(viewer.rotation * Vector3.forward));
                float moved = Vector3.Distance(_placedViewer.position, viewer.position);
                if (turned < _refollowAngle && moved < _refollowDistance)
                    return;
                _followFrom = new Pose(transform.position, transform.rotation);
                _followTo = Compute(viewer);
                _followTime = 0f;
                _placedViewer = viewer;
                return;
            }
            Pose pose = Compute(viewer);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            transform.localScale = Vector3.one * _scale;
            _placedViewer = viewer;
            _followTime = float.MaxValue;
            _placed = true;
        }

        private Pose Compute(Pose viewer)
        {
            Vector3 gaze = Flat(viewer.rotation * Vector3.forward);
            Vector3 focus = _focus ?? viewer.position + gaze;
            Vector3 toFocus = Vector3.ProjectOnPlane(focus - viewer.position, Vector3.up);
            float yaw = toFocus.sqrMagnitude > 1e-6f ? Vector3.SignedAngle(gaze, toFocus, Vector3.up) : 0f;
            Vector3 direction = Quaternion.AngleAxis(Mathf.Clamp(yaw, -_maxYawFromGaze, _maxYawFromGaze), Vector3.up) * gaze;
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            float side = Mathf.Abs(yaw) > 2f ? -Mathf.Sign(yaw) : 1f; // toward the view's centre
            Vector3 position = viewer.position + direction * _distance + right * (_offset.x * side) + Vector3.up * _offset.y;
            Vector3 toViewer = viewer.position - position;
            return new Pose(position, Quaternion.LookRotation(toViewer.sqrMagnitude > 1e-6f ? toViewer : -direction, Vector3.up));
        }

        private static Vector3 Flat(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
        }

        public void Tick(float deltaTime)
        {
            if (_button == null)
                return;
            if (_followTime < _followSeconds)
            {
                _followTime += deltaTime;
                float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_followTime / _followSeconds));
                transform.SetPositionAndRotation(Vector3.Lerp(_followFrom.position, _followTo.position, f),
                                                 Quaternion.Slerp(_followFrom.rotation, _followTo.rotation, f));
            }
            if (_block == null)
                _block = new MaterialPropertyBlock(); // not allowed in a field initializer

            _time += deltaTime;
            float cycle = _leadSeconds + 2f * _button.PressSeconds + _pauseSeconds;
            int played = cycle > 0f ? Mathf.FloorToInt(_time / cycle) : 0;
            float press = played < _maxDemonstrations ? PressAt(_time, _leadSeconds, _button.PressSeconds, _pauseSeconds) : 0f;
            float glow = Mathf.Lerp(_minGlow, 1f, 0.5f + 0.5f * Mathf.Sin(_time / _pulseSeconds * Mathf.PI * 2f));
            _button.Apply(press, glow, _glowColor, _block);
        }

        /// <summary>One demonstration: glow alone for lead, ease down over press, back up over press, rest for pause.</summary>
        public static float PressAt(float time, float lead, float press, float pause)
        {
            float cycle = lead + 2f * press + pause;
            float t = cycle > 0f ? time % cycle : 0f;
            if (t < lead)
                return 0f;
            t -= lead;
            if (t < press)
                return Mathf.SmoothStep(0f, 1f, t / press);
            t -= press;
            return t < press ? Mathf.SmoothStep(1f, 0f, t / press) : 0f;
        }
    }
}
```

- [ ] **Step 4: Correr los tests.** Expected: `passed=45 failed=0`.

- [ ] **Step 5: Punto de control (sin commit).**

---

### Task 6: Gestor de escena y componentes sin código

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/TutorialHelpManager.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Api/TutorialCue.cs`
- Create: `Assets/_Project/Systems/TutorialHelp/Runtime/Api/TutorialPause.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/CueAndPauseTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 1–5.
- Produces:
  - `TutorialHelpManager : MonoBehaviour, ITutorialHelpService`, con `HelpDirector Director`, `IReadOnlyList<HelpTarget> Visible`, `GestureHintView HintView` y `TargetHighlighter Highlighter`, y los campos serializados `_selectGesture`, `_hintView`, `_highlighter` y `_scanSeconds`.
  - `TutorialCue`: campos `_gesture` y `_targets`, `internal void Configure(TutorialGestureSO, params Transform[])`, `internal void Begin()` y `internal void End()`.
  - `TutorialPause`: `internal void Begin()` y `internal void End()`.

- [ ] **Step 1: Escribir los tests que fallan**

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/CueAndPauseTests.cs`:
```csharp
using NUnit.Framework;
using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help.Tests
{
    public class CueAndPauseTests
    {
        private TutorialGestureSO _gesture;
        private FakeHintView _view;
        private HelpDirector _director;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
            _gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant, "", "");
            _view = new FakeHintView();
            _director = new HelpDirector(_gesture, _view, new FakeHighlighter(), key => key);
            TutorialHelp.Register(_director);
            _go = new GameObject("Section");
        }

        [TearDown]
        public void TearDown()
        {
            TutorialHelp.Unregister(_director);
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_gesture);
        }

        [Test]
        public void Cue_ShowsItsGesture_WhileActive()
        {
            TutorialCue cue = _go.AddComponent<TutorialCue>();
            cue.Configure(_gesture, _go.transform);
            cue.Begin();
            Assert.IsTrue(_view.Visible);
            cue.End();
            Assert.IsFalse(_view.Visible);
        }

        [Test]
        public void Pause_StopsObserving_WhileActive()
        {
            TutorialPause pause = _go.AddComponent<TutorialPause>();
            pause.Begin();
            Assert.IsFalse(TutorialHelp.IsObserving);
            pause.End();
            Assert.IsTrue(TutorialHelp.IsObserving);
        }
    }
}
```

- [ ] **Step 2: Compilar y comprobar que falla.** Expected: `CS0246` por `TutorialCue` y `TutorialPause`.

- [ ] **Step 3: Implementar**

`Assets/_Project/Systems/TutorialHelp/Runtime/Api/TutorialCue.cs`:
```csharp
using System;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>No-code explicit help: shows the gesture on the targets while this object is active.</summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Tutorial Cue")]
    public sealed class TutorialCue : MonoBehaviour
    {
        [SerializeField] private TutorialGestureSO _gesture;
        [SerializeField] private Transform[] _targets = Array.Empty<Transform>();

        private IDisposable _help;

        internal void Configure(TutorialGestureSO gesture, params Transform[] targets)
        {
            _gesture = gesture;
            _targets = targets;
        }

        private void OnEnable() => Begin();
        private void OnDisable() => End();

        internal void Begin()
        {
            End();
            _help = TutorialHelp.Show(_gesture, _targets);
        }

        internal void End()
        {
            _help?.Dispose();
            _help = null;
        }
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/Api/TutorialPause.cs`:
```csharp
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>No-code Stop/Resume: the tutorial does not observe while this object is active.</summary>
    [AddComponentMenu("RideSafe/Tutorial Help/Tutorial Pause")]
    public sealed class TutorialPause : MonoBehaviour
    {
        private void OnEnable() => Begin();
        private void OnDisable() => End();

        internal void Begin() => TutorialHelp.Stop();
        internal void End() => TutorialHelp.Resume();
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Runtime/TutorialHelpManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using Cachacos;
using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// The scene's tutorial. Observes without touching any module: scans what can be selected,
    /// hears clicks on those targets, pauses for video and narration, follows the dominant hand,
    /// and lets <see cref="HelpDirector"/> decide what <see cref="GestureHintView"/> and
    /// <see cref="TargetHighlighter"/> show. Registers itself for <see cref="TutorialHelp"/>.
    /// Runs early so a module's OnEnable can already call it.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("RideSafe/Tutorial Help/Tutorial Help Manager")]
    public sealed class TutorialHelpManager : MonoBehaviour, ITutorialHelpService
    {
        [SerializeField] private TutorialGestureSO _selectGesture;
        [SerializeField] private GestureHintView _hintView;
        [SerializeField] private TargetHighlighter _highlighter;
        [SerializeField, Min(0.05f)] private float _scanSeconds = 0.2f;

        private readonly TargetScanner _scanner = new TargetScanner();
        private readonly QuietDetector _quiet = new QuietDetector();
        private readonly HandTracker _hand = new HandTracker();
        private readonly List<HelpTarget> _visible = new List<HelpTarget>();
        private readonly List<ClickWitness> _witnesses = new List<ClickWitness>();
        private HelpDirector _director;
        private ILocalizationProvider _localization;
        private float _sinceScan = float.MaxValue;
        private float _sinceHands = float.MaxValue;

        public HelpDirector Director => _director;
        public IReadOnlyList<HelpTarget> Visible => _visible;
        public GestureHintView HintView => _hintView;
        public TargetHighlighter Highlighter => _highlighter;
        public bool Observing => _director != null && _director.Observing;

        private void Awake()
        {
            _director = new HelpDirector(_selectGesture, _hintView, _highlighter, Translate);
            if (_hintView != null)
                _hintView.Hide();
            TutorialHelp.Register(this);
        }

        private void OnDestroy()
        {
            TutorialHelp.Unregister(this);
            _hand.Dispose();
            foreach (ClickWitness witness in _witnesses)
            {
                if (witness == null)
                    continue;
                witness.Clicked -= OnClicked;
                Destroy(witness);
            }
            _witnesses.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            _sinceHands += deltaTime;
            if (_sinceHands >= TargetScanner.ObjectRefreshSeconds)
            {
                _sinceHands = 0f;
                _hand.Listen(FindObjectsByType<ComfortSettingsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }
            _director.Hand = _hand.Hand;

            if (_director.Observing)
            {
                _sinceScan += deltaTime;
                Camera camera = Camera.main;
                if (_sinceScan >= _scanSeconds && camera != null)
                {
                    _scanner.Scan(new Pose(camera.transform.position, camera.transform.rotation), _sinceScan, transform, _visible);
                    _sinceScan = 0f;
                    Witness(_visible);
                }
            }
            else
            {
                _visible.Clear();
                _sinceScan = float.MaxValue;
            }

            _director.Update(deltaTime, _visible, _director.Observing && _quiet.IsQuiet(deltaTime));
        }

        public IDisposable Show(TutorialGestureSO gesture, IReadOnlyList<Transform> targets) => _director.Show(gesture, targets);
        public void Stop() => _director.Stop();
        public void Resume() => _director.Resume();

        private void Witness(List<HelpTarget> targets)
        {
            foreach (HelpTarget target in targets)
            {
                if (target.Transform == null)
                    continue;
                ClickWitness witness = ClickWitness.Attach(target.Transform.gameObject, target.Kind);
                if (_witnesses.Contains(witness))
                    continue;
                witness.Clicked += OnClicked;
                _witnesses.Add(witness);
            }
        }

        private void OnClicked(ClickWitness witness) => _director.Clicked(witness.Kind);

        private string Translate(string key)
        {
            if (_localization == null)
                _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            return _localization?.GetTranslation(key);
        }
    }
}
```

- [ ] **Step 4: Correr los tests.** Expected: `passed=47 failed=0`.

- [ ] **Step 5: Punto de control (sin commit).**

---

### Task 7: Textos I2, montaje en la v5 y tests de contrato

**Files:**
- Create: `Assets/_Project/Localization/Editor/TutorialHelpTerms.cs` (sin asmdef, como `Module01DemoSetup`)
- Create: `Assets/_Project/Systems/TutorialHelp/Editor/TutorialHelp.Editor.asmdef`
- Create: `Assets/_Project/Systems/TutorialHelp/Editor/TutorialHelpSceneSetup.cs`
- Test: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/GarajeV5ContractTests.cs`
- Modify: `Assets/_Project/Systems/TutorialHelp/Tests/Editor/TutorialHelp.Tests.asmdef` (añadir la referencia `"TutorialHelp.Editor"`)
- Generated: `Assets/_Project/Systems/TutorialHelp/Data/SO_Gesture_Select.asset`, `Assets/_Project/Systems/TutorialHelp/Art/M_TutorialHalo.mat`, `Assets/Resources/I2Languages.asset` (2 términos) y `Assets/_Project/Scene/RideSafe_Garaje_v5.unity`.

**Interfaces:**
- Consumes: `TutorialHelpManager`, `GestureHintView.Configure`, `TargetHighlighter.Configure`, `TutorialGestureSO.Configure`, `HelpTextKeys` (Tasks 1–6); `TouchPlusPrefabBuilder.LeftPrefabPath` y `RightPrefabPath` (`RideSafe.Tutorial.Editor`).
- Produces: `TutorialHelpSceneSetup.Build()` (menú `RideSafe/Tutorial Help/Montar en RideSafe_Garaje_v5`) y las constantes `ScenePath`, `RootName = "TutorialHelp"`, `OldRootName = "Tutorial"`, `GesturePath` y `HaloMaterialPath`; además, `TutorialHelpTerms.Apply()`.

- [ ] **Step 1: Comprobar el namespace del montador de los Touch Plus**

Run: `grep -n "^namespace\|class TouchPlusPrefabBuilder" Assets/_Project/Systems/Tutorial/Editor/TouchPlusPrefabBuilder.cs`
Expected: `namespace RideSafe.Tutorial.Editor` y `public static class TouchPlusPrefabBuilder`. Si el namespace es otro, ajusta el `using` del Step 3.

- [ ] **Step 2: Escribir el test de contrato que falla**

Añade `"TutorialHelp.Editor"` a `references` en `TutorialHelp.Tests.asmdef`.

`Assets/_Project/Systems/TutorialHelp/Tests/Editor/GarajeV5ContractTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Presentation;
using RideSafe.Help.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Help.Tests
{
    /// <summary>RideSafe_Garaje_v5 has one tutorial, wired, and no module knows it exists.</summary>
    public class GarajeV5ContractTests
    {
        private Scene _scene;
        private bool _openedHere;

        [OneTimeSetUp]
        public void Open()
        {
            _scene = SceneManager.GetSceneByPath(TutorialHelpSceneSetup.ScenePath);
            _openedHere = !_scene.isLoaded;
            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(TutorialHelpSceneSetup.ScenePath, OpenSceneMode.Additive);
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (_openedHere)
                EditorSceneManager.CloseScene(_scene, true);
        }

        private GameObject Root(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }

        [Test]
        public void TheOldTutorialIsGone()
        {
            Assert.IsNull(Root(TutorialHelpSceneSetup.OldRootName));
        }

        [Test]
        public void ThereIsExactlyOneTutorialHelp_FullyWired()
        {
            List<TutorialHelpManager> managers = new List<TutorialHelpManager>();
            foreach (GameObject root in _scene.GetRootGameObjects())
                managers.AddRange(root.GetComponentsInChildren<TutorialHelpManager>(true));
            Assert.AreEqual(1, managers.Count);
            TutorialHelpManager manager = managers[0];
            Assert.AreEqual(TutorialHelpSceneSetup.RootName, manager.gameObject.name);
            Assert.IsTrue(manager.gameObject.activeSelf);

            SerializedObject serialized = new SerializedObject(manager);
            TutorialGestureSO gesture = (TutorialGestureSO)serialized.FindProperty("_selectGesture").objectReferenceValue;
            Assert.IsNotNull(gesture);
            Assert.AreEqual(ControllerControl.Trigger, gesture.Control);
            Assert.AreEqual(TutorialGestureSO.HandChoice.Dominant, gesture.Hand);
            Assert.AreEqual(HelpTextKeys.SelectInterface, gesture.TextKeyFor(InteractionKind.Interface));
            Assert.IsNotNull(serialized.FindProperty("_hintView").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("_highlighter").objectReferenceValue);
        }

        [Test]
        public void TheHint_HasBothTouchPlus_WithATrigger()
        {
            GestureHintView view = Root(TutorialHelpSceneSetup.RootName).GetComponentInChildren<GestureHintView>(true);
            SerializedObject serialized = new SerializedObject(view);
            foreach (string field in new[] { "_left", "_right" })
            {
                ControllerModelVisual model = (ControllerModelVisual)serialized.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(model, field);
                Assert.IsTrue(model.TryGetButton(ControllerControl.Trigger, out _), field + " trigger");
            }
            Assert.IsNotNull(serialized.FindProperty("_caption").objectReferenceValue);
        }

        [Test]
        public void TheHalo_UsesTheTutorialsOwnShader()
        {
            TargetHighlighter highlighter = Root(TutorialHelpSceneSetup.RootName).GetComponent<TargetHighlighter>();
            Material material = (Material)new SerializedObject(highlighter).FindProperty("_haloMaterial").objectReferenceValue;
            Assert.AreEqual("RideSafe/Tutorial Halo", material.shader.name);
        }

        [Test]
        public void NoModuleReferencesTheTutorial()
        {
            Transform tutorial = Root(TutorialHelpSceneSetup.RootName).transform;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.transform.IsChildOf(tutorial))
                        continue;
                    string ns = behaviour.GetType().Namespace ?? string.Empty;
                    Assert.IsFalse(ns.StartsWith("RideSafe.Help"), behaviour.name + " carries a tutorial component outside TutorialHelp");
                }
            }
        }
    }
}
```

- [ ] **Step 3: Implementar los textos y el montaje**

`Assets/_Project/Localization/Editor/TutorialHelpTerms.cs`:
```csharp
using I2.Loc;
using RideSafe.Help;
using UnityEditor;

/// <summary>The tutorial's two short first-time texts, in es-CO and en (same pattern as Module01DemoSetup).</summary>
public static class TutorialHelpTerms
{
    [MenuItem("RideSafe/Tutorial Help/Añadir textos (I2)")]
    public static void Apply()
    {
        LanguageSourceAsset asset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>("Assets/Resources/I2Languages.asset");
        LanguageSourceData data = asset.SourceData;
        int es = data.GetLanguageIndexFromCode("es-CO");
        int en = data.GetLanguageIndexFromCode("en");
        if (es < 0 || en < 0)
            throw new System.InvalidOperationException("I2Languages has no es-CO or en.");
        Set(data, es, en, HelpTextKeys.SelectInterface, "Apunta y presiona el gatillo", "Point and press the trigger");
        Set(data, es, en, HelpTextKeys.SelectObject, "Apunta al objeto y presiona el gatillo", "Point at the object and press the trigger");
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static void Set(LanguageSourceData data, int es, int en, string key, string spanish, string english)
    {
        TermData term = data.GetTermData(key) ?? data.AddTerm(key);
        term.Languages[es] = spanish;
        term.Languages[en] = english;
    }
}
```

`Assets/_Project/Systems/TutorialHelp/Editor/TutorialHelp.Editor.asmdef`:
```json
{
    "name": "TutorialHelp.Editor",
    "rootNamespace": "RideSafe.Help.Editor",
    "references": [
        "TutorialHelp",
        "Tutorial",
        "Tutorial.Presentation",
        "Tutorial.Editor",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_Project/Systems/TutorialHelp/Editor/TutorialHelpSceneSetup.cs`:
```csharp
using System.Collections.Generic;
using System.Text;
using RideSafe.Tutorial;
using RideSafe.Tutorial.Editor;
using RideSafe.Tutorial.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Help.Editor
{
    /// <summary>
    /// Puts the agnostic tutorial into RideSafe_Garaje_v5: removes the old, inactive "Tutorial"
    /// root (its own copy of the module 0 screens) once nothing outside it references it, and
    /// adds the "TutorialHelp" root. Touches no module object. Idempotent.
    /// </summary>
    public static class TutorialHelpSceneSetup
    {
        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        public const string RootName = "TutorialHelp";
        public const string OldRootName = "Tutorial";
        public const string GesturePath = "Assets/_Project/Systems/TutorialHelp/Data/SO_Gesture_Select.asset";
        public const string HaloMaterialPath = "Assets/_Project/Systems/TutorialHelp/Art/M_TutorialHalo.mat";
        private const string HaloShader = "RideSafe/Tutorial Halo";
        private const string AtlasA = "Assets/_Project/UI/RideSafe_UI_Atlas_A_4096.png";
        private const string CaptionFont = "Assets/_Project/UI/Fonts/Inter-Regular SDF.asset";
        private const string HintName = "Gesture Hint";

        [MenuItem("RideSafe/Tutorial Help/Montar en RideSafe_Garaje_v5")]
        public static void Build()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                StringBuilder report = new StringBuilder("[TutorialHelp] RideSafe_Garaje_v5:\n");
                RemoveOldTutorial(scene, report);
                TutorialGestureSO gesture = EnsureGesture(report);
                Material halo = EnsureHaloMaterial(report);
                BuildRoot(scene, gesture, halo, report);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new System.InvalidOperationException("Could not save " + ScenePath);
                Debug.Log(report.ToString());
            }
            finally
            {
                if (openedHere)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void RemoveOldTutorial(Scene scene, StringBuilder report)
        {
            GameObject old = FindRoot(scene, OldRootName);
            if (old == null)
            {
                report.AppendLine("  old Tutorial: already gone");
                return;
            }
            List<string> users = new List<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == old)
                    continue;
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        Object value = property.objectReferenceValue;
                        Transform target = value is GameObject go ? go.transform : value is Component c ? c.transform : null;
                        if (target != null && target.IsChildOf(old.transform))
                            users.Add(component.GetType().Name + " on " + component.name + "." + property.propertyPath);
                    }
                }
            }
            if (users.Count > 0)
                throw new System.InvalidOperationException("The old Tutorial is still referenced: " + string.Join(", ", users));
            Object.DestroyImmediate(old);
            report.AppendLine("  old Tutorial removed (nothing referenced it)");
        }

        private static TutorialGestureSO EnsureGesture(StringBuilder report)
        {
            TutorialGestureSO gesture = AssetDatabase.LoadAssetAtPath<TutorialGestureSO>(GesturePath);
            if (gesture == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GesturePath));
                gesture = ScriptableObject.CreateInstance<TutorialGestureSO>();
                AssetDatabase.CreateAsset(gesture, GesturePath);
            }
            gesture.Configure(ControllerControl.Trigger, TutorialGestureSO.HandChoice.Dominant,
                              HelpTextKeys.SelectInterface, HelpTextKeys.SelectObject);
            EditorUtility.SetDirty(gesture);
            AssetDatabase.SaveAssetIfDirty(gesture);
            report.AppendLine("  gesture: trigger, dominant hand");
            return gesture;
        }

        private static Material EnsureHaloMaterial(StringBuilder report)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(HaloMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find(HaloShader);
            if (shader == null)
                throw new System.InvalidOperationException("Shader " + HaloShader + " is not imported.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, HaloMaterialPath);
            report.AppendLine("  halo material created");
            return material;
        }

        private static void BuildRoot(Scene scene, TutorialGestureSO gesture, Material halo, StringBuilder report)
        {
            GameObject root = FindRoot(scene, RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            root.SetActive(true);

            TargetHighlighter highlighter = GetOrAdd<TargetHighlighter>(root);
            highlighter.Configure(halo);
            EditorUtility.SetDirty(highlighter);

            Transform hint = root.transform.Find(HintName);
            if (hint == null)
            {
                hint = new GameObject(HintName).transform;
                hint.SetParent(root.transform, false);
            }
            GestureHintView view = GetOrAdd<GestureHintView>(hint.gameObject);
            ControllerModelVisual left = EnsureModel(hint, TouchPlusPrefabBuilder.LeftPrefabPath);
            ControllerModelVisual right = EnsureModel(hint, TouchPlusPrefabBuilder.RightPrefabPath);
            EnsureBackdrop(hint);
            TMP_Text caption = EnsureCaption(hint);
            view.Configure(left, right, caption);
            EditorUtility.SetDirty(view);
            hint.gameObject.SetActive(false);

            TutorialHelpManager manager = GetOrAdd<TutorialHelpManager>(root);
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("_selectGesture").objectReferenceValue = gesture;
            serialized.FindProperty("_hintView").objectReferenceValue = view;
            serialized.FindProperty("_highlighter").objectReferenceValue = highlighter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine("  TutorialHelp: manager, highlighter, hint (" + left.name + ", " + right.name + ")");
        }

        private static ControllerModelVisual EnsureModel(Transform parent, string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new System.IO.FileNotFoundException("Touch Plus prefab missing", prefabPath);
            foreach (Transform child in parent)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab)
                    return child.GetComponent<ControllerModelVisual>();
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.SetActive(false);
            return instance.GetComponent<ControllerModelVisual>();
        }

        /// <summary>Dark rounded card (Atlas A Panel_Dark_Base, 9-slice) so the white controller reads against the white garage.</summary>
        private static void EnsureBackdrop(Transform hint)
        {
            Sprite panel = null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AtlasA))
            {
                if (asset is Sprite sprite && sprite.name == "Panel_Dark_Base")
                    panel = sprite;
            }
            if (panel == null)
                throw new System.InvalidOperationException("Atlas A has no Panel_Dark_Base.");
            Transform backdrop = hint.Find("Backdrop");
            if (backdrop == null)
            {
                backdrop = new GameObject("Backdrop").transform;
                backdrop.SetParent(hint, false);
            }
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(backdrop.gameObject);
            renderer.sprite = panel;
            renderer.color = new Color(1f, 1f, 1f, 0.9f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            backdrop.localPosition = new Vector3(0f, 0.01f, -0.07f);
            backdrop.localRotation = Quaternion.Euler(0f, 180f, 0f);
            float borderUnits = Mathf.Max(panel.border.x, panel.border.y) / panel.pixelsPerUnit;
            float scale = borderUnits > 0f ? 0.02f / borderUnits : 1f;
            renderer.drawMode = SpriteDrawMode.Sliced;
            backdrop.localScale = Vector3.one * scale;
            renderer.size = new Vector2(0.2f, 0.19f) / scale;
        }

        private static TMP_Text EnsureCaption(Transform hint)
        {
            Transform caption = hint.Find("Caption");
            if (caption == null)
            {
                caption = new GameObject("Caption", typeof(TextMeshPro)).transform;
                caption.SetParent(hint, false);
            }
            TextMeshPro text = caption.GetComponent<TextMeshPro>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaptionFont);
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            text.fontSize = 0.12f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = new Vector2(0.18f, 0.04f);
            caption.localPosition = new Vector3(0f, 0.075f, 0f);
            caption.localRotation = Quaternion.identity;
            caption.gameObject.SetActive(false);
            return text;
        }

        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : owner.AddComponent<T>();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }
    }
}
```

- [ ] **Step 4: Compilar y comprobar que el contrato falla antes de montar**

Run: `"$S/ucmd.sh" refresh 300`, y luego `"$S/ucmd.sh" "editmode TutorialHelp.Tests" 590`.
Expected: FAIL `TheOldTutorialIsGone`, `ThereIsExactlyOneTutorialHelp_FullyWired`, `TheHint_HasBothTouchPlus_WithATrigger` y `TheHalo_UsesTheTutorialsOwnShader`. `NoModuleReferencesTheTutorial` falla con NullReference porque todavía no existe la raíz.

- [ ] **Step 5: Añadir los textos y montar la escena**

Pide al usuario que **guarde o descarte** sus cambios abiertos en la v5, porque el montaje guarda la escena. Luego:

```bash
"$S/ucmd.sh" "exec TutorialHelpTerms.Apply" 200
"$S/ucmd.sh" "exec RideSafe.Help.Editor.TutorialHelpSceneSetup.Build" 300
tail -n 400 "$LOCALAPPDATA/Unity/Editor/Editor.log" | grep -a "\[TutorialHelp\]\|old Tutorial\|Exception" | head
```
Expected: el informe `[TutorialHelp] RideSafe_Garaje_v5:` con `old Tutorial removed (nothing referenced it)` y la línea `TutorialHelp: manager, highlighter, hint (PF_TouchPlus_Left, PF_TouchPlus_Right)`. Ninguna excepción.

Ejecuta `Build` una segunda vez. Expected: `old Tutorial: already gone`, y `git diff --stat Assets/_Project/Scene/RideSafe_Garaje_v5.unity` no crece entre la primera y la segunda ejecución.

- [ ] **Step 6: Correr los tests.**
Run: `"$S/ucmd.sh" "editmode TutorialHelp.Tests" 590`. Expected: `passed=52 failed=0`.
Run: `"$S/ucmd.sh" editmode 590` (todos los EditMode). Expected: los mismos fallos que había antes de este trabajo y ninguno nuevo. Anota antes la cifra base con la misma orden.

- [ ] **Step 7: Punto de control (sin commit).** `git status` debe mostrar solo:
  - lo nuevo en `Systems/TutorialHelp/`;
  - `Localization/Editor/TutorialHelpTerms.cs`;
  - `Assets/Resources/I2Languages.asset`;
  - `Scene/RideSafe_Garaje_v5.unity`;
  - además de lo que ya estaba modificado antes (horneado, SDF, ProjectSettings…).

---

### Task 8: Sonda, regresión y recorrido en PC

**Files:**
- Create: `Assets/_Project/Systems/TutorialHelp/Editor/TutorialHelpProbe.cs`

**Interfaces:**
- Consumes: `TutorialHelpManager.Director/Visible/HintView/Highlighter`, `GestureHintView.IsVisible/CaptionText/ShownHand`, `TargetHighlighter.HighlightedCount`.
- Produces: comandos para el puente: `RideSafe.Help.Editor.TutorialHelpProbe.Dump`, `.Click` (lee el nombre del objetivo de `Temp/ridesafe-arg.txt`) y `.Shot`.

- [ ] **Step 1: Escribir la sonda**

`Assets/_Project/Systems/TutorialHelp/Editor/TutorialHelpProbe.cs`:
```csharp
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RideSafe.Help.Editor
{
    /// <summary>Diagnostics for walking RideSafe_Garaje_v5 in Play mode through the editor bridge.</summary>
    public static class TutorialHelpProbe
    {
        public const string DumpPath = "Temp/ridesafe-probe.txt";
        public const string ArgPath = "Temp/ridesafe-arg.txt";
        public const string ShotPath = "Temp/ridesafe-shot.png";

        public static void Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("playing=" + Application.isPlaying + " frame=" + Time.frameCount);
            TutorialHelpManager manager = Object.FindFirstObjectByType<TutorialHelpManager>();
            if (manager == null || manager.Director == null)
            {
                sb.AppendLine("manager=NONE");
                File.WriteAllText(DumpPath, sb.ToString());
                return;
            }
            HelpDirector director = manager.Director;
            sb.AppendLine("observing=" + director.Observing + " decision=" + director.Decision + " hand=" + director.Hand +
                          " learnedUI=" + director.IsLearned(InteractionKind.Interface) +
                          " learned3D=" + director.IsLearned(InteractionKind.Object3D));
            sb.AppendLine("hint visible=" + manager.HintView.IsVisible + " shownHand=" + manager.HintView.ShownHand +
                          " caption='" + manager.HintView.CaptionText + "'");
            sb.AppendLine("highlighted=" + manager.Highlighter.HighlightedCount);
            sb.AppendLine("visible targets=" + manager.Visible.Count);
            foreach (HelpTarget target in manager.Visible)
                sb.AppendLine("  " + target.Kind + " " + Path(target.Transform));
            File.WriteAllText(DumpPath, sb.ToString());
        }

        /// <summary>Clicks, as a pointer would (down, up, click), the active object named in ArgPath.</summary>
        public static void Click()
        {
            string name = File.ReadAllText(ArgPath).Trim();
            GameObject target = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name == name || Path(t).EndsWith("/" + name))
                {
                    target = t.gameObject;
                    break;
                }
            }
            if (target == null)
                throw new System.InvalidOperationException("No active object named " + name);
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            PointerEventData data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(handler, data, ExecuteEvents.pointerClickHandler);
        }

        public static void Shot() => ScreenCapture.CaptureScreenshot(ShotPath);

        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
```

- [ ] **Step 2: Compilar.** Run: `"$S/ucmd.sh" refresh 300`. Expected: sin `error CS`.

- [ ] **Step 3: Regresión del módulo 1.** El puente se cuelga con PlayMode, así que pide al usuario que corra en **Window → General → Test Runner → PlayMode** el ensamblado `RideSafe.Module01.PlayModeTests`. Expected: los 6 en verde, como antes. Si falla alguno, compáralo antes de seguir con lo que fallaba sin este trabajo (usa `git stash` solo si el usuario lo autoriza).

- [ ] **Step 4: Recorrido en PC con capturas**

Pide al usuario que ponga Unity **al frente** y no lo tape. Luego:
```bash
R="C:/Users/ccscc/Documents/Trabajo/Freelance/RideSafe"; P="RideSafe.Help.Editor.TutorialHelpProbe"
"$S/ucmd.sh" play 200
"$S/ucmd.sh" "exec $P.Dump" 60; cat "$R/Temp/ridesafe-probe.txt"
```
Por cada momento: espera a que el `Dump` muestre lo esperado; luego `exec $P.Shot` y copia `Temp/ridesafe-shot.png` a `$S/walk-v5/NN_nombre.png`. Para hacer clic: `echo NOMBRE > "$R/Temp/ridesafe-arg.txt"` y luego `exec $P.Click`. Los nombres se toman de la lista `visible targets` del `Dump`.

| # | Momento | Esperado en `Dump` |
|---|---|---|
| 1 | Welcome, ≥1,5 s después de terminar la narración | `decision=FirstTime(Interface)`, `hint visible=True shownHand=RightHand`, `caption='Point and press the trigger'` (o su versión en español), `highlighted≥1` |
| 2 | Clic en `BeginButton` | `learnedUI=True`, `hint visible=False` |
| 3 | Language, elegir `Option_es` y `ContinueButton` | ninguna ayuda de primera vez; tras elegir español, los textos del módulo están en español |
| 4 | Jurisdiction y Vehicle, elegir y continuar | `hint visible=False` (salvo atasco) |
| 5 | Comfort: activar la mano izquierda y pulsar el botón de empezar | — |
| 6 | Quieto ≥10 s en una pantalla con opciones | `decision=Stuck(...)`, `caption=''`, `highlighted` = todas las opciones visibles |
| 7 | Módulo 1: primer objeto 3D visible (EPP o puerta) | `decision=FirstTime(Object3D)`, `shownHand=LeftHand`, `caption='Apunta al objeto y presiona el gatillo'`, halo visible en la captura |
| 8 | Durante un video del módulo 1 | `decision=None` mientras suena |

Al terminar, `"$S/ucmd.sh" stop 60` y revisa el log desde el inicio del Play:
`grep -a "Exception\|\[TutorialHelp\]"`. Expected: sin excepciones; solo avisos de `[TutorialHelp]` justificados.

- [ ] **Step 5: Ajuste visual.** Revisa las capturas 1, 6 y 7: legibilidad del texto, titileo visible pero suave, halo visible y control sin tapar la pantalla. Si hay que ajustar el tono (`_dimTone`), el tamaño (`fontSize`) o la posición (`_offset`), cambia solo esos valores serializados en la escena, en `TutorialHelp`, y repite el momento afectado.

- [ ] **Step 6: Punto de control (sin commit).** Entrega al usuario las capturas y el resumen.

---

### Task 9: Video del recorrido

**Files:** ninguno del proyecto. El video se guarda en el scratchpad.

- [ ] **Step 1: Título de la ventana de Unity**

Run (PowerShell): `(Get-Process Unity | Where-Object { $_.MainWindowTitle } | Select-Object -First 1).MainWindowTitle`
Expected: un título que contiene `RideSafe_Garaje_v5`.

- [ ] **Step 2: Grabar mientras se repite el recorrido de la Task 8**

Con Unity al frente (pídeselo al usuario), lanza en segundo plano:
```bash
/c/ffmpeg/bin/ffmpeg -y -f gdigrab -framerate 30 -i title="<título del Step 1>" -c:v libx264 -preset veryfast -pix_fmt yuv420p "$S/tutorial-v5.mp4"
```
Repite los momentos 1–8 de la Task 8 sin las capturas y deja unos 3 s entre pasos. Detén ffmpeg enviándole `q` por su entrada estándar, o cerrando el proceso tras el último paso.

- [ ] **Step 3: Verificar el video.** Run: `/c/ffmpeg/bin/ffmpeg -i "$S/tutorial-v5.mp4" 2>&1 | grep Duration`. Expected: una duración acorde al recorrido. Extrae un fotograma para comprobar que no está en negro: `ffmpeg -ss 5 -i ... -frames:v 1 "$S/check.png"` y ábrelo.

- [ ] **Step 4: Entregar la ruta del video al usuario.** Aclara que es el modo PC: los clics van por la sonda y no se ve el rayo moviéndose.
