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

        public int Count => _rows.Count;

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

        public bool Contains(string id) => IndexOf(id) >= 0;

        public void AddItem(string id, string label)
        {
            if (string.IsNullOrEmpty(id) || Contains(id) || _rowTemplate == null)
                return;

            GameObject row = Instantiate(_rowTemplate.gameObject, _rowTemplate.parent);
            row.name = "Row · " + label;
            row.SetActive(true);
            SetText(row, "Label", label);
            SetText(row, "Tag", _removable ? _rowTag : string.Empty);

            UnityEngine.UI.Button button = row.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.interactable = _removable;
                button.onClick.AddListener(() => RemoveItem(id));
            }

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
            if (_emptyState != null)
                _emptyState.SetActive(!filled);
            if (_footnote != null)
                _footnote.SetActive(filled);
            if (_subtitle != null)
            {
                string text = filled ? _filledSubtitle : _emptySubtitle;
                _subtitle.text = text;
                _subtitle.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
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
