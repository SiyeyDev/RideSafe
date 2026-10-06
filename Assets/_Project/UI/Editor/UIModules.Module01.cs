using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Module 1 — preparation, diagnostic and review (storyboard frames 06–13B).
    /// Comparison and Explanation are one panel each: the safety review (10/10B) and the
    /// diagnostic review (13/13B) differ only in content, set through their APIs.
    /// </summary>
    internal sealed partial class UIModules
    {
        private void BuildModule01()
        {
            BeginModule("PF_Module01_UI");
            Orientation(Panel("Orientation"));
            Checklist(Panel("Preparation"), "Prepare for your ride", "Choose the items you would take with you.",
                      "My ride preparation", "Items you add will appear here.", string.Empty, "Nothing added yet",
                      "Review my choices", 736f, true, 588f, 158f, 369f);
            ReviewChoices(Panel("ReviewChoices"));
            Checklist(Panel("Diagnostic"), "Pre-ride check", "What would you inspect on this vehicle before riding?",
                      "My pre-ride check", "Components you check will appear here.", "Point a row to remove it.",
                      "Nothing checked yet", "Finish diagnostic selection", 678f, false, 597f, 150f, 360f);
            Comparison(Panel("Comparison", true));
            Explanation(Panel("Explanation", true));
            EndModule("PF_Module01_UI");
        }

        // 06 — nothing is judged yet; spoken line subtitled.
        private void Orientation(Transform panel)
        {
            Transform box = K.Slice(panel, "Panel_Modal_Light", 190f, 148f, 811f, 451f, 2.4f, Color.white, "OrientationPanel").transform;
            Vector2 o = Origin(190f, 148f, 2.4f);

            K.Dot(box, 409f + o.x, 189f + o.y, 5f, UIBuilderKit.Coral, "ModuleDot");
            K.TL(box, "Module 1 · Readiness", K.Caps, 422f + o.x, 189f + o.y, 250f, "ModuleTag");
            TMP_Text title = K.TC(box, "You are in a preparation area", K.H1, 501f + o.x, 231f + o.y, 600f, "Title");
            TMP_Text body = K.Text(box, "No traffic here. You will choose what to take,\nthen check the vehicle. Choose without hints.\nWe’ll review your choices after you submit.",
                                   K.Sub, 230f + o.x, 260f + o.y, 772f + o.x, 346f + o.y, TextAlignmentOptions.Top, 12f, "Body");
            Card(box.gameObject, null, title, body, null);

            _ui.Button(box, "RepeatAudioButton", "Repeat audio", false, 360f + o.x, 365f + o.y, 523f + o.x, 417f + o.y);
            _ui.Button(box, "StartButton", "Start", true, 538f + o.x, 367f + o.y, 642f + o.x, 415f + o.y);
            _ui.AutoLabel(panel, "Subtitle", "“You are in a preparation area. No traffic here.”", K.Subtitle.With(15f),
                          "Debrief_Label", 3f, 14f, 7f, 501f, 465f, true, true);
        }

        /// <summary>Preparation (07/08) and diagnostic (11/12): growing checklist + Back + primary CTA.</summary>
        private void Checklist(Transform panel, string title, string subtitle, string listTitle, string emptySubtitle,
                               string filledSubtitle, string emptyText, string cta, float ctaX0, bool withCounter,
                               float listX, float listY, float listWidth)
        {
            Header(panel, title, subtitle, 51f, 90f, 32f);

            GameObject list = _ui.StackPanel(panel, "ChecklistPanel", "Checklist_Container", 2.4f, listX, listY, listWidth,
                                             new RectOffset(24, 24, 20, 18), 8f);
            _ui.StackText(list.transform, "Title", listTitle, K.H2.With(20f));
            TMP_Text sub = _ui.StackText(list.transform, "Subtitle", emptySubtitle, K.Small);
            _ui.StackDivider(list.transform);

            GameObject empty = EmptyState(list.transform, emptyText);
            RectTransform rows = UIElements.StackList(list.transform, "Rows", 2f);
            RectTransform template = _ui.ChecklistRowTemplate(rows, "Item");
            template.gameObject.SetActive(false);

            TMP_Text footnote = _ui.StackText(list.transform, "Footnote", "A check means added, not correct. Point at a row to remove it.", K.Small);
            footnote.gameObject.SetActive(false);

            TMP_Text counter = null;
            if (withCounter)
            {
                counter = K.TL(panel, "Preparation zone · 0 items", K.Caps, 171f, 489f, 320f, "ZoneCounter");
                counter.gameObject.SetActive(false);
            }

            _ui.Button(panel, "BackButton", "Back", false, 41f, 457f, 139f, 507f);
            _ui.Button(panel, "PrimaryButton", cta, true, ctaX0, 457f, 957f, 507f);

            ChecklistView view = panel.gameObject.AddComponent<ChecklistView>();
            UIWire.Ref(view, "_rowTemplate", template);
            UIWire.Ref(view, "_emptyState", empty);
            UIWire.Ref(view, "_footnote", footnote.gameObject);
            UIWire.Ref(view, "_subtitle", sub);
            UIWire.Str(view, "_emptySubtitle", emptySubtitle);
            UIWire.Str(view, "_filledSubtitle", filledSubtitle);
            UIWire.Ref(view, "_counter", counter);
        }

        private GameObject EmptyState(Transform parent, string text)
        {
            GameObject empty = UIBuilderKit.NewUi("EmptyState", parent);
            UnityEngine.UI.HorizontalLayoutGroup layout = empty.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 6, 6);

            GameObject icon = UIBuilderKit.NewUi("Icon", empty.transform);
            UnityEngine.UI.Image dashed = icon.AddComponent<UnityEngine.UI.Image>();
            dashed.sprite = K.S("Status_Attention");
            dashed.preserveAspect = true;
            dashed.color = new Color(1f, 1f, 1f, 0.35f);
            dashed.raycastTarget = false;
            UnityEngine.UI.LayoutElement iconLayout = icon.AddComponent<UnityEngine.UI.LayoutElement>();
            iconLayout.preferredWidth = iconLayout.preferredHeight = 32f;

            TMP_Text label = K.AddText(UIBuilderKit.NewUi("Text", empty.transform), text,
                                       K.Body.With(16f, UIBuilderKit.Hex("#4A6480", 0.8f)), TextAlignmentOptions.Left);
            label.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            return empty;
        }

        // 09 — recap, lock-in notice, two-step confirm. No evaluation.
        private void ReviewChoices(Transform panel)
        {
            Transform modal = K.Slice(panel, "Panel_Modal_Light", 154f, 59f, 848f, 497f, 2.2f, Color.white, "Modal").transform;
            Vector2 o = Origin(154f, 59f, 2.2f);

            K.TL(modal, "Review my choices", K.H1.With(30f), 191f + o.x, 111f + o.y, 600f, "Title");
            K.TL(modal, "These are the items you would take. You can still change them.", K.Sub.With(17f), 191f + o.x, 149f + o.y, 620f, "Subtitle");

            Transform box = K.Slice(modal, "Card_Default", 191f + o.x, 182f + o.y, 810f + o.x, 321f + o.y, 3.2f, Color.white, "ListBox").transform;
            float boxPad = UIBuilderKit.PadFor(3.2f);
            RectTransform rows = UIElements.StackList(box, "Rows", 0f);
            UIBuilderKit.Place(rows, boxPad + 14f, boxPad + 12f, 619f + boxPad - 14f, 139f + boxPad - 8f);

            RectTransform template = _ui.ChecklistRowTemplate(rows, "Item");
            template.gameObject.SetActive(false);
            // Sample rows for edit-time preview; ChecklistView removes them on Awake.
            foreach (string item in new[] { "Helmet · undamaged", "Closed-toe shoes", "Fitted jacket" })
            {
                GameObject sample = Object.Instantiate(template.gameObject, rows);
                sample.name = "Sample · " + item;
                sample.SetActive(true);
                UIBuilderKit.SetText(sample, "Label", item);
                sample.transform.Find("Tag").gameObject.SetActive(false);
            }

            K.Slice(modal, "Checklist_Row_Selected", 191f + o.x, 342f + o.y, 810f + o.x, 390f + o.y, 2.4f, Color.white, "LockNotice");
            K.TL(modal, "Once you continue, your selection is locked for this attempt.", K.Body.With(15f), 214f + o.x, 366f + o.y, 580f, "LockNoticeText");

            _ui.Button(modal, "KeepChoosingButton", "Keep choosing", false, 415f + o.x, 411f + o.y, 591f + o.x, 463f + o.y);
            _ui.Button(modal, "ConfirmButton", "Confirm selection", true, 607f + o.x, 411f + o.y, 810f + o.x, 463f + o.y);

            ChecklistView view = panel.gameObject.AddComponent<ChecklistView>();
            UIWire.Ref(view, "_rowTemplate", template);
            UIWire.Bool(view, "_removable", false);
        }

        // 10 / 13 — the learner's list beside the reference, four-state key, "Explain each one".
        private void Comparison(Transform panel)
        {
            CardView header = Header(panel, "What you took", "Your selection beside the reference for this ride.", 45f, 79f, 30f, true);

            List<ComparisonEntry> left = new List<ComparisonEntry>
            {
                new ComparisonEntry(ItemStatus.Selected, "Helmet · undamaged"),
                new ComparisonEntry(ItemStatus.Selected, "Closed-toe shoes"),
                new ComparisonEntry(ItemStatus.Selected, "Reflective layer"),
                new ComparisonEntry(ItemStatus.NotSuited, "Bag hung on handlebar", "Unsafe load — shifts weight and can foul the steering")
            };
            List<ComparisonEntry> right = new List<ComparisonEntry>
            {
                new ComparisonEntry(ItemStatus.Selected, "Helmet · undamaged"),
                new ComparisonEntry(ItemStatus.Selected, "Closed-toe shoes"),
                new ComparisonEntry(ItemStatus.CoreOmitted, "Fitted clothing", "Core item — you did not take it"),
                new ComparisonEntry(ItemStatus.ConditionDependent, "Reflective layer", "Depends on light and weather")
            };

            RectTransform leftRows, rightRows;
            TMP_Text leftHeading = Column(panel, "LeftColumn", 57f, "Your choices", out leftRows);
            TMP_Text rightHeading = Column(panel, "RightColumn", 553f, "Reference · this ride", out rightRows);

            RectTransform template = _ui.StatusRowTemplate(leftRows);
            template.gameObject.SetActive(false);
            StatusGlyphs glyphs = new StatusGlyphs { Sprites = _ui.StatusSprites() };
            BakeSamples(template, leftRows, left, glyphs);
            BakeSamples(template, rightRows, right, glyphs);

            Transform legend = K.Slice(panel, "Checklist_Row_Selected", 176f, 421f, 827f, 466f, 2.4f, Color.white, "Legend").transform;
            float pad = UIBuilderKit.PadFor(2.4f);
            LegendItem(legend, "Status_Independent", 27f + pad, "Selected");
            LegendItem(legend, "Status_Supported", 130f + pad, "Core item omitted");
            LegendItem(legend, "Status_Attention", 292f + pad, "Condition-dependent");
            LegendItem(legend, "Status_Retry", 476f + pad, "Not suited to this ride");

            _ui.Button(panel, "ExplainButton", "Explain each one", true, 397f, 473f, 604f, 523f);

            ComparisonView view = panel.gameObject.AddComponent<ComparisonView>();
            UIWire.Refs(view, "_glyphs.Sprites", _ui.StatusSprites());
            UIWire.Ref(view, "_rowTemplate", template);
            UIWire.Ref(view, "_title", header.transform.Find("Title").GetComponent<TMP_Text>());
            UIWire.Ref(view, "_subtitle", header.transform.Find("Body").GetComponent<TMP_Text>());
            UIWire.Ref(view, "_leftHeading", leftHeading);
            UIWire.Ref(view, "_leftRows", leftRows);
            UIWire.Ref(view, "_rightHeading", rightHeading);
            UIWire.Ref(view, "_rightRows", rightRows);
            UIWire.Entries(view, "_left", left);
            UIWire.Entries(view, "_right", right);
        }

        private TMP_Text Column(Transform panel, string name, float x0, string heading, out RectTransform rows)
        {
            GameObject column = _ui.StackPanel(panel, name, "Card_Default", 2.8f, x0, 123f, 392f, new RectOffset(23, 23, 22, 20), 8f);
            TMP_Text title = _ui.StackText(column.transform, "Heading", heading, K.Caps);
            _ui.StackDivider(column.transform);
            rows = UIElements.StackList(column.transform, "Rows", 2f);
            return title;
        }

        /// <summary>Edit-time preview rows; ComparisonView rebuilds them from data on Awake.</summary>
        private static void BakeSamples(RectTransform template, RectTransform rows, List<ComparisonEntry> entries, StatusGlyphs glyphs)
        {
            foreach (ComparisonEntry entry in entries)
            {
                GameObject row = Object.Instantiate(template.gameObject, rows);
                row.name = "Sample · " + entry.Title;
                row.SetActive(true);
                glyphs.Bind(row.transform, entry);
            }
        }

        private void LegendItem(Transform legend, string glyph, float x, string label)
        {
            float cy = 23f + UIBuilderKit.PadFor(2.4f) - 1f;
            K.Icon(legend, glyph, x, cy, 18f);
            K.TL(legend, label, K.LabelBold.With(14f), x + 16f, cy, 170f, "Legend · " + label);
        }

        // 10B / 13B — video card, item card, explanation, subtitle.
        private void Explanation(Transform panel)
        {
            GameObject badge = _ui.DarkLabel(panel, "Badge", "Explaining · item 3 of 5", 57f, 88f, false, K.CapsOnDark.With(11f));

            Transform card = K.Slice(panel, "Card_Default", 57f, 125f, 578f, 484f, 2.8f, Color.white, "VideoCard").transform;
            Vector2 o = Origin(57f, 125f, 2.8f);
            RectTransform surfaceRect = UIBuilderKit.Rect(card, "VideoSurface", 59f + o.x, 127f + o.y, 576f + o.x, 378f + o.y);
            UnityEngine.UI.RawImage surface = surfaceRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            surface.color = UIBuilderKit.Hex("#123D6B");
            surface.raycastTarget = false;
            K.Slice(card, "Video_Frame", 57f + o.x, 125f + o.y, 578f + o.x, 378f + o.y, 2.8f, Color.white, "VideoFrame");
            GameObject playing = _ui.DarkLabel(card, "PlayingTag", "Playing", 484f + o.x, 140f + o.y, false, K.CapsOnDark.With(11f));
            ProgressView videoProgress = _ui.ProgressBar(card, "VideoProgress", 57f + o.x, 578f + o.x, 376f + o.y, 4f, 0.46f,
                                                         UIBuilderKit.Hex("#35557A"));
            TMP_Text caption = K.TL(card, "Loose clothing near the drive train.", K.Body, 76f + o.x, 405f + o.y, 480f, "Caption");
            _ui.Button(card, "PauseButton", "Pause", false, 76f + o.x, 430f + o.y, 155f + o.x, 468f + o.y, 14f);
            _ui.Button(card, "ReplayButton", "Replay", false, 165f + o.x, 430f + o.y, 248f + o.x, 468f + o.y, 14f);
            _ui.Button(card, "ContinueButton", "Continue", true, 455f + o.x, 430f + o.y, 560f + o.x, 468f + o.y, 15f);

            Transform itemCard = K.Slice(panel, "Card_Default", 605f, 147f, 945f, 321f, 2.8f, Color.white, "ItemCard").transform;
            Vector2 i = Origin(605f, 147f, 2.8f);
            TMP_Text heading = K.TL(itemCard, "The item being explained", K.Caps, 630f + i.x, 180f + i.y, 300f, "Heading");
            K.Line(itemCard, 630f + i.x, 920f + i.x, 204f + i.y);
            RectTransform itemRow = _ui.StatusRowTemplate(itemCard);
            itemRow.name = "Item";
            UIBuilderKit.Place(itemRow, 626f + i.x, 215f + i.y, 920f + i.x, 262f + i.y);
            StatusGlyphs glyphs = new StatusGlyphs { Sprites = _ui.StatusSprites() };
            glyphs.Bind(itemRow, new ComparisonEntry(ItemStatus.CoreOmitted, "Fitted clothing", "Core item — you did not take it"));
            TMP_Text counter = K.TL(itemCard, "Item 3 of 5", K.Small, 630f + i.x, 289f + i.y, 80f, "Counter");
            ProgressView itemProgress = _ui.ProgressBar(itemCard, "ItemProgress", 706f + i.x, 920f + i.x, 289f + i.y, 4f, 0.6f, UIBuilderKit.Track);

            Transform box = K.Slice(panel, "Panel_Modal_Dark", 605f, 324f, 945f, 433f, 2.4f, Color.white, "Explanation").transform;
            float bp = UIBuilderKit.PadFor(2.4f);
            TMP_Text explanation = K.Text(box, "Loose clothing can catch on the drive train. Fitted clothing keeps that from deciding the ride for you.",
                                          K.BodyOnDark, 21f + bp, 14f + bp, 323f + bp, 100f + bp, TextAlignmentOptions.Left, 10f, "Text");

            _ui.Subtitle(panel, "Subtitles on · “Fitted clothing keeps the drive train clear.”", 501f, 491f);

            ExplanationView view = panel.gameObject.AddComponent<ExplanationView>();
            UIWire.Refs(view, "_glyphs.Sprites", _ui.StatusSprites());
            UIWire.Ref(view, "_badge", UIBuilderKit.Find<TMP_Text>(badge, "Text"));
            UIWire.Ref(view, "_heading", heading);
            UIWire.Ref(view, "_caption", caption);
            UIWire.Ref(view, "_playingTag", playing);
            UIWire.Ref(view, "_videoProgress", videoProgress);
            UIWire.Ref(view, "_videoSurface", surface);
            UIWire.Ref(view, "_itemRow", itemRow);
            UIWire.Ref(view, "_itemCounter", counter);
            UIWire.Ref(view, "_itemProgress", itemProgress);
            UIWire.Ref(view, "_explanation", explanation);
        }
    }
}
