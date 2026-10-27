using System.Collections.Generic;
using System.Linq;
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
            Assert.IsNotNull(profile.Model, "El perfil debe apuntar a E_Scooter.fbx.");
        }

        /// <summary>
        /// Se revisa casi lo mismo en los dos vehículos —frenos, luces, llantas—, solo que
        /// en sitios distintos. Compartir esas 3 zonas es lo que evita duplicar los
        /// elementos y sus claves de localización: el scooter suma la plataforma, pero las
        /// otras tres son <b>las mismas</b> <see cref="ZoneSO"/> de la bici, no copias.
        /// </summary>
        [Test]
        public void Los_dos_vehiculos_comparten_las_tres_zonas_comunes()
        {
            VehicleProfileSO ebike = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(VehiclePath);
            VehicleProfileSO scooter = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(ebike, Missing);
            Assert.IsNotNull(scooter, Missing);

            CollectionAssert.AreEqual(ebike.InspectionZones,
                scooter.InspectionZones.Where(z => z.ZoneId != "deck").ToList(),
                "Las zonas comunes deben ser las mismas ZoneSO, en el mismo orden.");
        }

        [Test]
        public void El_escooter_recorre_plataforma_y_la_ebike_no()
        {
            VehicleProfileSO ebike = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(VehiclePath);
            VehicleProfileSO scooter = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(ebike, Missing);
            Assert.IsNotNull(scooter, Missing);

            CollectionAssert.AreEqual(new[] { "cockpit", "wheels", "visibility" },
                ebike.InspectionZones.Select(z => z.ZoneId).ToArray());
            CollectionAssert.AreEqual(new[] { "cockpit", "wheels", "deck", "visibility" },
                scooter.InspectionZones.Select(z => z.ZoneId).ToArray(),
                "Narrativa §4.8.2: el scooter revisa la plataforma; la bici no tiene plataforma.");

            ZoneSO deck = scooter.InspectionZones.First(z => z.ZoneId == "deck");
            Assert.AreEqual("M1/Zone_Deck", deck.TitleKey);
            Assert.AreEqual(1, deck.Items.Count);
            Assert.AreEqual("point_deck", deck.Items[0].ItemId);
            Assert.AreEqual(SafetyItemCategory.Core, deck.Items[0].Category,
                "Como los otros puntos: omitirlo es no diagnosticar el vehiculo.");
        }

        [Test]
        public void El_escooter_usa_el_modelo_nuevo()
        {
            VehicleProfileSO scooter = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(scooter, Missing);
            Assert.AreEqual("E_Scooter", scooter.Model.name);
        }

        /// <summary>
        /// «Usar modelos reales» deja el perfil del scooter en el modelo que declara
        /// <c>VehicleProfiles</c>. El comando se corre de verdad y el perfil se restaura después,
        /// pase lo que pase.
        /// </summary>
        [Test]
        public void Usar_modelos_reales_deja_al_scooter_su_modelo()
        {
            VehicleProfileSO scooter = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(ScooterPath);
            Assert.IsNotNull(scooter, Missing);
            SerializedObject so = new SerializedObject(scooter);
            UnityEngine.Object before = so.FindProperty("_model").objectReferenceValue;
            try
            {
                Module01RealModels.Apply();
                Assert.AreEqual("E_Scooter", scooter.Model.name);
            }
            finally
            {
                so.Update();
                so.FindProperty("_model").objectReferenceValue = before;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// La bici y la patineta se revisan parecido, pero son vehículos distintos y cada una tiene
        /// su video de revisión (PO, 2026-10-27). Mientras lleguen los reales, los dos llevan el de
        /// prueba; lo que no puede pasar es que uno se quede sin video.
        /// </summary>
        [Test]
        public void Cada_vehiculo_tiene_su_video_de_revision()
        {
            foreach (VehicleProfiles.Definition definition in VehicleProfiles.Definitions)
            {
                VehicleProfileSO profile = VehicleProfiles.Load(definition);
                Assert.IsNotNull(profile, Missing);
                Assert.IsNotNull(profile.ExplanationVideo, $"{profile.name} no tiene video de revisión.");
                Assert.AreEqual(definition.VideoPath, AssetDatabase.GetAssetPath(profile.ExplanationVideo));
            }
        }

        /// <summary>
        /// Cada zona que declara un vehículo existe: una mal escrita dejaba un hueco en su lista de
        /// zonas y el recorrido se paraba ahí.
        /// </summary>
        [Test]
        public void Cada_zona_que_declara_un_vehiculo_esta_en_su_perfil()
        {
            foreach (VehicleProfiles.Definition definition in VehicleProfiles.Definitions)
            {
                VehicleProfileSO profile = VehicleProfiles.Load(definition);
                Assert.IsNotNull(profile, Missing);
                CollectionAssert.AreEqual(definition.ZoneIds.ToArray(),
                    profile.InspectionZones.Select(z => z != null ? z.ZoneId : "(vacía)").ToArray(), profile.name);
            }
        }

        /// <summary>
        /// El reporte nombra cada fila con el catálogo de presentación. Hecho solo con la e-bike,
        /// la plataforma se calificaba pero no salía en «Mi Elección» del scooter.
        /// </summary>
        [Test]
        public void El_catalogo_de_presentacion_tiene_los_puntos_de_todos_los_vehiculos()
        {
            Module01CatalogSO presentation = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01PresentationCatalog.asset");
            Assert.IsNotNull(presentation, Missing);
            HashSet<string> named = new HashSet<string>(presentation.AllItems.Select(i => i.ItemId));

            foreach (VehicleProfileSO profile in VehicleProfiles.LoadAll())
                foreach (ZoneSO zone in profile.InspectionZones)
                    foreach (SafetyItemSO item in zone.Items)
                        Assert.IsTrue(named.Contains(item.ItemId),
                            $"'{item.ItemId}' de {profile.name} no tiene nombre en el reporte.");
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
