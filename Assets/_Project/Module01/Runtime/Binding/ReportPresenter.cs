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
        [SerializeField] private string _titleKey = "Module1/Report_Title";
        [SerializeField] private string _subtitleKey = "Module1/Report_Subtitle";
        [SerializeField] private string _yoursKey = "Module1/Report_Yours";
        [SerializeField] private string _referenceKey = "Module1/Report_Reference";

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

            foreach (SafetyItemSO item in catalog.AllItems)
            {
                bool chosen = Contains(report.CoreSelected, item.ItemId)
                              || Contains(report.OptionalSelected, item.ItemId)
                              || Contains(report.ConditionDependentSelected, item.ItemId)
                              || Contains(report.InappropriateSelected, item.ItemId);

                // El núcleo aparece siempre, elegido u omitido, porque su ausencia es
                // la información. Las demás categorías solo aparecen si el aprendiz las
                // eligió: listar un opcional que no tomó lo haría parecer un olvido.
                if (item.Category != SafetyItemCategory.Core && !chosen)
                    continue;

                ItemStatus status;
                switch (item.Category)
                {
                    case SafetyItemCategory.Core:
                        status = chosen ? ItemStatus.Selected : ItemStatus.CoreOmitted;
                        break;
                    case SafetyItemCategory.ConditionDependent:
                        status = ItemStatus.ConditionDependent;
                        break;
                    case SafetyItemCategory.Inappropriate:
                        status = ItemStatus.NotSuited;
                        break;
                    default:
                        status = ItemStatus.Selected;
                        break;
                }

                string title = translate != null ? translate(item.NameKey) : item.NameKey;
                entries.Add(new ComparisonEntry(status, title, item.ItemId));
            }

            return entries;
        }

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
