using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.UI
{
    /// <summary>
    /// Una tarjeta por elemento dentro de una cuadrícula (PO, 2026-11-04). La usan «Revisa tus
    /// elecciones», sin iconos, y «Evaluación de tus elecciones», con ellos: la tarjeta y su
    /// dibujo viven en un único sitio.
    /// <para>
    /// Solo crea las tarjetas. El <c>GridLayoutGroup</c> del contenido las coloca, y quien tenga
    /// la ventana de scroll —<see cref="GrowWithContent"/> en la revisión, <see cref="ComparisonView"/>
    /// en la evaluación— la ajusta a lo que midan.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class CardGrid : MonoBehaviour
    {
        [Tooltip("La tarjeta que se copia por cada elemento (PF_EppCard).")]
        [SerializeField] private ItemCard _cardPrefab;
        [Tooltip("Donde van las tarjetas: el contenido con el GridLayoutGroup.")]
        [SerializeField] private RectTransform _content;

        private readonly List<ItemCard> _cards = new List<ItemCard>();

        public IReadOnlyList<ItemCard> Cards => _cards;
        public RectTransform Content => _content;

        public void Configure(ItemCard cardPrefab, RectTransform content)
        {
            _cardPrefab = cardPrefab;
            _content = content;
        }

        private void Awake()
        {
            // Las muestras horneadas en el prefab para verlo al editar no llegan al runtime.
            if (_cards.Count == 0)
                Clear();
        }

        /// <summary>
        /// Cambia las tarjetas por las de <paramref name="entries"/>, en su orden. Con
        /// <paramref name="glyphs"/> cada tarjeta lleva el icono de su estado; sin ellos, ninguno.
        /// </summary>
        public void Show(IList<ComparisonEntry> entries, StatusGlyphs glyphs = null)
        {
            Clear();
            if (_cardPrefab == null || _content == null || entries == null)
                return;
            for (int i = 0; i < entries.Count; i++)
            {
                ItemCard card = Instantiate(_cardPrefab, _content);
                card.name = "Card · " + entries[i].Title;
                card.gameObject.SetActive(true);
                card.Bind(entries[i], glyphs);
                _cards.Add(card);
            }
        }

        public void Clear()
        {
            _cards.Clear();
            if (_content == null)
                return;
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                GameObject child = _content.GetChild(i).gameObject;
                // Fuera de la cuadrícula ya: Destroy espera al final del fotograma, y hasta entonces
                // la tarjeta vieja contaría al medir la ventana.
                child.SetActive(false);
                child.transform.SetParent(null, false);
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }
}
