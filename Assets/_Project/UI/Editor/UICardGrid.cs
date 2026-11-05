using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// «Revisa tus elecciones» y «Evaluación de tus elecciones» como cuadrícula de tarjetas (PO,
    /// 2026-11-04, con dos capturas de referencia): el render del EPP como protagonista, el nombre
    /// debajo y, solo al evaluar, el icono del estado arriba a la derecha. Solo el título: los
    /// subtítulos se van.
    /// <para>
    /// Construye la tarjeta (<c>PF_EppCard</c>) y, en los dos paneles, el contenido con su
    /// <c>GridLayoutGroup</c> dentro de la ventana de scroll que ya tenían (<see cref="UIScrollList"/>):
    /// en la revisión pasa a ser lo único que se desplaza; en la evaluación comparte la ventana con
    /// las filas, que siguen para el vehículo y el reporte final. Corre después de
    /// <see cref="UIScrollList.ApplyToModule01"/>, que vuelve a apuntar la ventana a las filas.
    /// Idempotente: fija valores absolutos y reutiliza lo que ya creó.
    /// </para>
    /// </summary>
    internal static class UICardGrid
    {
        public const string CardPath = UIModules.Folder + "/PF_EppCard.prefab";
        public const string CardsName = "Cards";
        public const string ScorePillName = "ScorePill";

        /// <summary>Tres por fila, como la referencia.</summary>
        public const int Columns = 3;
        public const float CardHeight = 150f;
        /// <summary>Entre celdas; el borde transparente del sprite de la tarjeta pone el resto del aire.</summary>
        public const float Spacing = 4f;
        /// <summary>Dos filas a la vista; la tercera ya se desplaza.</summary>
        public const float VisibleHeight = 2f * CardHeight + Spacing;

        private const string CardSprite = "Card_Default";
        private const float CardMultiplier = 2.8f;
        private const string PillSprite = "Status_Pill_Default";
        private const float PillHeight = 40f;
        private const float PillWidth = 230f;
        private static readonly Color PillColor = UIBuilderKit.Hex("#DCEAF8");
        private static readonly Color CardColor = UIBuilderKit.Hex("#EEF2F7");

        [MenuItem("RideSafe/UI/Tarjetas de EPP (Revisa y Evaluación)")]
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
            Debug.Log("[RideSafe.UI] EPP cards: " + CardPath + " and the two panels of " + UIComposition.Module01Path);
        }

        public static void ApplyToModule01(GameObject root, UIBuilderKit kit)
        {
            ItemCard card = BuildCard(kit);
            Review(root.transform.Find("ReviewChoices"), card);
            Evaluation(root.transform.Find("Comparison"), card, kit);
        }

        #region Card

        /// <summary>
        /// La tarjeta: caja blanca, render arriba, nombre en dos líneas como mucho y el icono del
        /// estado en la esquina de arriba a la derecha. Todo anclado a los bordes, así que vale
        /// para el tamaño de celda que le dé cada cuadrícula.
        /// </summary>
        public static ItemCard BuildCard(UIBuilderKit kit)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CardPath);
            GameObject root = existing != null ? PrefabUtility.LoadPrefabContents(CardPath) : UIBuilderKit.NewUi("PF_EppCard", null);
            try
            {
                RectTransform rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(184f, CardHeight);
                // Un gris muy claro y no blanco, como la referencia: lo blanco —el casco, las gafas
                // transparentes, el pantalón— se perdía sobre la tarjeta blanca.
                kit.AddSliced(root, CardSprite, CardMultiplier, CardColor);
                float pad = UIBuilderKit.PadFor(CardMultiplier);

                Image picture = Ensure<Image>(Child(root.transform, "Picture").gameObject);
                Inset((RectTransform)picture.transform, pad + 10f, pad + 8f, pad + 10f, pad + 44f);
                picture.preserveAspect = true;
                picture.raycastTarget = false;

                Image placeholder = Ensure<Image>(Child(root.transform, "Placeholder").gameObject);
                Inset((RectTransform)placeholder.transform, pad + 10f, pad + 8f, pad + 10f, pad + 44f);
                placeholder.sprite = kit.S("Status_Attention");
                placeholder.preserveAspect = true;
                placeholder.color = new Color(1f, 1f, 1f, 0.3f);
                placeholder.raycastTarget = false;
                placeholder.gameObject.SetActive(false);

                RectTransform nameRect = Child(root.transform, "Name");
                nameRect.anchorMin = Vector2.zero;
                nameRect.anchorMax = new Vector2(1f, 0f);
                nameRect.pivot = new Vector2(0.5f, 0f);
                nameRect.offsetMin = new Vector2(pad + 6f, pad + 4f);
                nameRect.offsetMax = new Vector2(-(pad + 6f), pad + 4f + 40f);
                TMP_Text label = kit.AddText(nameRect.gameObject, "Casco abrochado", kit.LabelBold.With(15f), TextAlignmentOptions.Center);
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 15f;
                label.maxVisibleLines = 2;
                label.overflowMode = TextOverflowModes.Ellipsis;

                // El icono a 28 visibles, el tamaño de las filas de antes (UIElements.StatusIconSize).
                RectTransform statusRect = Child(root.transform, "Status");
                float icon = UIElements.StatusIconSize * 256f / 176f;
                statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = Vector2.one;
                statusRect.sizeDelta = new Vector2(icon, icon);
                statusRect.anchoredPosition = new Vector2(-(pad - 2f), -(pad - 2f));
                Image status = Ensure<Image>(statusRect.gameObject);
                status.sprite = kit.S(UIStatusIcons.MonoName(UIComposition.StatusSemaphore[0].Glyph));
                status.color = UIComposition.StatusSemaphore[0].Tint;
                status.preserveAspect = true;
                status.raycastTarget = false;

                ItemCard card = Ensure<ItemCard>(root);
                UIWire.Ref(card, "_picture", picture);
                UIWire.Ref(card, "_placeholder", placeholder.gameObject);
                UIWire.Ref(card, "_name", label);
                UIWire.Ref(card, "_status", status);

                PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            }
            finally
            {
                if (existing != null)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<ItemCard>(CardPath);
        }

        #endregion

        #region Panels

        /// <summary>
        /// «Revisa tus elecciones»: título, tarjetas sin icono y los dos botones de siempre. Las
        /// filas y el subtítulo se apagan, y lo de abajo sube a ocupar el sitio del subtítulo.
        /// </summary>
        private static void Review(Transform panel, ItemCard card)
        {
            RectTransform modal = Find(panel, "Modal");
            RectTransform box = Find(modal, "ListBox");
            RectTransform viewport = Find(box, UIScrollList.ViewportName);
            RectTransform rows = Find(viewport, "Rows");
            RectTransform cards = Grid(viewport, rows, viewport.sizeDelta.x - (UIScrollList.BarWidth + UIScrollList.BarGap));
            rows.gameObject.SetActive(false);
            cards.gameObject.SetActive(true);
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = cards;
            EditorUtility.SetDirty(scroll);
            // Sin caja alrededor: las tarjetas ya son cajas y van sobre el panel, como en la referencia.
            box.GetComponent<Image>().enabled = false;

            // El subtítulo se va (PO: «tener solo el título»): la lista y los botones suben a su sitio
            // y el panel se acorta lo mismo, sin dejar hueco. Una sola vez.
            RectTransform subtitle = Find(modal, "Subtitle");
            RectTransform keep = Find(modal, "KeepChoosingButton"), confirm = Find(modal, "ConfirmButton");
            if (subtitle.gameObject.activeSelf)
            {
                float lift = box.anchoredPosition.y - subtitle.anchoredPosition.y;
                foreach (RectTransform moved in new[] { box, keep, confirm })
                {
                    moved.anchoredPosition -= new Vector2(0f, lift);
                    EditorUtility.SetDirty(moved);
                }
                modal.sizeDelta += new Vector2(0f, lift);
                EditorUtility.SetDirty(modal);
                subtitle.gameObject.SetActive(false);
            }

            ChecklistView recap = panel.GetComponent<ChecklistView>();
            if (recap != null)
                Object.DestroyImmediate(recap, true);
            CardGrid grid = Ensure<CardGrid>(panel.gameObject);
            grid.Configure(card, cards);
            EditorUtility.SetDirty(grid);
            Samples(cards, card);

            // La ventana crece con las tarjetas hasta dos filas; la tercera ya se desplaza.
            GrowWithContent grow = modal.GetComponent<GrowWithContent>();
            grow.Configure(viewport, box, new[] { modal }, new[] { keep, confirm }, modal, cards,
                           Mathf.Max(0f, VisibleHeight - viewport.sizeDelta.y));
            EditorUtility.SetDirty(grow);
        }

        /// <summary>
        /// «Evaluación de tus elecciones»: las tarjetas comparten la ventana de las filas, y encima
        /// va la píldora del puntaje. <see cref="ComparisonView.SetCards"/> enciende lo uno y
        /// <see cref="ComparisonView.SetSingleList"/> lo otro.
        /// </summary>
        private static void Evaluation(Transform panel, ItemCard card, UIBuilderKit kit)
        {
            RectTransform column = Find(panel, "LeftColumn");
            RectTransform viewport = Find(column, UIScrollList.ViewportName);
            RectTransform rows = Find(viewport, "Rows");
            VerticalLayoutGroup stack = column.GetComponent<VerticalLayoutGroup>();
            float inner = column.sizeDelta.x - (stack != null ? stack.padding.horizontal : 0);
            RectTransform cards = Grid(viewport, rows, inner - (UIScrollList.BarWidth + UIScrollList.BarGap));
            cards.gameObject.SetActive(false);
            Samples(cards, card);

            TMP_Text score = ScorePill(column, kit);
            CardGrid grid = Ensure<CardGrid>(panel.gameObject);
            grid.Configure(card, cards);
            EditorUtility.SetDirty(grid);

            ComparisonView view = panel.GetComponent<ComparisonView>();
            UIWire.Ref(view, "_cards", grid);
            UIWire.Ref(view, "_score", score);
            UIWire.Refs(view, "_cardsOnly", new[] { score.transform.parent.parent.gameObject });
            UIWire.Refs(view, "_rowsOnly", new[] { column.Find("Divider").gameObject });
            UIWire.Ref(view, "_columnBackground", column.Find("Background").GetComponent<Image>());
            UIWire.Float(view, "_maxCardsHeight", VisibleHeight);
            EditorUtility.SetDirty(view);
        }

        /// <summary>«Puntaje: 85», centrado bajo el título, en una píldora azul claro como la referencia.</summary>
        private static TMP_Text ScorePill(RectTransform column, UIBuilderKit kit)
        {
            RectTransform row = Child(column, ScorePillName);
            row.SetSiblingIndex(0);
            LayoutElement size = Ensure<LayoutElement>(row.gameObject);
            size.minHeight = size.preferredHeight = PillHeight + 8f;
            size.flexibleWidth = 1f;

            RectTransform pill = Child(row, "Pill");
            float multiplier = UIBuilderKit.PillMultiplier(PillSprite, PillHeight);
            float pad = UIBuilderKit.PadFor(multiplier);
            pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(0.5f, 0.5f);
            pill.anchoredPosition = Vector2.zero;
            pill.sizeDelta = new Vector2(PillWidth + 2f * pad, PillHeight + 2f * pad);
            kit.AddSliced(pill.gameObject, PillSprite, multiplier, PillColor);

            RectTransform textRect = Child(pill, "Text");
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(pad, pad);
            textRect.offsetMax = new Vector2(-pad, -pad);
            TMP_Text text = kit.AddText(textRect.gameObject, "Score: 85", kit.H2.With(20f), TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            row.gameObject.SetActive(false);
            return text;
        }

        /// <summary>
        /// El contenido de la cuadrícula, hermano de las filas en la ventana y colgado como ellas
        /// del borde de arriba: celdas del ancho que cabe en tres columnas.
        /// </summary>
        private static RectTransform Grid(RectTransform viewport, RectTransform rows, float width)
        {
            RectTransform cards = Child(viewport, CardsName);
            cards.anchorMin = new Vector2(0f, 1f);
            cards.anchorMax = Vector2.one;
            cards.pivot = new Vector2(0.5f, 1f);
            cards.offsetMin = new Vector2(0f, cards.offsetMin.y);
            cards.offsetMax = new Vector2(-(UIScrollList.BarWidth + UIScrollList.BarGap), 0f);
            cards.anchoredPosition = new Vector2(cards.anchoredPosition.x, 0f);
            cards.SetSiblingIndex(rows.GetSiblingIndex() + 1);

            GridLayoutGroup grid = Ensure<GridLayoutGroup>(cards.gameObject);
            float cell = Mathf.Floor((width - (Columns - 1) * Spacing) / Columns);
            grid.cellSize = new Vector2(cell, CardHeight);
            grid.spacing = new Vector2(Spacing, Spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.padding = new RectOffset(0, 0, 0, 0);
            ContentSizeFitter fit = Ensure<ContentSizeFitter>(cards.gameObject);
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            EditorUtility.SetDirty(grid);
            return cards;
        }

        /// <summary>Tres tarjetas para ver la maquetación al editar; <see cref="CardGrid"/> las quita al arrancar.</summary>
        private static void Samples(RectTransform cards, ItemCard card)
        {
            for (int i = cards.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(cards.GetChild(i).gameObject);
            foreach (string name in new[] { "Casco abrochado", "Calzado cerrado", "Poncho-tarp extragrande" })
            {
                GameObject sample = (GameObject)PrefabUtility.InstantiatePrefab(card.gameObject, cards);
                sample.name = "Sample · " + name;
                sample.GetComponent<ItemCard>().Bind(new ComparisonEntry(ItemStatus.Selected, name), null);
            }
        }

        #endregion

        private static RectTransform Find(Transform root, string path)
        {
            RectTransform found = root.Find(path) as RectTransform;
            if (found == null)
                throw new System.InvalidOperationException(root.name + " has no " + path);
            return found;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            return existing != null ? (RectTransform)existing : (RectTransform)UIBuilderKit.NewUi(name, parent).transform;
        }

        private static void Inset(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        // Not «GetComponent() ?? AddComponent()»: in the editor a missing component comes back as a fake null.
        private static T Ensure<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
