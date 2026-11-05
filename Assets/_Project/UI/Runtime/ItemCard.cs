using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI
{
    /// <summary>
    /// La tarjeta de un EPP en «Revisa tus elecciones» y «Evaluación de tus elecciones» (PO,
    /// 2026-11-04): el render como protagonista, el nombre debajo y, solo al evaluar, el icono del
    /// estado arriba a la derecha. Es el prefab <c>PF_EppCard</c>; la crea <see cref="CardGrid"/>.
    /// <para>
    /// Sin render —un EPP que todavía no se fotografió— enseña un marcador neutro en su lugar en
    /// vez de un hueco, y el nombre basta para leerla.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemCard : MonoBehaviour
    {
        [SerializeField] private Image _picture;
        [Tooltip("Lo que ocupa el sitio del render cuando el elemento no tiene ninguno.")]
        [SerializeField] private GameObject _placeholder;
        [SerializeField] private TMP_Text _name;
        [Tooltip("El icono del estado. Apagado en la revisión, que no adelanta la respuesta.")]
        [SerializeField] private Image _status;

        public string ItemId { get; private set; }
        public string Name => _name != null ? _name.text : string.Empty;
        public Sprite Picture => _picture != null && _picture.enabled ? _picture.sprite : null;
        public bool ShowsPlaceholder => _placeholder != null && _placeholder.activeSelf;
        public bool ShowsStatus => _status != null && _status.gameObject.activeSelf;
        public Sprite StatusSprite => _status != null ? _status.sprite : null;
        public Color StatusColor => _status != null ? _status.color : Color.clear;

        /// <summary>
        /// Rellena la tarjeta. Con <paramref name="glyphs"/> nulo no hay icono de estado: es la
        /// revisión, antes de enviar.
        /// </summary>
        public void Bind(ComparisonEntry entry, StatusGlyphs glyphs)
        {
            ItemId = entry.ItemId;
            if (_name != null)
                _name.text = entry.Title ?? string.Empty;

            bool hasPicture = entry.Image != null;
            if (_picture != null)
            {
                _picture.sprite = entry.Image;
                _picture.enabled = hasPicture;
            }
            if (_placeholder != null)
                _placeholder.SetActive(!hasPicture);

            Sprite glyph = glyphs?.For(entry.Status);
            if (_status != null)
            {
                _status.gameObject.SetActive(glyph != null);
                if (glyph != null)
                {
                    _status.sprite = glyph;
                    _status.color = glyphs.ColorFor(entry.Status);
                }
            }
        }
    }
}
