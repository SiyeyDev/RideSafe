using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using RideSafe.Module01.Editor;

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
            // Ni el numero ni las zonas se escriben a mano: salen de Module01Library, que es
            // el Excel del cliente transcrito. Asi el test no hay que retocarlo cada vez que
            // el cliente mueva un elemento, y a la vez no deja que el catalogo se desvie.
            Assert.AreEqual(Module01Library.All.Length,
                            new List<SafetyItemSO>(catalog.AllItems).Count);
            Assert.AreEqual(Module01Library.Tables.Length, catalog.Zones.Count);
        }

        /// <summary>
        /// Zonas sin elemento de riesgo, con su motivo. Son excepciones temporales y la
        /// lista está aquí a propósito: una zona sin respuesta equivocada no enseña nada,
        /// porque el aprendiz acepta todo y acierta. Vaciar esta lista al resolverlas.
        /// </summary>
        private static readonly Dictionary<string, string> ZonesWithoutRisk = new Dictionary<string, string>
        {
            // Vacía desde el 2026-10-14: el garaje v7 trajo las prendas y `clothing_loose`
            // volvió al catálogo con la bufanda. Las tres zonas vuelven a poder enseñarse mal.
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

                // Lo que no puede pasar es que una mesa no se juzgue en absoluto: seria
                // decorado. Pero exigir nucleo EN CADA mesa ya no vale, y el caso es real:
                // en la mesa de «Visibilidad y control» los dos unicos esenciales de la hoja
                // son las luces, que son «esencial condicional» y con dia claro cuentan como
                // bono. Esa mesa ensena a NO llevarse cosas -audifonos, AirPods, mitones, el
                // vaso de cafe- y eso es un ejercicio legitimo.
                Assert.IsTrue(hasCore || hasInappropriate,
                    $"La zona '{zone.ZoneId}' no tiene nada que se juzgue: es decorado.");

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
                StringAssert.StartsWith("M1/", item.NameKey,
                    $"El elemento '{item.ItemId}' no usa la categoria Module1.");
            }
        }
    }
}
