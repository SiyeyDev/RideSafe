using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace RideSafe.UI
{
    /// <summary>
    /// A list of chosen items built from a hidden row template: "My ride preparation" (07/08),
    /// "My pre-ride check" (11/12) and the read-only recap in "Review my choices" (09).
    /// <para>
    /// A check means ADDED, never "correct", and the expected count is never shown. When rows
    /// are removable, pointing at one and pulling the trigger removes it.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ChecklistView : MonoBehaviour
    {
        [Tooltip("Inactive child used as the row blueprint. Needs children 'Label' and 'Tag' and a Button.")]
        [SerializeField] private RectTransform _rowTemplate;
        [SerializeField] private bool _removable = true;
        [SerializeField] private string _rowTag = "Added";

        [Header("Optional state")]
        [SerializeField] private GameObject _emptyState;
        [SerializeField] private GameObject _footnote;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private string _emptySubtitle;
        [SerializeField] private string _filledSubtitle;
        [SerializeField] private TMP_Text _counter;
        [SerializeField] private string _counterFormat = "Preparation zone · {0} items";

        public UnityEvent<string> onItemRemoved = new UnityEvent<string>();

        private readonly List<KeyValuePair<string, GameObject>> _rows = new List<KeyValuePair<string, GameObject>>();

        // Plegable (PO, 2026-10-28: «un tipo de dropdown donde contenga los elementos y no se haga
        // tan largo»). Plegado solo queda el título con cuántos hay; el resto se abre al pulsarlo.
        private GameObject[] _foldParts = new GameObject[0];
        private TMP_Text _foldLabel;
        private bool _collapsible;

        public int Count => _rows.Count;

        /// <summary>Si la lista está plegada: solo se ve el título.</summary>
        public bool Collapsed { get; private set; }

        /// <summary>
        /// Convierte el título en el botón de un desplegable. Al plegar se esconden las filas, el
        /// estado vacío, la nota y el subtítulo, que ya gobierna esta vista, y además
        /// <paramref name="parts"/> (el separador, por ejemplo).
        /// </summary>
        public void MakeCollapsible(TMP_Text header, IEnumerable<GameObject> parts, bool collapsed = true)
        {
            var fold = new List<GameObject>(parts);
            if (_rowTemplate != null && _rowTemplate.parent != transform && !fold.Contains(_rowTemplate.parent.gameObject))
                fold.Add(_rowTemplate.parent.gameObject);
            _foldParts = fold.ToArray();
            _collapsible = true;

            Transform existing = header.transform.Find("Fold");
            _foldLabel = existing != null ? existing.GetComponent<TMP_Text>() : CreateFoldLabel(header);

            header.raycastTarget = true;
            UnityEngine.UI.Button button = header.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
            {
                button = header.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.targetGraphic = header;
                button.transition = UnityEngine.UI.Selectable.Transition.None;
                button.onClick.AddListener(Toggle);
            }

            SetCollapsed(collapsed);
        }

        public void Toggle() => SetCollapsed(!Collapsed);

        public void SetCollapsed(bool collapsed)
        {
            Collapsed = _collapsible && collapsed;
            Refresh();
        }

        /// <summary>La cuenta y la flecha, a la derecha del título y con su misma letra.</summary>
        private static TMP_Text CreateFoldLabel(TMP_Text header)
        {
            GameObject go = new GameObject("Fold", typeof(RectTransform));
            go.transform.SetParent(header.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.font = header.font;
            label.fontSize = header.fontSize;
            label.color = header.color;
            label.alignment = TextAlignmentOptions.Right;
            label.raycastTarget = false;
            return label;
        }

        private void Awake()
        {
            if (_rowTemplate != null)
            {
                _rowTemplate.gameObject.SetActive(false);
                // Sample rows baked into the prefab for edit-time preview never reach runtime.
                Transform container = _rowTemplate.parent;
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    if (container.GetChild(i) != _rowTemplate)
                        Destroy(container.GetChild(i).gameObject);
                }
            }
            Refresh();
        }

        /// <summary>
        /// Sizes every row to its name now (<see cref="ChecklistRowFit"/>); each row also refits
        /// itself when its width or name changes.
        /// </summary>
        public void FitRows()
        {
            foreach (KeyValuePair<string, GameObject> row in _rows)
                if (row.Value.TryGetComponent(out ChecklistRowFit fit))
                    fit.Fit();
        }

        public void ConfigureLabels(string empty, string filled, string tag)
        { _emptySubtitle = empty; _filledSubtitle = filled; _rowTag = tag; Refresh(); }

        public bool Contains(string id) => IndexOf(id) >= 0;

        public void AddItem(string id, string label)
        {
            if (string.IsNullOrEmpty(id) || Contains(id) || _rowTemplate == null)
                return;

            GameObject row = Instantiate(_rowTemplate.gameObject, _rowTemplate.parent);
            row.name = "Row · " + label;
            row.SetActive(true);
            SetText(row, "Label", label);
            SetText(row, "Tag", _removable ? UIText.Resolve(_rowTag) : string.Empty);

            UnityEngine.UI.Button button = row.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.interactable = _removable;
                button.onClick.AddListener(() => RemoveItem(id));
            }

            // On the row and not on this view: «Mi preparación» lifts its rows out to the head-following
            // sidebar and leaves this view inactive, where its own LateUpdate never runs.
            if (!row.TryGetComponent(out ChecklistRowFit rowFit))
                rowFit = row.AddComponent<ChecklistRowFit>();
            rowFit.Fit();

            _rows.Add(new KeyValuePair<string, GameObject>(id, row));
            Refresh();
        }

        public void RemoveItem(string id)
        {
            int index = IndexOf(id);
            if (index < 0)
                return;
            Destroy(_rows[index].Value);
            _rows.RemoveAt(index);
            Refresh();
            onItemRemoved.Invoke(id);
        }

        /// <summary>Replaces the whole list (e.g. the recap shown in Review my choices).</summary>
        public void SetItems(IEnumerable<KeyValuePair<string, string>> items)
        {
            Clear();
            foreach (KeyValuePair<string, string> item in items)
                AddItem(item.Key, item.Value);
        }

        public void Clear()
        {
            foreach (KeyValuePair<string, GameObject> row in _rows)
                Destroy(row.Value);
            _rows.Clear();
            Refresh();
        }

        private int IndexOf(string id)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Key == id)
                    return i;
            }
            return -1;
        }

        private void Refresh()
        {
            bool filled = _rows.Count > 0;
            bool open = !Collapsed;
            if (_emptyState != null)
                _emptyState.SetActive(open && !filled);
            if (_footnote != null)
                _footnote.SetActive(open && filled);
            if (_subtitle != null)
            {
                string text = filled ? _filledSubtitle : _emptySubtitle;
                _subtitle.text = UIText.Resolve(text);
                _subtitle.gameObject.SetActive(open && !string.IsNullOrEmpty(text));
            }
            foreach (GameObject part in _foldParts)
                if (part != null)
                    part.SetActive(open);
            if (_foldLabel != null)
                _foldLabel.text = $"({_rows.Count})  {(Collapsed ? "↓" : "↑")}";
            if (_counter != null)
            {
                _counter.text = string.Format(_counterFormat, _rows.Count);
                _counter.gameObject.SetActive(filled);
            }
        }

        private static void SetText(GameObject row, string child, string text)
        {
            Transform target = row.transform.Find(child);
            if (target == null)
                return;
            TMP_Text tmp = target.GetComponent<TMP_Text>();
            if (tmp != null)
                tmp.text = text;
            target.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }
}

