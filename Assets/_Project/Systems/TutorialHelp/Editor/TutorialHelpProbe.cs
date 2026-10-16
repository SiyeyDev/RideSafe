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

        /// <summary>Everything a ray from the main camera to the object named in ArgPath crosses, nearest first.</summary>
        public static void Ray()
        {
            string name = File.ReadAllText(ArgPath).Trim();
            Camera camera = Camera.main;
            StringBuilder sb = new StringBuilder("ray from " + (camera != null ? camera.name : "NO CAMERA") + " to " + name + System.Environment.NewLine);
            Transform target = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name == name) { target = t; break; }
            if (camera != null && target != null)
            {
                Vector3 to = target.position - camera.transform.position;
                RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, to.normalized, to.magnitude + 0.5f, Physics.AllLayers, QueryTriggerInteraction.Collide);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                    sb.AppendLine("  " + hit.distance.ToString("F2") + " " + Path(hit.transform) + " layer=" + LayerMask.LayerToName(hit.collider.gameObject.layer) + " trigger=" + hit.collider.isTrigger);
                sb.AppendLine("distance to target " + to.magnitude.ToString("F2"));
            }
            File.WriteAllText(DumpPath, sb.ToString());
        }

        /// <summary>Every 3D click receiver in the scene and why the scanner keeps or drops it.</summary>
        public static void Candidates()
        {
            Camera camera = Camera.main;
            Pose viewer = new Pose(camera.transform.position, camera.transform.rotation);
            StringBuilder sb = new StringBuilder();
            foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(b is UnityEngine.EventSystems.IPointerClickHandler) || b is UnityEngine.UI.Selectable || b.GetComponentInParent<Canvas>() != null)
                    continue;
                Collider c = b.GetComponentInChildren<Collider>(false);
                string why;
                if (!b.isActiveAndEnabled) why = "inactive";
                else if (c == null || !c.enabled) why = "no collider";
                else
                {
                    Vector3 to = c.transform.position - viewer.position;
                    float angle = Vector3.Angle(viewer.rotation * Vector3.forward, to);
                    why = "angle=" + angle.ToString("F0") + " dist=" + to.magnitude.ToString("F2") +
                          " inView=" + TargetScanner.InView(viewer, c.transform.position) +
                          " reachable=" + TargetScanner.Reachable(viewer, c, b.transform);
                }
                sb.AppendLine(b.GetType().Name + " " + Path(b.transform) + " : " + why);
            }
            sb.AppendLine("camera fov(v)=" + camera.fieldOfView.ToString("F0") + " aspect=" + camera.aspect.ToString("F2"));
            File.WriteAllText(DumpPath, sb.ToString());
        }

        /// <summary>Every loaded object whose name contains the text in ArgPath (include inactive): path, world position, active, particles.</summary>
        public static void Where()
        {
            string part = File.ReadAllText(ArgPath).Trim();
            StringBuilder sb = new StringBuilder();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (Path(t).IndexOf(part, System.StringComparison.OrdinalIgnoreCase) < 0 || t.hideFlags != HideFlags.None)
                    continue;
                sb.AppendLine(Path(t) + (t is RectTransform rt ? " rect=" + rt.rect.size.ToString("F1") : "") + " pos=" + t.position.ToString("F2") + " active=" + t.gameObject.activeInHierarchy +
                              " particles=" + t.GetComponentsInChildren<ParticleSystem>(true).Length);
            }
            File.WriteAllText(DumpPath, sb.ToString());
        }

        /// <summary>Each module 1 inspection marker ('!') against the clickable sphere of its point.</summary>
        public static void Markers()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.GetType().Name != "InspectionMarker")
                    continue;
                TMPro.TMP_Text glyph = b.GetComponentInChildren<TMPro.TMP_Text>(true);
                Vector3 g = glyph != null ? glyph.transform.position : b.transform.position;
                SphereCollider nearest = null;
                float best = float.MaxValue;
                foreach (SphereCollider c in Object.FindObjectsByType<SphereCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    float d = Vector3.Distance(c.transform.TransformPoint(c.center), g);
                    if (d < best) { best = d; nearest = c; }
                }
                string sphere = nearest == null ? "none" :
                    Path(nearest.transform) + " centre=" + nearest.transform.TransformPoint(nearest.center).ToString("F2") +
                    " radius=" + (nearest.radius * nearest.transform.lossyScale.x).ToString("F2") + " enabled=" + nearest.enabled +
                    " activeGO=" + nearest.gameObject.activeInHierarchy;
                sb.AppendLine(Path(b.transform) + " glyph=" + g.ToString("F2") + " active=" + b.gameObject.activeInHierarchy +
                              " | nearest sphere " + sphere + " dist=" + best.ToString("F2"));
            }
            File.WriteAllText(DumpPath, sb.ToString());
        }

        /// <summary>Enters module 1 with the project's test settings (Module01Experience.StartWithDefaults).</summary>
        public static void QuickStart()
        {
            MonoBehaviour experience = null;
            foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (b.GetType().Name == "Module01Experience") experience = b;
            string[] guids = UnityEditor.AssetDatabase.FindAssets("SO_Module00_TestSettings");
            Object settings = UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            experience.GetType().GetMethod("StartWithDefaults").Invoke(experience, new object[] { settings });
        }

        /// <summary>Shows the module 1 panel named in ArgPath; ReviewChoices gets 7 rows.</summary>
        public static void ShowPanel()
        {
            string id = File.ReadAllText(ArgPath).Trim();
            foreach (RideSafe.UI.ModuleUI module in Object.FindObjectsByType<RideSafe.UI.ModuleUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (module.name != "PF_Module01_UI")
                    continue;
                module.gameObject.SetActive(true);
                module.Show(id);
                if (id == "Comparison")
                {
                    foreach (Transform t in module.GetComponentsInChildren<Transform>(true))
                        if (t.name == "FinalResult") t.gameObject.SetActive(true);
                    var entries = new System.Collections.Generic.List<RideSafe.UI.ComparisonEntry>();
                    string[] names = { "Casco íntegro y ajustado", "Casco deteriorado", "Gafas", "Calzado cerrado", "Chaleco reflectivo",
                                       "Ropa o cordones sueltos", "Coderas", "Carga asegurada", "Dirección y manubrio", "Frenos",
                                       "Estado eléctrico y controles" };
                    for (int i = 0; i < names.Length; i++)
                        entries.Add(new RideSafe.UI.ComparisonEntry((RideSafe.UI.ItemStatus)(i % 3), names[i]));
                    module.Get<RideSafe.UI.ComparisonView>(id).SetSingleList("Mi Elección · Recapacitar · 46%",
                        "Faltó buena parte del equipo imprescindible. Vuelve a recorrer el garaje sin prisa antes de circular.", entries);
                }
                if (id == "ReviewChoices")
                {
                    RideSafe.UI.ChecklistView view = module.Get<RideSafe.UI.ChecklistView>(id);
                    var items = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();
                    foreach (string name in new[] { "Casco íntegro y ajustado", "Gafas", "Chaleco reflectivo", "Rodilleras",
                                                    "Pantalón ajustado", "Calzado cerrado", "Carga asegurada" })
                        items.Add(new System.Collections.Generic.KeyValuePair<string, string>(name, name));
                    view.SetItems(items);
                }
            }
        }

        /// <summary>The images GestureHintView counts as module panels: path, alpha, viewport rect.</summary>
        public static void Panels()
        {
            Camera camera = Camera.main;
            StringBuilder sb = new StringBuilder();
            Vector3[] corners = new Vector3[4];
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!canvas.isActiveAndEnabled || canvas.rootCanvas.renderMode != RenderMode.WorldSpace || canvas.transform.root.name == "TutorialHelp")
                    continue;
                var graphics = UnityEngine.UI.GraphicRegistry.GetGraphicsForCanvas(canvas);
                for (int i = 0; i < graphics.Count; i++)
                {
                    var g = graphics[i];
                    if (!(g is UnityEngine.UI.Image || g is UnityEngine.UI.RawImage) || !g.isActiveAndEnabled || g.color.a < 0.05f)
                        continue;
                    g.rectTransform.GetWorldCorners(corners);
                    Vector2 min = new Vector2(9, 9), max = new Vector2(-9, -9);
                    foreach (Vector3 c in corners) { Vector3 v = camera.WorldToViewportPoint(c); min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
                    sb.AppendLine(Path(g.transform) + " a=" + g.color.a.ToString("F2") + " crA=" + g.canvasRenderer.GetInheritedAlpha().ToString("F2") +
                                  " x=" + min.x.ToString("F2") + ".." + max.x.ToString("F2") + " y=" + min.y.ToString("F2") + ".." + max.y.ToString("F2"));
                }
            }
            File.WriteAllText(DumpPath, sb.ToString());
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
