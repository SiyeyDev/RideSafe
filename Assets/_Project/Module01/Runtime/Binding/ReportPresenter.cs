using System;
using System.Collections.Generic;
using Cachacos;
using UnityEngine;
using RideSafe.UI;

namespace RideSafe.Module01
{
    /// <summary>
    /// Pinta el reporte en el tablero comparativo.
    /// <para>
    /// Se vuelve a construir entero cuando cambia el idioma, porque los títulos salen
    /// de claves y no de texto guardado. El estado visual nunca depende solo del color:
    /// ComparisonView acompaña cada fila con icono y forma.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Report Presenter")]
    public class ReportPresenter : MonoBehaviour
    {
        [SerializeField] private Module01CatalogSO _catalog;
        [SerializeField] private Module01Binding _binding;

        [Tooltip("Claves de localización del encabezado del tablero.")]
        [SerializeField] private string _titleKey = "M1/Report_Title";
        [SerializeField] private string _subtitleKey = "M1/Report_Subtitle";
        [SerializeField] private string _yoursKey = "M1/Report_Yours";
        [SerializeField] private string _referenceKey = "M1/Report_Reference";

        private ILocalizationProvider _localization;
        private SelectionReport _last;

        private void OnEnable()
        {
            _localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            if (_localization != null)
                _localization.LanguageChanged += Repaint;
        }

        private void OnDisable()
        {
            if (_localization != null)
                _localization.LanguageChanged -= Repaint;
        }

        public void Show(SelectionReport report)
        {
            _last = report;
            Repaint();
        }

        private void Repaint()
        {
            if (_last == null || _binding == null || _binding.Comparison == null)
                return;

            List<ComparisonEntry> entries = BuildEntries(_catalog, _last, Translate);

            _binding.Comparison.SetContent(
                Translate(_titleKey), Translate(_subtitleKey),
                Translate(_yoursKey), entries,
                Translate(_referenceKey), entries);
        }

        private string Translate(string key) =>
            _localization != null ? _localization.GetTranslation(key) : key;

        /// <summary>
        /// Una fila por elemento que el reporte debe mostrar.
        /// <c>Detail</c> lleva el id para que el reporte sea rastreable y testeable.
        /// </summary>
        public static List<ComparisonEntry> BuildEntries(Module01CatalogSO catalog, SelectionReport report,
                                                         Func<string, string> translate)
        {
            List<ComparisonEntry> entries = new List<ComparisonEntry>();
            if (catalog == null || report == null)
                return entries;

            // Solo lo que el aprendiz SE LLEVO, y en tres estados. Lo pidio Charlie el
            // 2026-10-16: listar lo omitido convierte el resumen en un boletin de fallos, y
            // el porque de cada cosa se explica al final, con el video.
            foreach (SafetyItemSO item in catalog.AllItems)
            {
                if (item == null)
                    continue;

                bool chosen = Contains(report.CoreSelected, item.ItemId)
                              || Contains(report.OptionalSelected, item.ItemId)
                              || Contains(report.ConditionDependentSelected, item.ItemId)
                              || Contains(report.InappropriateSelected, item.ItemId);
                if (!chosen)
                    continue;

                ItemStatus status;
                switch (item.Category)
                {
                    case SafetyItemCategory.Core:
                        status = ItemStatus.Selected;
                        break;
                    case SafetyItemCategory.Inappropriate when Contains(report.InappropriateSelected, item.ItemId):
                        // Deteriorado y "no es lo que toca" se dicen igual: son tres estados.
                        // Por la lista y no por la categoría: la gorra junto al casco no es peligro.
                        status = ItemStatus.NotSuited;
                        break;
                    default:
                        // Opcional y segun condicion: no cambian la nota.
                        status = ItemStatus.NoEffect;
                        break;
                }

                string title = translate != null ? translate(item.NameKey) : item.NameKey;
                // El id va además en su propio campo: una tarjeta tiene que saber de qué es sin
                // leerse el detalle, y necesita pedirle su render al elemento.
                entries.Add(new ComparisonEntry(status, title, item.ItemId)
                {
                    ItemId = item.ItemId,
                    Image = item.Thumbnail,
                });
            }

            return entries;
        }

        /// <summary>
        /// Los puntos del vehículo, todos: los que el aprendiz revisó y los que no, sea porque dijo
        /// que no o porque ni los señaló (PO, 2026-11-06). Aquí no vale lo de listar solo lo elegido
        /// de <see cref="BuildEntries"/>: lo que se quiere leer es justo lo que quedó sin revisar.
        /// </summary>
        public static List<ComparisonEntry> BuildInspectionEntries(Module01CatalogSO catalog, SelectionReport report,
                                                                   Func<string, string> translate)
        {
            List<ComparisonEntry> entries = new List<ComparisonEntry>();
            if (catalog == null || report == null)
                return entries;

            foreach (SafetyItemSO item in catalog.AllItems)
            {
                if (item == null)
                    continue;
                bool reviewed = Contains(report.CoreSelected, item.ItemId);
                if (!reviewed && !Contains(report.CoreOmitted, item.ItemId))
                    continue;
                string title = translate != null ? translate(item.NameKey) : item.NameKey;
                entries.Add(new ComparisonEntry(reviewed ? ItemStatus.Selected : ItemStatus.CoreOmitted, title, item.ItemId)
                {
                    ItemId = item.ItemId,
                    StateKey = reviewed ? ReviewedKey : NotReviewedKey,
                });
            }
            return entries;
        }

        public const string ReviewedKey = "M1/Inspection_Reviewed";
        public const string NotReviewedKey = "M1/Inspection_NotReviewed";

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                    return true;
            }
            return false;
        }
    }
}
