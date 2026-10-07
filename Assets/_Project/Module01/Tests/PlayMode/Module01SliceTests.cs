using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    // Exercise the saved scene through screen-space raycasts, never Accept/StartWithDefaults/onClick.Invoke.
    public class Module01SliceTests
    {
        private const string ResultPath = "Temp/ridesafe-playmode.txt";
        private readonly List<string> _problems = new List<string>();

        private Module01Director _director;
        private ItemInspector _inspector;
        private Module01Binding _binding;
        private Camera _camera;

        [OneTimeSetUp] public void StartReport() => System.IO.File.WriteAllText(ResultPath, "PLAYMODE RUNNING\n");
        [OneTimeTearDown] public void EndReport() => System.IO.File.AppendAllText(ResultPath, "PLAYMODE DONE\n");
        [UnitySetUp] public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _problems.Clear();
            Application.logMessageReceived += Collect;
            SceneManager.LoadScene("RideSafe_Garaje_v5");
            yield return null; yield return null;

            _director = Object.FindAnyObjectByType<Module01Director>();
            _inspector = Object.FindAnyObjectByType<ItemInspector>();
            _binding = Object.FindAnyObjectByType<Module01Binding>();
            _camera = GameObject.Find("Desktop Camera").GetComponent<Camera>();
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Application.logMessageReceived -= Collect;
            LogAssert.ignoreFailingMessages = false;
            var r = TestContext.CurrentContext.Result;
            // La traza va al archivo porque la recarga de dominio al salir de PlayMode se lleva
            // los callbacks del TestRunnerApi: sin esto solo queda el mensaje, sin linea culpable.
            System.IO.File.AppendAllText(ResultPath, $"{r.Outcome.Status}  {TestContext.CurrentContext.Test.Name}\n{r.Message}\n{r.StackTrace}\n");
            yield return null;
        }
        [UnityTest] public IEnumerator El_modulo_recorre_de_la_primera_zona_al_reporte_final()
        { yield return RunFlow(false, true, scripted: true); }
        [UnityTest] public IEnumerator Rechazar_el_casco_correcto_lo_reporta_como_omision_critica()
        { yield return RunFlow(true, false, scripted: false); }

        private IEnumerator RunFlow(bool omitHelmet, bool subtitles, bool scripted)
        {
            Assert.AreEqual(1, Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled && c.cameraType == CameraType.Game), "Debe haber una sola camara de juego en modo PC.");
            Assert.Greater(_camera.transform.position.z, 6.6f, "El jugador debe empezar fuera del garaje.");
            var door = GameObject.Find("Door_Garage_Brown_5x").transform;
            var tourForFade = Object.FindAnyObjectByType<GuidedTour>();
            var fade = Field<CanvasGroup>(tourForFade, "_fade");
            Assert.IsNotNull(fade);
            int blackTeleports = 0;
            tourForFade.ZoneReached += _ => { Assert.GreaterOrEqual(fade.alpha, .99f, "La camara salto sin pantalla negra"); blackTeleports++; };
            Vector3 closed = door.position;
            Vector3 outside = _camera.transform.position;
            yield return Onboarding(scripted, subtitles);
            yield return WaitFor(() => _binding.Module.gameObject.activeInHierarchy && _binding.Module.Current == "Orientation", "Entrada al garaje", 8);
            Assert.Greater(Vector3.Distance(door.position, closed), 1, "La puerta no se abrio.");
            Assert.Greater(Vector3.Distance(_camera.transform.position, outside), 2, "No hubo teletransporte al interior.");
            foreach (var renderer in door.GetComponentsInChildren<Renderer>())
                Assert.Greater(renderer.bounds.min.y, 2.1f, "La puerta aun bloquea la altura de paso");
            Assert.IsFalse(fade.blocksRaycasts, "El fade sigue interceptando clics");
            yield return Press(_binding.Module.Get<RectTransform>("Orientation"), "StartButton");

            SelectionReport personal = null, vehicle = null;
            _director.PersonalSectionCompleted += r => personal = r;
            _director.VehicleSectionCompleted += r => vehicle = r;
            string[] zones = { "head", "clothing", "load", "cockpit", "wheels", "visibility" };
            foreach (string zoneId in zones)
            {
                yield return WaitFor(() => _director.CurrentZone != null && _director.CurrentZone.ZoneId == zoneId && Object.FindAnyObjectByType<Module01Experience>().Phase == "Choosing" && Field<CanvasGroup>(Object.FindAnyObjectByType<GuidedTour>(), "_fade").alpha == 0, "Llegada a " + zoneId);
                var tour = Object.FindAnyObjectByType<GuidedTour>();
                var anchor = GameObject.Find("anchor_" + zoneId).transform;
                Assert.Less(Vector3.Distance(_camera.transform.position, anchor.position), .05f, "La camara no alcanzo el ancla " + zoneId);
                Assert.Less(Quaternion.Angle(_camera.transform.rotation, anchor.rotation), 1);
                AssertVisible(_binding.Sidebar.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Title").rectTransform);
                var experience = Object.FindAnyObjectByType<Module01Experience>();
                Assert.AreEqual(subtitles, Field<GameObject>(experience, "_instruction").activeInHierarchy, "La preferencia de subtitulos no se conserva");
                foreach (var item in _director.CurrentZone.Items)
                {
                    var entity = Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsSortMode.None).Single(e => e.ItemId == item.ItemId);
                    yield return ClickWorld(entity);
                    Assert.IsTrue(_inspector.IsPresenting, "No abre el inspector: " + item.ItemId);
                    var label = Field<TMP_Text>(_inspector, "_nameLabel");
                    AssertVisible(label.rectTransform);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(label.text));
                    Assert.IsFalse(label.text.StartsWith("Module1/"), "Nombre sin traducir");
                    var stage = Field<Transform>(_inspector, "_stage");
                    Assert.Greater(stage.childCount, 0, "No aparece el modelo");
                    var model = stage.GetChild(0);
                    Assert.IsTrue(model.GetComponentsInChildren<Renderer>().Any(r => r.enabled));
                    Quaternion before = model.rotation;
                    yield return new WaitForSeconds(.15f);
                    Assert.Greater(Quaternion.Angle(before, model.rotation), 1, "El EPP no gira");
                    AssertPedestalClearsPopup();
                    AssertModelClearsName();
                    AssertHudDoesNotOverlap();
                    var asked = Field<TMP_Text>(_inspector, "_questionLabel");
                    Assert.IsNotEmpty(asked.text, "El popup pregunta en blanco en " + item.ItemId);
                    Assert.IsFalse(asked.text.StartsWith("Module1/"), "Pregunta sin traducir: " + asked.text);
                    if (!omitHelmet && item.ItemId == "helmet_ok") Capture("inspector");
                    bool take = item.Category == SafetyItemCategory.Core || item.Category == SafetyItemCategory.ConditionDependent;
                    if (item.ItemId == "helmet_ok" && omitHelmet) take = false;
                    var button = Field<Button>(_inspector, take ? "_acceptButton" : "_declineButton");
                    AssertOnBrand(button);
                    yield return ClickGraphic(button.GetComponent<RectTransform>());
                    Assert.IsFalse(_inspector.IsPresenting, "El boton no cerro el inspector");
                    Assert.AreEqual(take, _binding.PreparationList.Contains(item.ItemId));
                    Assert.AreEqual(zoneId, _director.CurrentZone.ZoneId, "La zona avanzo antes de pulsar continuar");
                }
                AssertHudDoesNotOverlap();   // ahora sin presentar: vuelven navegacion e instruccion
                yield return Press(_binding.Module.transform, "NextZoneButton");
                if (zoneId == "load")
                {
                    Assert.AreEqual("ReviewChoices", _binding.Module.Current);
                    Assert.IsNull(personal, "Feedback antes del envio");
                    yield return Press(_binding.Module.Get<RectTransform>("ReviewChoices"), "ConfirmButton");
                    Assert.IsNotNull(personal);
                    Assert.AreEqual(omitHelmet, personal.HasCriticalProblem);
                    yield return ReviewAndVideo();
                }
                if (zoneId == "visibility")
                {
                    AssertHudDoesNotOverlap();   // con el panel de decision en pantalla
                    yield return Press(_binding.Module.transform, "RoadReadyButton");
                    Assert.IsNotNull(vehicle);
                    yield return ReviewAndVideo();
                }
            }
            yield return WaitFor(() => _director.IsFinished, "Reporte final");
            Assert.AreEqual("Comparison", _binding.Module.Current);
            AssertVisible(_binding.Comparison.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Title").rectTransform);
            Assert.IsTrue(_binding.Comparison.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("point_brakes")), "El reporte no muestra el vehiculo");
            Assert.GreaterOrEqual(blackTeleports, 7, "No se probaron todos los cambios de zona con fade");
            if (!omitHelmet) Capture("final-report");
            CollectionAssert.IsEmpty(_problems, string.Join("\n", _problems));
        }
        private IEnumerator ReviewAndVideo()
        {
            Assert.AreEqual("Comparison", _binding.Module.Current);
            yield return Press(_binding.Comparison.transform, "ExplainButton");
            Assert.AreEqual("Explanation", _binding.Module.Current);
            var video = _binding.Module.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            Assert.IsNotNull(video);
            yield return WaitFor(() => video.isPlaying && video.frame > 0,
                () => $"El video no se reprodujo: clip={(video.clip == null ? "null" : video.clip.name)} "
                    + $"prepared={video.isPrepared} playing={video.isPlaying} frame={video.frame} "
                    + $"renderMode={video.renderMode} target={(video.targetTexture == null ? "null" : video.targetTexture.name)} "
                    + $"audio={video.audioOutputMode} canPlay={video.canSetTime}", 15);
            AssertVisible(_binding.Explanation.VideoSurface.rectTransform);
            Assert.IsNotNull(_binding.Explanation.VideoSurface.texture);
            yield return Press(_binding.Explanation.transform, "PauseButton");
            Assert.IsFalse(video.isPlaying);
            yield return Press(_binding.Explanation.transform, "ReplayButton");
            yield return WaitFor(() => video.isPlaying, "Repetir video");
            yield return Press(_binding.Explanation.transform, "ContinueButton");
        }
        /// <summary>
        /// Entra al módulo 1 pulsando Begin, nunca llamando a StartWithDefaults: ese atajo apagaba
        /// todos los ModuleUI y por eso la escena pudo tener dos PF_Module00_UI superpuestos sin
        /// que ningún test se enterara. Con <paramref name="scripted"/> Begin usa el asset de
        /// parámetros; sin él recorre las cinco pantallas de verdad.
        /// </summary>
        private IEnumerator Onboarding(bool scripted, bool subtitles)
        {
            var experience = Object.FindAnyObjectByType<Module01Experience>();
            var onboardings = Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != _binding.Module).ToArray();
            Assert.AreEqual(1, onboardings.Length,
                "La escena tiene " + onboardings.Length + " UI de onboarding. Dos copias superpuestas dejan el Welcome encendido al pulsar Begin.");
            var onboarding = onboardings[0];
            Assert.IsTrue(onboarding.gameObject.activeInHierarchy, "El UI del modulo 0 esta apagado; Begin no se puede pulsar.");

            var quickStart = Field<Module00SettingsSO>(experience, "_quickStart");
            Assert.IsNotNull(quickStart, "Falta el asset de parametros del modulo 0 en _quickStart.");
            if (!scripted) SetField(experience, "_quickStart", null);

            var welcome = onboarding.Get<RectTransform>("Welcome");
            yield return WaitFor(() => onboarding.Current == "Welcome", "Welcome visible");
            yield return Press(welcome, "BeginButton");
            Assert.IsFalse(welcome.gameObject.activeInHierarchy, "El panel Welcome sigue encendido despues de Begin.");

            if (scripted)
            {
                Assert.AreEqual("Entrance", experience.Phase, "Begin con parametros no entro al modulo 1.");
                Assert.AreEqual(quickStart.Vehicle.ToLowerInvariant(), experience.Context.Get(Module01Context.VehicleKey));
                Assert.AreEqual(quickStart.Jurisdiction.ToLowerInvariant(), experience.Context.Get(Module01Context.JurisdictionKey));
                Assert.AreEqual(subtitles, experience.SubtitlesEnabled, "El asset no impuso la preferencia de subtitulos.");
                yield break;
            }

            Assert.AreEqual("Language", onboarding.Current, "Begin sin parametros deberia abrir Language.");
            yield return Choose(onboarding.Get<ChoicePanel>("Language"), "Option_es");
            yield return Choose(onboarding.Get<ChoicePanel>("Jurisdiction"), "Option_bogota");
            yield return Choose(onboarding.Get<ChoicePanel>("Vehicle"), "Option_ebike");
            // Lo que el aprendiz eligió tiene que llegar al contexto, no solo pasar de pantalla.
            Assert.AreEqual("ebike", experience.Context.Get(Module01Context.VehicleKey), "El vehiculo elegido no llego al contexto.");
            Assert.AreEqual("bogota", experience.Context.Get(Module01Context.JurisdictionKey), "La jurisdiccion elegida no llego al contexto.");

            var comfort = onboarding.Get<ComfortSettingsPanel>("Comfort");
            yield return WaitFor(() => comfort.gameObject.activeInHierarchy, "Comfort visible");
            if (comfort.Current.Subtitles != subtitles)
                yield return PressControl<Toggle>(comfort.transform, "SubtitlesSwitch");
            Assert.AreEqual(subtitles, comfort.Current.Subtitles, "El interruptor de subtitulos no cambio.");
            yield return Press(comfort.transform, "StartButton");
            Assert.AreEqual(subtitles, experience.SubtitlesEnabled, "El modulo 1 no recibio la preferencia de subtitulos.");
        }
        private IEnumerator Choose(ChoicePanel panel, string optionName)
        {
            yield return WaitFor(() => panel.gameObject.activeInHierarchy, "Panel visible " + panel.name);
            yield return PressControl<Toggle>(panel.transform, optionName);
            // Vehicle no confirma con "ContinueButton" sino con "ConfirmButton", en una barra que
            // nace apagada. Se pulsa el que el ChoicePanel tiene cableado, no uno buscado por nombre.
            var confirm = Field<Button>(panel, "_continueButton");
            Assert.IsNotNull(confirm, "ChoicePanel sin boton de continuar: " + panel.name);
            yield return WaitFor(() => confirm.gameObject.activeInHierarchy && confirm.IsInteractable(),
                "Continuar habilitado en " + panel.name);
            yield return ClickGraphic(confirm.GetComponent<RectTransform>());
        }
        /// <summary>Como Press, pero para cualquier Selectable y no solo Button.</summary>
        private IEnumerator PressControl<T>(Transform root, string name) where T : Selectable
        {
            var target = root.GetComponentsInChildren<T>(true).FirstOrDefault(x => x.name == name);
            Assert.IsNotNull(target, "No hay " + typeof(T).Name + " llamado " + name);
            Assert.IsTrue(target.IsInteractable(), "Deshabilitado: " + name);
            yield return ClickGraphic(target.GetComponent<RectTransform>());
        }
        private IEnumerator ChooseFirst(Transform root)
        {
            var toggle = root.GetComponentsInChildren<Toggle>().First(t => t.IsInteractable());
            if (!toggle.isOn) yield return ClickGraphic(toggle.GetComponent<RectTransform>());
        }
        private IEnumerator Press(Transform root, string name)
        {
            var b = root.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == name);
            Assert.IsNotNull(b, "No hay boton visible " + name);
            Assert.IsTrue(b.IsInteractable(), "Boton deshabilitado: " + name);
            AssertOnBrand(b);
            yield return ClickGraphic(b.GetComponent<RectTransform>());
        }

        /// <summary>
        /// El pedestal es un disco: su borde cercano se adelanta hacia la camara, asi que en
        /// pantalla cuelga bastante mas abajo que su centro y puede comerse el techo del popup.
        /// Comparar alturas en el mundo NO lo detecta; hay que proyectar la silueta.
        /// </summary>
        private void AssertPedestalClearsPopup()
        {
            Bounds disc = Field<GameObject>(_inspector, "_pedestal").GetComponent<Renderer>().bounds;
            var confirm = (RectTransform)Field<GameObject>(_inspector, "_confirmRoot").transform;
            var corners = new Vector3[4];
            confirm.GetWorldCorners(corners);
            Assert.Less(corners.Max(c => _camera.WorldToScreenPoint(c).y), Lowest(disc), string.Format(
                "El pedestal se monta sobre el popup (disco baja a y={0:0})", Lowest(disc)));
        }

        /// <summary>
        /// Nada de lo que esté encendido puede taparse entre sí. Quién está encendido lo decide
        /// el código, no el test: simular los estados a mano se equivoca en cuáles coexisten.
        /// </summary>
        private void AssertHudDoesNotOverlap()
        {
            string[] names = { "Confirmation", "ItemName", "Navigation", "StationInstruction",
                               "ReadinessDecision", "FinalSummary", "ChecklistPanel" };
            var live = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                             .Where(x => names.Contains(x.name)).ToArray();
            for (int i = 0; i < live.Length; i++)
                for (int j = i + 1; j < live.Length; j++)
                {
                    Rect a = ScreenRect(live[i]), b = ScreenRect(live[j]);
                    Assert.IsFalse(a.Overlaps(b),
                        "Se tapan en pantalla: " + live[i].name + " " + a + " y " + live[j].name + " " + b);
                }
        }

        private Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var pts = corners.Select(c => (Vector2)_camera.WorldToScreenPoint(c)).ToArray();
            float x0 = pts.Min(p => p.x), x1 = pts.Max(p => p.x);
            float y0 = pts.Min(p => p.y), y1 = pts.Max(p => p.y);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        /// <summary>El modelo tampoco puede taparle el nombre, y por la misma razón: perspectiva.</summary>
        private void AssertModelClearsName()
        {
            var renderers = Field<Transform>(_inspector, "_stage").GetComponentsInChildren<Renderer>();
            Bounds model = renderers[0].bounds;
            foreach (var r in renderers) model.Encapsulate(r.bounds);
            var label = Field<TMP_Text>(_inspector, "_nameLabel").rectTransform;
            var corners = new Vector3[4];
            label.GetWorldCorners(corners);
            Assert.Greater(corners.Min(c => _camera.WorldToScreenPoint(c).y), Highest(model), string.Format(
                "El modelo tapa el nombre (el modelo sube a y={0:0})", Highest(model)));
        }

        /// <summary>Las 8 esquinas, no el centro: una caja con fondo proyecta más alto y más bajo que su centro.</summary>
        private static IEnumerable<float> Projected(Bounds b, Camera camera)
        {
            for (int k = 0; k < 8; k++)
                yield return camera.WorldToScreenPoint(b.center + Vector3.Scale(b.extents,
                    new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1))).y;
        }
        private float Lowest(Bounds b) => Projected(b, _camera).Min();
        private float Highest(Bounds b) => Projected(b, _camera).Max();

        /// <summary>
        /// Todo boton sale del kit del atlas, lo construya el prefab o el montador de escena.
        /// Un Image de color plano con la fuente por defecto de TMP se ve pegado al lado del
        /// modulo 0, y es justo lo que este test esta para no dejar pasar.
        /// </summary>
        private static void AssertOnBrand(Button b)
        {
            Assert.AreEqual(Selectable.Transition.SpriteSwap, b.transition, "Boton sin sprite-swap: " + b.name);
            Assert.IsNotNull((b.targetGraphic as Image)?.sprite, "Boton sin pastilla del atlas: " + b.name);
            Assert.IsNotNull(b.spriteState.pressedSprite, "Boton sin estado Pressed: " + b.name);
            Assert.IsNotNull(b.spriteState.highlightedSprite, "Boton sin estado Hover: " + b.name);
            var label = b.GetComponentInChildren<TMP_Text>(true);
            Assert.IsNotNull(label, "Boton sin etiqueta: " + b.name);
            Assert.AreNotEqual(TMP_Settings.defaultFontAsset, label.font,
                               "Boton con la fuente por defecto de TMP en vez de la del kit: " + b.name);
        }
        private IEnumerator ClickGraphic(RectTransform rect)
        {
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            AssertVisible(rect);
            var canvas = rect.GetComponentInParent<Canvas>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _camera, rect.TransformPoint(rect.rect.center));
            yield return Dispatch(point, rect.gameObject);
        }
        private IEnumerator ClickWorld(WorldObjectTaskEntity entity)
        {
            Physics.SyncTransforms();
            var collider = entity.GetComponent<Collider>();
            Vector3 p = _camera.WorldToScreenPoint(collider.bounds.center);
            Assert.IsTrue(p.z > 0 && p.x > 0 && p.x < Screen.width && p.y > 0 && p.y < Screen.height, "Objeto fuera de pantalla: " + entity.ItemId);
            yield return Dispatch(p, entity.gameObject);
        }
        private IEnumerator Dispatch(Vector2 point, GameObject expected)
        {
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.IsNotEmpty(hits, "Ningun raycast en " + expected.name);
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.AreEqual(expected, target, "Clic bloqueado por " + hits[0].gameObject.name);
            data.pointerCurrentRaycast = hits[0];
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
        }
        private void AssertVisible(RectTransform rect)
        {
            Assert.IsTrue(rect.gameObject.activeInHierarchy, "UI inactiva " + rect.name);
            foreach(var group in rect.GetComponentsInParent<CanvasGroup>()) Assert.Greater(group.alpha, .9f);
            var canvas = rect.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas); Assert.IsTrue(canvas.enabled);
            Vector3 p = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? rect.TransformPoint(rect.rect.center) : _camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
            Assert.IsTrue((canvas.renderMode == RenderMode.ScreenSpaceOverlay || p.z > 0) && p.x > 0 && p.x < Screen.width && p.y > 0 && p.y < Screen.height, "UI fuera de pantalla: " + rect.name + " " + p);
        }
        private void Capture(string name)
        {
            const string folder = "Docs/verification/module01";
            System.IO.Directory.CreateDirectory(folder);
            var target = new RenderTexture(1280, 720, 24);
            var previous = _camera.targetTexture;
            var active = RenderTexture.active;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                _camera.targetTexture = target; _camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                System.IO.File.WriteAllBytes(folder + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                _camera.targetTexture = previous; RenderTexture.active = active;
                target.Release(); Object.Destroy(target); Object.Destroy(texture);
            }
        }
        private static void SetField(object o, string name, object value) =>
            o.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(o, value);
        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(o);
        private static IEnumerator WaitFor(System.Func<bool> condition, string label, float seconds = 5)
        { yield return WaitFor(condition, () => label, seconds); }
        /// <summary>Mensaje diferido: se arma al expirar, con el estado real en vez de un "Timeout" pelado.</summary>
        private static IEnumerator WaitFor(System.Func<bool> condition, System.Func<string> label, float seconds = 5)
        { float until = Time.realtimeSinceStartup + seconds; while (!condition() && Time.realtimeSinceStartup < until) yield return null; Assert.IsTrue(condition(), "Timeout: " + label()); }
        private void Collect(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) if (message.Contains("Module01") || stack.Contains("RideSafe.Module01")) _problems.Add(message); }
    }
}






