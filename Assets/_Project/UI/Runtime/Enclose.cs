using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// The box of a panel: keeps this sliced background around every visible piece it holds
    /// (title, options, list, buttons) with the same margin on each side, behind them, and
    /// follows them when a list grows. Pieces were laid out one by one with nothing behind,
    /// so titles floated above their box and buttons below it.
    /// <para>
    /// Works in the parent's space; the pieces may be nested deeper. Runs after the layout
    /// helpers (FollowBelow, GrowWithContent) and writes only when something moved, so an idle
    /// panel never dirties its canvas.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Enclose")]
    [DefaultExecutionOrder(1000)]
    public class Enclose : MonoBehaviour
    {
        [SerializeField] private RectTransform[] _pieces = new RectTransform[0];
        [Tooltip("Margin between the visible edge and the pieces: left, top, right, bottom.")]
        [SerializeField] private Vector4 _margin = new Vector4(24f, 24f, 24f, 24f);
        [Tooltip("Transparent padding of the atlas cell around its art, in this rect's units.")]
        [SerializeField, Min(0f)] private float _atlasPadding;

        private readonly Vector3[] _corners = new Vector3[4];

        public void Configure(RectTransform[] pieces, Vector4 margin, float atlasPadding)
        {
            _pieces = pieces;
            _margin = margin;
            _atlasPadding = atlasPadding;
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            RectTransform frame = (RectTransform)transform;
            Transform space = frame.parent;
            if (space == null)
                return;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool any = false;
            foreach (RectTransform piece in _pieces)
            {
                if (piece == null || !piece.gameObject.activeInHierarchy)
                    continue;
                piece.GetWorldCorners(_corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 local = space.InverseTransformPoint(_corners[i]);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
                any = true;
            }
            if (!any)
                return;

            float grow = _atlasPadding;
            min -= new Vector2(_margin.x + grow, _margin.w + grow);
            max += new Vector2(_margin.z + grow, _margin.y + grow);

            // Top-left anchored, top-left pivoted, like every rect of the kit.
            Rect parent = ((RectTransform)space).rect;
            Vector2 position = new Vector2(min.x - parent.xMin, max.y - parent.yMax);
            Vector2 size = max - min;
            if (frame.anchorMin != new Vector2(0f, 1f) || frame.anchorMax != new Vector2(0f, 1f) || frame.pivot != new Vector2(0f, 1f))
            {
                frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 1f);
            }
            if ((frame.anchoredPosition - position).sqrMagnitude > 1e-4f)
                frame.anchoredPosition = position;
            if ((frame.sizeDelta - size).sqrMagnitude > 1e-4f)
                frame.sizeDelta = size;
            if (frame.GetSiblingIndex() != 0)
                frame.SetAsFirstSibling();
        }
    }
}
