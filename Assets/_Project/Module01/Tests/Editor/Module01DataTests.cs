using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Valida los assets reales de la primera pasada, no instancias en memoria.
    /// Si alguien edita el catálogo y deja un id duplicado o un elemento sin clave
    /// de localización, esto falla antes de que la escena llegue a Play.
    /// </summary>
    public class Module01DataTests
    {
        private const string CatalogPath = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";
        private const string VehiclePath = "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset";

        private const string Missing =
            "Corre el menu RideSafe > Modulo 1 > Crear datos de la primera pasada.";

        [Test]
        public void El_catalogo_real_existe_y_no_tiene_problemas_de_validacion()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, Missing);

            CollectionAssert.IsEmpty(catalog.Validate());
            Assert.AreEqual(8, new List<SafetyItemSO>(catalog.AllItems).Count);
            Assert.AreEqual(3, catalog.Zones.Count);
        }

        [Test]
        public void Cada_zona_tiene_al_menos_un_nucleo_y_un_inapropiado()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, Missing);

            foreach (ZoneSO zone in catalog.Zones)
            {
                bool hasCore = false;
                bool hasInappropriate = false;
                foreach (SafetyItemSO item in zone.Items)
                {
                    if (item.Category == SafetyItemCategory.Core) hasCore = true;
                    if (item.Category == SafetyItemCategory.Inappropriate) hasInappropriate = true;
                }

                Assert.IsTrue(hasCore, $"La zona '{zone.ZoneId}' no tiene ningun elemento nucleo.");
                Assert.IsTrue(hasInappropriate, $"La zona '{zone.ZoneId}' no tiene ningun elemento de riesgo.");
            }
        }

        [Test]
        public void El_perfil_del_ebike_declara_su_valor_de_contexto_y_sus_zonas()
        {
            VehicleProfileSO profile = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(VehiclePath);
            Assert.IsNotNull(profile, Missing);

            Assert.AreEqual("ebike", profile.VehicleContextValue);
            Assert.AreEqual(3, profile.InspectionZones.Count);
            Assert.IsNotNull(profile.Model, "El perfil debe apuntar al greybox mientras llega 3D.");
        }

        [Test]
        public void Las_claves_de_localizacion_viven_bajo_la_categoria_Module1()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, Missing);

            foreach (SafetyItemSO item in catalog.AllItems)
            {
                StringAssert.StartsWith("Module1/", item.NameKey,
                    $"El elemento '{item.ItemId}' no usa la categoria Module1.");
            }
        }
    }
}
