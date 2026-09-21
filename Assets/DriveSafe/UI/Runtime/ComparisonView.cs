using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Post-submission four-state key. State is carried by shape, colour AND text, never
    /// colour alone.
    /// </summary>
    public enum ItemStatus
    {
        Selected = 0,
        CoreOmitted = 1,
        ConditionDependent = 2,
        NotSuited = 3
    }

    [Serializable]
    public struct ComparisonEntry
    {
        public ItemStatus Status;
        public string Title;
        public string Detail;

        public ComparisonEntry(ItemStatus status, string title, string detail = null)
        {
            Status = status;
            Title = title;
            Detail = detail;
        }
    }

    /// <summary>Glyph per status, shared by the comparison and the explanation panels.</summary>
    [Serializable]
    public class StatusGlyphs
    {
        [Tooltip("In ItemStatus order: Selected, CoreOmitted, ConditionDependent, NotSuited.")]
        public Sprite[] Sprites = new Sprite[4];

        public Sprite For(ItemStatus status)
        {
            int index = (int)status;
            return Sprites != null && index < Sprites.Length ? Sprites[index] : null;
        }

        /// <summary>Fills a status row (children 'Icon', 'Title', 'Detail').</summary>
        public void Bind(Transform row, ComparisonEntry entry)
        {
            UnityEngine.UI.Image icon = FindComponent<UnityEngine.UI.Image>(row, "Icon");
            if (icon != null && For(entry.Status) != null)
                icon.sprite = For(entry.Status);

            TMP_Text title = FindComponent<TMP_Text>(row, "Text/Title");
            if (title != null)
                title.text = entry.Title;

            TMP_Text detail = FindComponent<TMP_Text>(row, "Text/Detail");
            if (detail != null)
            {
                detail.text = entry.Detail ?? string.Empty;
                detail.gameObject.SetActive(!string.IsNullOrEmpty(entry.Detail));
            }
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            Transform child = root.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }
    }

    /// <summary>
    /// Review state A (10 / 13): the learner's list beside the reference. One panel serves
    /// both the safety-element review and the diagnostic review; content comes from data.
    /// </summary>
    [DisallowMultipleComponent]
    public class ComparisonView : MonoBehaviour
    {
        [SerializeField] private StatusGlyphs _glyphs = new StatusGlyphs();
        [Tooltip("Inactive status row blueprint (children 'Icon', 'Text/Title', 'Text/Detail').")]
        [SerializeField] private RectTransform _rowTemplate;

        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private TMP_Text _leftHeading;
        [SerializeField] private RectTransform _leftRows;
        [SerializeField] private TMP_Text _rightHeading;
        [SerializeField] private RectTransform _rightRows;

        [Header("Initial content")]
        [SerializeField] private List<ComparisonEntry> _left = new List<ComparisonEntry>();
        [SerializeField] private List<ComparisonEntry> _right = new List<ComparisonEntry>();

        private void Awake()
        {
            if (_rowTemplate != null)
                _rowTemplate.gameObject.SetActive(false);
            Fill(_leftRows, _left);
            Fill(_rightRows, _right);
        }

        public void SetContent(string title, string subtitle,
                               string leftHeading, IList<ComparisonEntry> left,
                               string rightHeading, IList<ComparisonEntry> right)
        {
            if (_title != null) _title.text = title;
            if (_subtitle != null) _subtitle.text = subtitle;
            if (_leftHeading != null) _leftHeading.text = leftHeading;
            if (_rightHeading != null) _rightHeading.text = rightHeading;
            Fill(_leftRows, left);
            Fill(_rightRows, right);
        }

        private void Fill(RectTransform container, IList<ComparisonEntry> entries)
        {
            if (container == null || _rowTemplate == null)
                return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child != _rowTemplate)
                    Destroy(child.gameObject);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GameObject row = Instantiate(_rowTemplate.gameObject, container);
                row.name = "Row · " + entries[i].Title;
                row.SetActive(true);
                _glyphs.Bind(row.transform, entries[i]);
            }
        }
    }
}
