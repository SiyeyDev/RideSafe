using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// «Revisa tus elecciones» and «Mi Elección» scroll their rows inside a window instead of growing
    /// down past the view (PO, 2026-10-26: «un scrollview para que no quede tan largo sino que
    /// mantenga las proporciones»). With the twenty items a learner can take, the review went off the
    /// bottom and the final report pushed its buttons out of sight.
    /// <para>
    /// The rows move into a <c>Viewport</c> (RectMask2D, a transparent image so the ray can drag the
    /// list anywhere in it, and a vertical ScrollRect without rubber band) with a thin bar on its right
    /// that only shows when the rows do not fit. The window grows with the rows up to a cap:
    /// GrowWithContent for the review, ComparisonView for «Mi Elección». Idempotent.
    /// </para>
    /// </summary>
    internal static class UIScrollList
    {
        public const string ViewportName = "Viewport";
        public const float BarWidth = 8f;
        public const float BarGap = 6f;
        /// <summary>About eight rows of the review; past it they scroll.</summary>
        public const float ReviewVisibleHeight = 330f;
        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        [MenuItem("RideSafe/UI/Listas con scroll (Mi Elección y Revisa tus elecciones)")]
        public static void ApplyInPlace()
        {
            UIBuilderKit kit = RideSafeUIPrefabBuilder.CreateKit();
            GameObject root = PrefabUtility.LoadPrefabContents(UIComposition.Module01Path);
            try
            {
                ApplyToModule01(root, kit);
                PrefabUtility.SaveAsPrefabAsset(root, UIComposition.Module01Path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            if (kit.MissingSprites.Count > 0)
                throw new System.InvalidOperationException("Sprites not in the atlases: " + string.Join(", ", kit.MissingSprites));
            RevertRowOverridesInScene();
        }

        public static void ApplyToModule01(GameObject root, UIBuilderKit kit)
        {
            // «Revisa tus elecciones»: the box grows with the rows up to the cap, then they scroll.
            RectTransform modal = Find(root.transform, "ReviewChoices/Modal");
            RectTransform box = Find(modal, "ListBox");
            RectTransform reviewRows = Find(box, "Rows") ?? Find(box, ViewportName + "/Rows");
            ScrollRect review = Make(reviewRows, kit, false);
            RectTransform viewport = (RectTransform)review.transform;
            GrowWithContent grow = Ensure<GrowWithContent>(modal.gameObject);
            grow.Configure(viewport, box, new[] { modal },
                           new[]
                           {
                               Find(modal, "LockNotice"), Find(modal, "LockNoticeText"),
                               Find(modal, "KeepChoosingButton"), Find(modal, "ConfirmButton")
                           },
                           modal, reviewRows, Mathf.Max(0f, ReviewVisibleHeight - viewport.sizeDelta.y));
            EditorUtility.SetDirty(grow);

            // «Mi Elección»: the window sits in the list's column, above the empty notice.
            Transform comparison = root.transform.Find("Comparison");
            RectTransform column = Find(comparison, "LeftColumn");
            RectTransform choiceRows = Find(column, "Rows") ?? Find(column, ViewportName + "/Rows");
            ScrollRect choice = Make(choiceRows, kit, true);
            ComparisonView view = comparison.GetComponent<ComparisonView>();
            UIWire.Ref(view, "_column", column);
            UIWire.Ref(view, "_viewport", choice.GetComponent<LayoutElement>());
            EditorUtility.SetDirty(view);
        }

        /// <summary>Wraps <paramref name="rows"/> in a scrolled window, or restyles the one it is in.</summary>
        public static ScrollRect Make(RectTransform rows, UIBuilderKit kit, bool inLayout)
        {
            RectTransform viewport = rows.parent as RectTransform;
            if (viewport == null || viewport.name != ViewportName)
            {
                RectTransform host = (RectTransform)rows.parent;
                viewport = (RectTransform)UIBuilderKit.NewUi(ViewportName, host).transform;
                viewport.SetSiblingIndex(rows.GetSiblingIndex());
                viewport.anchorMin = rows.anchorMin;
                viewport.anchorMax = rows.anchorMax;
                viewport.pivot = rows.pivot;
                viewport.anchoredPosition = rows.anchoredPosition;
                viewport.sizeDelta = rows.sizeDelta;
                float height = rows.rect.height;
                rows.SetParent(viewport, false);
                if (inLayout)
                {
                    LayoutElement window = viewport.gameObject.AddComponent<LayoutElement>();
                    window.flexibleWidth = 1f;
                    window.minHeight = window.preferredHeight = height;
                }
            }

            // The rows hang from the top of the window, full width less the bar, at their own height.
            rows.anchorMin = new Vector2(0f, 1f);
            rows.anchorMax = Vector2.one;
            rows.pivot = new Vector2(0.5f, 1f);
            rows.offsetMin = new Vector2(0f, rows.offsetMin.y);
            rows.offsetMax = new Vector2(-(BarWidth + BarGap), 0f);
            rows.anchoredPosition = new Vector2(rows.anchoredPosition.x, 0f);
            ContentSizeFitter fit = Ensure<ContentSizeFitter>(rows.gameObject);
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
            Image catcher = Ensure<Image>(viewport.gameObject);
            catcher.color = new Color(1f, 1f, 1f, 0f);
            catcher.raycastTarget = true;

            ScrollRect scroll = Ensure<ScrollRect>(viewport.gameObject);
            scroll.content = rows;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 30f;
            scroll.verticalScrollbar = Bar(viewport, kit);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 0f;
            EditorUtility.SetDirty(scroll);
            return scroll;
        }

        /// <summary>A thin pill on the window's right edge: light track, navy handle.</summary>
        private static Scrollbar Bar(RectTransform viewport, UIBuilderKit kit)
        {
            Transform existing = viewport.Find("Scrollbar");
            GameObject bar = existing != null ? existing.gameObject : UIBuilderKit.NewUi("Scrollbar", viewport);
            RectTransform rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(BarWidth, 0f);
            const string pill = "Button_Secondary_Default";
            float multiplier = UIBuilderKit.PillMultiplier(pill, BarWidth);
            Image track = kit.AddSliced(bar, pill, multiplier, UIBuilderKit.Track, true);

            Transform areaT = rect.Find("Sliding Area");
            RectTransform area = areaT != null ? (RectTransform)areaT : (RectTransform)UIBuilderKit.NewUi("Sliding Area", rect).transform;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;
            Transform handleT = area.Find("Handle");
            RectTransform handle = handleT != null ? (RectTransform)handleT : (RectTransform)UIBuilderKit.NewUi("Handle", area).transform;
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            Image grip = kit.AddSliced(handle.gameObject, pill, multiplier, UIBuilderKit.BeamNavy, true);

            Scrollbar scrollbar = Ensure<Scrollbar>(bar);
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = grip;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            scrollbar.transition = Selectable.Transition.None;
            track.raycastTarget = true;
            EditorUtility.SetDirty(scrollbar);
            return scrollbar;
        }

        /// <summary>
        /// The v5 scene stores layout-driven RectTransform values of the instance as overrides; on the
        /// two rows they would pin the old top-left rect over the new top-stretch one.
        /// </summary>
        private static void RevertRowOverridesInScene()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                return;
            int reverted = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true))
                {
                    bool scrolled = r.parent != null && r.parent.name == ViewportName &&
                                    (r.name == "Rows" || r.name == "Scrollbar");
                    if (!scrolled && r.name != ViewportName)
                        continue;
                    if (!PrefabUtility.IsPartOfPrefabInstance(r))
                        continue;
                    PrefabUtility.RevertObjectOverride(r, InteractionMode.AutomatedAction);
                    reverted++;
                }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RideSafe.UI] Scroll lists: reverted " + reverted + " RectTransform override(s) in " + ScenePath);
        }

        private static RectTransform Find(Transform root, string path) => root.Find(path) as RectTransform;

        // Not «GetComponent() ?? AddComponent()»: in the editor a missing component comes back as a fake null.
        private static T Ensure<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
