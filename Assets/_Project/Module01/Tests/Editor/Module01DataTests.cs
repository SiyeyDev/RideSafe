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
        private const string ScooterPath = "Assets/_Project/Module01/Data/SO_VehicleProfile_Escooter.asset";

        private const string Missing =
            "Corre el menu RideSafe > Modulo 1 > Crear datos de la primera pasada.";

        [Test]
        public void El_catalogo_real_existe_y_no_tiene_problemas_de_validacion()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, Missing);

            CollectionAssert.IsEmpty(catalog.Validate());
            Assert.AreEqual(7, new List<SafetyItemSO>(catalog.AllItems).Count);
            Assert.AreEqual(3, catalog.Zones.Count);
        }

        /// <summary>
        /// Zonas sin elemento de riesgo, con su motivo. Son excepciones temporales y la
        /// lista está aquí a propósito: una zona sin respuesta equivocada no enseña nada,
        /// porque el aprendiz acepta todo y acierta. Vaciar esta lista al resolverlas.
        /// </summary>
        private static readonly Dictionary<string, string> ZonesWithoutRisk = new Dictionary<string, string>
        {
            // Charlie, 2026-10-07: no hay modelo de ropa suelta, así que se apaga el riesgo.
            { "clothing", "clothing_loose apagado hasta que exista modelo o se sustituya el riesgo" }
        };

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

                if (ZonesWithoutRisk.TryGetValue(zone.ZoneId, out string reason))
                {
                    Assert.IsFalse(hasInappropriate,
                        $"La zona '{zone.ZoneId}' ya tiene riesgo ({reason}): quitala de ZonesWithoutRisk.");
                    continue;
                }

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
        public void El_perfil_del_escooter_declara_su_valor_de_contexto_y_su_modelo()
        {
            VehicleProfileSO profile = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(profile, Missing);

            Assert.AreEqual("escooter", profile.VehicleContextValue);
            Assert.IsNotNull(profile.Model, "El perfil debe apuntar a scooter.fbx.");
        }

        /// <summary>
        /// Se revisa lo mismo en los dos vehículos —frenos, luces, llantas—, solo que
        /// en sitios distintos. Compartir las 3 zonas es lo que evita duplicar los
        /// elementos y sus claves de localización.
        /// </summary>
        [Test]
        public void Los_dos_vehiculos_comparten_las_mismas_tres_zonas()
        {
            VehicleProfileSO ebike = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(VehiclePath);
            VehicleProfileSO scooter = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(ebike, Missing);
            Assert.IsNotNull(scooter, Missing);

            CollectionAssert.AreEqual(ebike.InspectionZones, scooter.InspectionZones,
                "Los dos perfiles deben apuntar a las mismas ZoneSO, en el mismo orden.");
        }

        [Test]
        public void Cada_perfil_apunta_al_modelo_de_su_vehiculo()
        {
            VehicleProfileSO ebike = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(VehiclePath);
            VehicleProfileSO scooter = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(ebike, Missing);
            Assert.IsNotNull(scooter, Missing);

            StringAssert.Contains("ebike", ebike.Model.name.ToLowerInvariant());
            StringAssert.Contains("scooter", scooter.Model.name.ToLowerInvariant());
            Assert.AreNotSame(ebike.Model, scooter.Model,
                "Elegir patinete tiene que mostrar un patinete.");
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
