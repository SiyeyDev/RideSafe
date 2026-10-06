using System.Collections.Generic;

namespace RideSafe.Module01
{
    /// <summary>Convierte catálogo + selecciones en el reporte. Sin Unity, sin escena.</summary>
    public static class SelectionGrader
    {
        public static SelectionReport Grade(Module01CatalogSO catalog, SelectionLedger ledger)
        {
            List<string> coreSelected = new List<string>();
            List<string> coreOmitted = new List<string>();
            List<string> conditional = new List<string>();
            List<string> optional = new List<string>();
            List<string> inappropriate = new List<string>();

            if (catalog == null || ledger == null)
                return new SelectionReport(coreSelected, coreOmitted, conditional, optional, inappropriate);

            // El recorrido va sobre el catálogo, no sobre el libro: así un id que el
            // libro traiga y el catálogo no conozca se ignora solo, sin caso especial.
            foreach (SafetyItemSO item in catalog.AllItems)
            {
                bool chosen = ledger.Contains(item.ItemId);
                switch (item.Category)
                {
                    case SafetyItemCategory.Core:
                        (chosen ? coreSelected : coreOmitted).Add(item.ItemId);
                        break;
                    case SafetyItemCategory.ConditionDependent:
                        if (chosen) conditional.Add(item.ItemId);
                        break;
                    case SafetyItemCategory.Optional:
                        if (chosen) optional.Add(item.ItemId);
                        break;
                    case SafetyItemCategory.Inappropriate:
                        if (chosen) inappropriate.Add(item.ItemId);
                        break;
                }
            }

            return new SelectionReport(coreSelected, coreOmitted, conditional, optional, inappropriate);
        }
    }
}
