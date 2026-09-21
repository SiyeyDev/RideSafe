using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Builds one prefab per module: a single world-space Canvas whose children are the
    /// module's panels, positioned as in the storyboard, with NO backdrop. ModuleUI shows
    /// one panel at a time.
    /// </summary>
    internal sealed partial class UIModules
    {
        public const string Folder = "Assets/DriveSafe/UI/Prefabs";

        /// <summary>A built module: its prefab and, per panel, whether the storyboard shows it over a dark scene.</summary>
        public sealed class Built
        {
            public GameObject Prefab;
            public readonly List<KeyValuePair<string, bool>> Panels = new List<KeyValuePair<string, bool>>();
        }

        public readonly List<Built> Modules = new List<Built>();

        private readonly UIElements _ui;
        private UIBuilderKit K => _ui.Kit;

        private GameObject _module;
        private Built _current;
        private readonly List<ModuleUI.Panel> _panels = new List<ModuleUI.Panel>();

        public UIModules(UIElements ui)
        {
            _ui = ui;
        }

        public void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/DriveSafe/UI", "Prefabs");

            BuildModule00();
            BuildModule01();
            BuildModule02();
        }

        #region Module scaffolding

        private void BeginModule(string name)
        {
            _module = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                                     typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            _module.layer = LayerMask.NameToLayer("UI");
            _module.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            UnityEngine.UI.CanvasScaler scaler = _module.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 4f;

            RectTransform rect = (RectTransform)_module.transform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(UIBuilderKit.FrameW, UIBuilderKit.FrameH);
            rect.localScale = Vector3.one * UIBuilderKit.WorldScale;

            _current = new Built();
            _panels.Clear();
        }

        /// <summary>Adds a full-frame panel under the module canvas.</summary>
        private Transform Panel(string id, bool darkContext = false)
        {
            Transform panel = UIBuilderKit.Group(_module.transform, id);
            _panels.Add(new ModuleUI.Panel { Id = id, Root = panel.gameObject });
            _current.Panels.Add(new KeyValuePair<string, bool>(id, darkContext));
            return panel;
        }

        /// <summary>Wires ModuleUI, settles layout with every panel active, then saves with only the first panel on.</summary>
        private void EndModule(string fileName)
        {
            ModuleUI module = _module.AddComponent<ModuleUI>();
            UIWire.Panels(module, "_panels", _panels);
            UIWire.Str(module, "_startPanel", _panels[0].Id);

            UIBuilderKit.RebuildLayouts(_module);
            for (int i = 1; i < _panels.Count; i++)
                _panels[i].Root.SetActive(false);

            _current.Prefab = PrefabUtility.SaveAsPrefabAsset(_module, Folder + "/" + fileName + ".prefab");
            Object.DestroyImmediate(_module);
            Modules.Add(_current);
            _module = null;
        }

        #endregion

        #region Shared pieces

        /// <summary>Centred title + subtitle pair with a CardView for runtime text.</summary>
        private CardView Header(Transform panel, string title, string subtitle, float titleY, float subtitleY,
                                float titleSize = 34f, bool onDark = false)
        {
            Transform group = UIBuilderKit.Group(panel, "Header");
            TMP_Text t = K.TC(group, title, (onDark ? K.H1OnDark : K.H1).With(titleSize), 501f, titleY, 960f, "Title");
            TMP_Text b = K.TC(group, subtitle, onDark ? K.SubOnDark : K.Sub, 501f, subtitleY, 960f, "Body");
            return Card(group.gameObject, null, t, b, null);
        }

        /// <summary>Dark translucent instruction panel (passthrough / mounting).</summary>
        private void DarkInstruction(Transform panel, string title, string body,
                                     float x0, float y0, float x1, float y1, float titleY, float bodyY, float titleSize)
        {
            Transform box = K.Slice(panel, "Panel_Modal_Dark", x0, y0, x1, y1, 2.4f, new Color(1f, 1f, 1f, 0.95f), "Instruction").transform;
            float pad = UIBuilderKit.PadFor(2.4f);
            float cx = (x1 - x0) * 0.5f + pad;
            TMP_Text t = K.TC(box, title, K.H1OnDark.With(titleSize), cx, titleY - y0 + pad, x1 - x0 - 20f, "Title");
            TMP_Text b = K.TC(box, body, K.SubOnDark.With(17f, UIBuilderKit.ClearWhite), cx, bodyY - y0 + pad, x1 - x0 - 20f, "Body");
            Card(box.gameObject, null, t, b, null);
        }

        private static CardView Card(GameObject host, TMP_Text counter, TMP_Text title, TMP_Text body, ProgressView progress,
                                     string counterFormat = null)
        {
            CardView card = host.AddComponent<CardView>();
            UIWire.Ref(card, "_counter", counter);
            UIWire.Ref(card, "_title", title);
            UIWire.Ref(card, "_body", body);
            UIWire.Ref(card, "_progress", progress);
            if (counterFormat != null)
                UIWire.Str(card, "_counterFormat", counterFormat);
            return card;
        }

        /// <summary>Converts storyboard coordinates to coordinates local to a sliced box's top-left.</summary>
        private static Vector2 Origin(float x0, float y0, float multiplier)
        {
            float pad = UIBuilderKit.PadFor(multiplier);
            return new Vector2(-x0 + pad, -y0 + pad);
        }

        #endregion
    }
}
