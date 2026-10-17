using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RideSafe.UI
{
    /// <summary>
    /// Clave de estado posterior al envío. El estado lo lleva la forma, el color Y el texto,
    /// nunca el color solo.
    /// <para>
    /// Los cuatro que la reunión pidió en la leyenda son <see cref="Selected"/> (correcto),
    /// <see cref="NotSuited"/> (incorrecto), <see cref="CoreOmitted"/> (omisión) y
    /// <see cref="AtRisk"/> (en riesgo o deteriorado). <see cref="ConditionDependent"/> no
    /// entra en la leyenda: no es ni acierto ni fallo, se pinta neutro.
    /// </para>
    /// </summary>
    public enum ItemStatus
    {
        Selected = 0,
        CoreOmitted = 1,
        ConditionDependent = 2,
        NotSuited = 3,

        /// <summary>
        /// Elegido estando deteriorado. Resta igual que <see cref="NotSuited"/>, pero el
        /// mensaje es otro: el casco dañado enseña a mirar el estado del equipo, no a elegir
        /// otra cosa.
        /// </summary>
        AtRisk = 4,

        /// <summary>
        /// Elegido y no cambia la nota: lo opcional y lo que depende de la condicion. El
        /// cliente pidio tres estados en el resumen —correcto, incorrecto y este— y nada mas.
        /// </summary>
        NoEffect = 5
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
        [Tooltip("En orden de ItemStatus: Selected, CoreOmitted, ConditionDependent, NotSuited, AtRisk.")]
        public Sprite[] Sprites = new Sprite[5];

        public Sprite For(ItemStatus status)
        {
            int index = (int)status;
            return Sprites != null && index < Sprites.Length ? Sprites[index] : null;
        }

        /// <summary>Rellena una fila de estado (hijos 'Icon', 'Text/Title', 'Text/Detail', 'State').</summary>
        public void Bind(Transform row, ComparisonEntry entry) => Bind(row, entry, null);

        /// <summary>
        /// Igual, y además escribe la palabra del estado a la derecha de la fila.
        /// <para>
        /// Con <paramref name="stateLabel"/> vacío la columna se apaga, que es lo que necesita
        /// el panel de explicación: allí hay una sola fila y el estado ya se cuenta en el texto.
        /// </para>
        /// </summary>
        public void Bind(Transform row, ComparisonEntry entry, string stateLabel)
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

            TMP_Text state = FindComponent<TMP_Text>(row, "State");
            if (state != null)
            {
                state.text = stateLabel ?? string.Empty;
                state.gameObject.SetActive(!string.IsNullOrEmpty(stateLabel));
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

        [Tooltip("Clave de la palabra de cada estado, en orden de ItemStatus. Se muestra en cada fila.")]
        [SerializeField]
        private string[] _stateKeys =
        {
            "M1/Legend_Correct", "M1/Legend_Omission", "M1/Conditional",
            "M1/Legend_Incorrect", "M1/Legend_AtRisk", "M1/Legend_NoEffect"
        };

        private readonly List<ComparisonEntry> _current = new List<ComparisonEntry>();

        private RectTransform _below;
        private float _belowGap;

        /// <summary>
        /// Lo que tiene que quedar siempre por debajo de la lista, mida lo que mida.
        /// <para>
        /// La columna crece con el contenido —lleva un <c>ContentSizeFitter</c>— pero el botón
        /// «Ver explicación» estaba clavado 469 px por debajo del borde del panel. Medido: con
        /// 5 filas sobran 62 px y <b>a partir de 7 la lista se le echa encima</b>. Y la sección
        /// personal puede llegar a 11 filas, porque el aprendiz puede aceptar todo lo que hay.
        /// </para>
        /// </summary>
        public void KeepBelow(RectTransform follower, float gap)
        {
            _below = follower;
            _belowGap = gap;
            ArrangeBelow();
        }

        /// <summary>Baja lo que sigue a la lista hasta justo debajo de ella.</summary>
        public void ArrangeBelow()
        {
            if (_below == null || _leftRows == null)
                return;

            RectTransform column = _leftRows.parent as RectTransform;
            if (column == null)
                return;

            // Sin esto se mide el alto del fotograma anterior: el ContentSizeFitter no ha
            // recalculado todavia y el boton se coloca para la lista que habia antes.
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);

            Vector2 sitio = _below.anchoredPosition;
            sitio.y = column.anchoredPosition.y - column.rect.height - _belowGap;
            _below.anchoredPosition = sitio;
        }

        /// <summary>Solo para tests: el alto que ocupa ahora la columna de la lista.</summary>
        public float ColumnHeight =>
            _leftRows != null && _leftRows.parent is RectTransform c ? c.rect.height : 0f;

        /// <summary>
        /// Lo que el panel está mostrando. Público para que un test pueda afirmar sobre el
        /// contenido sin depender del idioma ni de leer píxeles.
        /// </summary>
        public IReadOnlyList<ComparisonEntry> Entries => _current;

        /// <summary>
        /// La palabra del estado, ya traducida.
        /// <para>
        /// Lo condicional también lleva la suya. Se probó a dejarlo en blanco —no es ni acierto
        /// ni fallo, así que no entra en la cuenta— y en pantalla el hueco no se lee como
        /// «no aplica» sino como un dato que falta. Decirlo es más barato que explicarlo.
        /// </para>
        /// </summary>
        private string StateLabel(ItemStatus status)
        {
            int i = (int)status;
            if (_stateKeys == null || i >= _stateKeys.Length || string.IsNullOrEmpty(_stateKeys[i]))
                return string.Empty;
            return UIText.Resolve(_stateKeys[i]);
        }

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
            if (_leftHeading != null) { _leftHeading.text = leftHeading; _leftHeading.gameObject.SetActive(true); }
            if (_rightHeading != null) { _rightHeading.text = rightHeading; _rightHeading.gameObject.SetActive(true); }
            Fill(_leftRows, left);
            Fill(_rightRows, right);
        }

        /// <summary>
        /// «Mi Elección»: <b>una sola lista en una sola columna</b>.
        /// <para>
        /// La reunión pidió quitar la tabla duplicada de «Referencia para este recorrido» y
        /// dejar un único panel. Primero se repartió la lista en las dos columnas que ya
        /// existían, pero leer de arriba abajo y luego saltar a la derecha para seguir la misma
        /// lista es peor que una columna larga: parecen dos tablas otra vez, que es justo lo que
        /// se quería quitar.
        /// </para>
        /// <para>
        /// Caben las doce filas que puede llegar a tener —7 de protección más 5 del vehículo—
        /// porque cada fila es de una sola línea: el estado va como palabra a su derecha, no
        /// como segunda línea ni en una leyenda al pie.
        /// </para>
        /// </summary>
        public void SetSingleList(string title, string note, IList<ComparisonEntry> entries)
        {
            if (_title != null) _title.text = title;
            if (_subtitle != null) _subtitle.text = note;
            if (_leftHeading != null) _leftHeading.gameObject.SetActive(false);
            if (_rightHeading != null) _rightHeading.gameObject.SetActive(false);

            // La segunda columna se apaga entera, no solo se vacía: un contenedor vacío sigue
            // ocupando su sitio y dejaba el panel descentrado.
            if (_rightRows != null && _rightRows.parent != null)
                _rightRows.parent.gameObject.SetActive(false);

            Fill(_leftRows, entries);
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
                // El Detail de este panel lleva el id del elemento -'helmet_ok', 'point_brakes'-
                // para que el reporte sea rastreable y testeable, pero eso NO se enseña: es
                // texto de depuración y estaba saliendo debajo de cada título. Se pinta sin él;
                // quien lo necesita lo lee de Entries.
                ComparisonEntry visible = new ComparisonEntry(entries[i].Status, entries[i].Title);
                _glyphs.Bind(row.transform, visible, StateLabel(entries[i].Status));
            }

            if (container == _leftRows)
            {
                ArrangeBelow();
                _current.Clear();
                _current.AddRange(entries);
            }
        }
    }
}
