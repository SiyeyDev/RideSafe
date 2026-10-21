using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace RideSafe.UI
{
    /// <summary>
    /// The company mark at the top left of a panel, on the line of its title (2026-10-21).
    /// <para>
    /// Its left edge stays a fixed distance from the title's box, and its centre on the middle of
    /// the capitals of the title's first line. That line is only known once the text is set:
    /// the prefabs hold English, the session may be Spanish, and a centred title that wraps
    /// climbs. Runs after the layout helpers and writes only when the mark has to move.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/UI/Title Mark")]
    [DefaultExecutionOrder(1050)]
    [RequireComponent(typeof(RectTransform))]
    public class TitleMark : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [Tooltip("Left edge of the mark from the left edge of the title's box, in the title's units.")]
        [SerializeField] private float _offsetX;

        public void Configure(TMP_Text title, float offsetX)
        {
            _title = title;
            _offsetX = offsetX;
        }

        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (_title == null || !_title.gameObject.activeInHierarchy)
                return;
            // A text set this frame (the localiser's) is laid out only when the canvas renders: lay it
            // out now, or the mark sits on last frame's lines.
            if (_title.havePropertiesChanged || _title.textInfo == null || _title.textInfo.characterCount == 0)
                _title.ForceMeshUpdate();
            TMP_TextInfo info = _title.textInfo;
            if (info == null || info.characterCount == 0)
                return;

            TMP_LineInfo line = info.lineInfo[0];
            int first = Mathf.Clamp(line.firstVisibleCharacterIndex, 0, info.characterCount - 1);
            TMP_CharacterInfo character = info.characterInfo[first];
            // Half the cap height of the first line's own size: a bold title line at 112% too.
            FaceInfo face = character.fontAsset != null ? character.fontAsset.faceInfo : _title.font.faceInfo;
            float capHeight = face.capLine * character.pointSize / face.pointSize * face.scale;
            Vector3 target = _title.transform.TransformPoint(
                new Vector3(_title.rectTransform.rect.xMin + _offsetX, character.baseLine + capHeight * 0.5f, 0f));

            RectTransform mark = (RectTransform)transform;
            // Left edge on the point, centred on it vertically, whatever the anchors.
            Vector3 corner = mark.TransformPoint(new Vector3(mark.rect.xMin, mark.rect.center.y, 0f));
            Vector3 move = target - corner;
            if (move.sqrMagnitude > 1e-10f)
                mark.position += move;
        }
    }
}
