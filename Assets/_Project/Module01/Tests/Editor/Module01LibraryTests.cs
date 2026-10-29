using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RideSafe.Module01.Editor;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Ata <see cref="Module01Library"/> a la hoja del cliente.
    /// <para>
    /// No comprueba que el código funcione —eso lo hacen los tests del puntuador— sino que la
    /// <b>transcripción</b> siga cuadrando: los totales de cada bloque, los grupos de peligro y
    /// que nadie meta un elemento sin ID de la biblioteca. Si alguien añade una fila a ojo, aquí
    /// se entera.
    /// </para>
    /// </summary>
    public class Module01LibraryTests
    {
        private static IEnumerable<Module01Library.Entry> Of(SafetyItemCategory c)
        {
            foreach (var e in Module01Library.All)
                if (e.Category == c)
                    yield return e;
        }

        [Test]
        public void El_pool_esencial_de_dia_claro_son_los_65_de_la_hoja()
        {
            // Casco 40 + calzado 20 + ropa ceñida 5. Las dos luces NO entran: son «esencial
            // condicional» y de día claro la hoja las paga como bono de 1.
            float total = 0f;
            int n = 0;
            foreach (var e in Of(SafetyItemCategory.Core)) { total += e.Weight; n++; }

            Assert.AreEqual(3, n, "De día claro solo hay tres esenciales.");
            Assert.AreEqual(65f, total, 0.01f);
        }

        [Test]
        public void Los_peligros_suman_los_235_de_la_hoja()
        {
            // Solo los de la hoja (H): los P son los peligros evidentes que pidió la PO, aparte.
            float total = 0f;
            int n = 0;
            foreach (var e in Of(SafetyItemCategory.Inappropriate).Where(e => e.LibraryId.StartsWith("H")))
            { total += e.Weight; n++; }

            Assert.AreEqual(12, n, "Las 11 «peligrosa visible» más la gorra condicional.");
            Assert.AreEqual(255f, total, 0.01f, "235 de las visibles más los 20 de la gorra.");
        }

        [Test]
        public void Los_peligros_evidentes_comparten_grupo_con_el_peligro_que_repiten()
        {
            // Ropa suelta, los audífonos y las chanclas de antes: llevarse varios descuenta una vez.
            string[] evident = Of(SafetyItemCategory.Inappropriate)
                .Where(e => !e.LibraryId.StartsWith("H")).Select(e => e.LibraryId).ToArray();
            CollectionAssert.AreEquivalent(new[] { "P24", "P25", "P26", "P27", "P28", "P29", "P30" }, evident);
        }

        [Test]
        public void Todo_peligro_declara_su_grupo_y_todo_lo_demas_no()
        {
            foreach (var e in Module01Library.All)
            {
                if (e.Category == SafetyItemCategory.Inappropriate)
                    Assert.IsNotNull(e.HazardGroup, $"{e.LibraryId} sin grupo de peligro.");
                else
                    Assert.IsNull(e.HazardGroup, $"{e.LibraryId} no es un peligro y trae grupo.");
            }
        }

        [Test]
        public void Los_grupos_compartidos_estan()
        {
            Dictionary<string, List<string>> byGroup = new Dictionary<string, List<string>>();
            foreach (var e in Of(SafetyItemCategory.Inappropriate))
            {
                if (!byGroup.TryGetValue(e.HazardGroup, out var l))
                    byGroup[e.HazardGroup] = l = new List<string>();
                l.Add(e.LibraryId);
            }

            CollectionAssert.AreEquivalent(new[] { "H01", "H02" }, byGroup["helmet_integrity"],
                "Los dos cascos rotos descuentan una sola vez.");
            CollectionAssert.AreEquivalent(new[] { "H03", "H13", "P29" }, byGroup["auditory_occlusion"],
                "Audífonos, AirPods y los audífonos de antes son el mismo error.");
            CollectionAssert.AreEquivalent(new[] { "H04", "P30" }, byGroup["footwear_control"],
                "Las Crocs y las chanclas de antes son el mismo error.");
            CollectionAssert.AreEquivalent(new[] { "P24", "P25", "P26", "P27", "P28" }, byGroup["loose_clothing"],
                "Toda la ropa suelta descuenta una sola vez.");

            string[] shared = { "helmet_integrity", "auditory_occlusion", "footwear_control", "loose_clothing" };
            foreach (var kv in byGroup)
                if (!shared.Contains(kv.Key))
                    Assert.AreEqual(1, kv.Value.Count, $"El grupo {kv.Key} debería ser de uno solo.");
        }

        [Test]
        public void El_par_de_bono_excluyente_se_apunta_en_los_dos_sentidos()
        {
            // Si solo lo declarara uno, el orden del catálogo decidiría si se cobra doble.
            Dictionary<string, Module01Library.Entry> byId = new Dictionary<string, Module01Library.Entry>();
            foreach (var e in Module01Library.All) byId[e.ItemId] = e;

            int declarados = 0;
            foreach (var e in Module01Library.All)
            {
                if (string.IsNullOrEmpty(e.ExclusiveBonusWith)) continue;
                declarados++;
                Assert.IsTrue(byId.ContainsKey(e.ExclusiveBonusWith),
                    $"{e.LibraryId} apunta a {e.ExclusiveBonusWith}, que no existe.");
                Assert.AreEqual(e.ItemId, byId[e.ExclusiveBonusWith].ExclusiveBonusWith,
                    "La exclusividad tiene que ser recíproca.");
            }

            Assert.AreEqual(2, declarados, "La hoja solo declara el par O01 / O02.");
        }

        [Test]
        public void Ningun_elemento_entra_sin_su_id_de_la_biblioteca()
        {
            HashSet<string> vistos = new HashSet<string>();
            foreach (var e in Module01Library.All)
            {
                // P: añadidos por la PO que la hoja no tiene (coderas y rodilleras).
                StringAssert.IsMatch("^[EOHNP][0-9]{2}$", e.LibraryId);
                Assert.IsTrue(vistos.Add(e.LibraryId), $"{e.LibraryId} está dos veces.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.ItemId), $"{e.LibraryId} sin itemId.");
            }
        }

        [Test]
        public void Los_ids_del_motor_no_se_repiten()
        {
            HashSet<string> vistos = new HashSet<string>();
            foreach (var e in Module01Library.All)
                Assert.IsTrue(vistos.Add(e.ItemId), $"{e.ItemId} está dos veces.");
        }

        [Test]
        public void Los_ocho_ids_que_ya_tenian_traduccion_y_audio_se_conservan()
        {
            // Renombrarlos tiraría sus términos de I2 y sus mp3 de locución.
            string[] heredados =
            {
                "helmet_ok", "helmet_damaged", "shoes_ok", "trousers_fitted",
                "reflective", "glasses", "cargo_secured", "cargo_handheld"
            };

            HashSet<string> ids = new HashSet<string>();
            foreach (var e in Module01Library.All) ids.Add(e.ItemId);

            foreach (string id in heredados)
                Assert.IsTrue(ids.Contains(id), $"{id} ya tenía I2 y audio: no se puede perder.");
        }

        [Test]
        public void Cada_elemento_va_a_una_de_las_tres_mesas()
        {
            foreach (var e in Module01Library.All)
                CollectionAssert.Contains(
                    new[] { Module01Library.Mesa1, Module01Library.Mesa2, Module01Library.Mesa3 },
                    e.Table, $"{e.LibraryId} no tiene mesa válida.");
        }

        [Test]
        public void Las_tres_mesas_tienen_elementos()
        {
            foreach (var t in Module01Library.Tables)
                CollectionAssert.IsNotEmpty(Module01Library.Of(t.Id), $"La mesa {t.Id} está vacía.");
        }

        [Test]
        public void Ninguna_mesa_choca_con_una_zona_del_vehiculo()
        {
            // El id de entidad es «module01.{zona}.{elemento}». Si una mesa se llamara igual
            // que una zona del vehículo, dos entidades distintas compartirían id y una de las
            // dos dejaría de recibir clics, sin un solo error en consola.
            string[] zonasDeVehiculo = { "cockpit", "wheels", "visibility" };

            foreach (var t in Module01Library.Tables)
                CollectionAssert.DoesNotContain(zonasDeVehiculo, t.Id,
                    $"La mesa {t.Id} choca con una zona del vehículo.");
        }

        [Test]
        public void Cada_mesa_declara_su_clave_de_titulo_y_no_se_repiten()
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (var t in Module01Library.Tables)
            {
                StringAssert.StartsWith("M1/Zone_", t.TitleKey);
                Assert.IsTrue(keys.Add(t.TitleKey), $"{t.TitleKey} está dos veces.");
            }
        }

        [Test]
        public void Todo_modelo_declarado_existe_en_disco()
        {
            // Un FBX mal escrito deja el elemento en greybox sin que nadie se entere hasta
            // abrir la escena. Ya pasó con el espacio final de «jean_ajustado_doblado ».
            foreach (var e in Module01Library.All)
            {
                if (e.NeedsMarker) continue;
                Assert.IsTrue(File.Exists(e.Model), $"{e.LibraryId} apunta a {e.Model}, que no existe.");
            }
        }

        /// <summary>
        /// Lo que arte entregó en la carpeta de esenciales el 26 y el 27-10: las gafas de sol de
        /// montura negra, la mochila y un candado con textura. El elemento es ese FBX, no el
        /// arte viejo que hacía de él.
        /// </summary>
        [TestCase("O08", "gafas de sol.fbx")]
        [TestCase("O09", "mochila.fbx")]
        [TestCase("O11", "candado.fbx")]
        // Los cinco que entregó el artista el 2026-10-27 («faltan 10»).
        [TestCase("O12", "kit compacto.fbx")]
        [TestCase("O14", "soporte celular.fbx")]
        [TestCase("O15", "timbre bici.fbx")]
        [TestCase("O18", "termo.fbx")]
        [TestCase("H03", "diadema.fbx")]
        // Los del 2026-10-28 («listo»).
        [TestCase("O13", "herramienta.fbx")]
        [TestCase("O19", "alforja.fbx")]
        [TestCase("H04", "Crocs.fbx")]
        [TestCase("H08", "guante voluminoso.fbx")]
        [TestCase("H13", "audifonos ipod.fbx")]
        [TestCase("P06", "gafas_deportivas.fbx")]
        public void Los_EPP_nuevos_de_esenciales_usan_su_modelo(string libraryId, string file)
        {
            Module01Library.Entry e = Module01Library.All.First(x => x.LibraryId == libraryId);

            Assert.AreEqual(Module01Library.Essentials + file, e.Model);
        }

        /// <summary>
        /// Las gafas dañadas son las de seguridad de O07 con el material rayado (artista,
        /// 2026-10-28): misma malla, otro material, y nunca el material limpio.
        /// </summary>
        [Test]
        public void Las_gafas_danadas_son_las_de_seguridad_con_el_material_rayado()
        {
            Module01Library.Entry damaged = Module01Library.All.First(x => x.LibraryId == "H07");
            Module01Library.Entry clean = Module01Library.All.First(x => x.LibraryId == "O07");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(damaged.Model);
            Material scratched = AssetDatabase.LoadAssetAtPath<Material>(Module01DamagedGlasses.MaterialPath);

            Assert.IsNotNull(prefab, damaged.Model);
            Assert.IsNotNull(scratched, Module01DamagedGlasses.MaterialPath);
            Assert.AreEqual(clean.Model, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab)),
                "el mismo modelo que las gafas buenas");
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers);
            foreach (Renderer r in renderers)
                CollectionAssert.AreEqual(Enumerable.Repeat(scratched, r.sharedMaterials.Length), r.sharedMaterials, r.name);
        }

        [Test]
        public void Cada_elemento_va_a_una_sola_parada()
        {
            var all = Module01Library.Stops.SelectMany(s => s.ItemIds).ToList();

            CollectionAssert.AllItemsAreUnique(all, "un elemento en dos paradas");
            CollectionAssert.AreEquivalent(Module01Library.All.Select(e => e.ItemId), all,
                "cada elemento de la hoja tiene que estar en una parada, o no se ve en el recorrido");
        }

        [Test]
        public void Cada_parada_tiene_su_mueble_y_no_choca_con_el_vehiculo()
        {
            CollectionAssert.AllItemsAreUnique(Module01Library.Stops.Select(s => s.Id));
            var vehicle = VehicleProfiles.AllInspectionZones().Select(z => z.ZoneId).ToList();
            foreach (Module01Library.Stop stop in Module01Library.Stops)
            {
                Assert.IsTrue(Module01Stations.TryGet(stop.Id, out _), $"'{stop.Id}' no tiene mueble en Module01Stations");
                CollectionAssert.DoesNotContain(vehicle, stop.Id, "colisionaría en los ids de entidad");
                StringAssert.StartsWith("M1/Stop_", stop.TitleKey);
            }
        }

        [Test]
        public void Cuantos_esperan_modelo_todavia()
        {
            // No es un límite, es un recordatorio con número: estos son los que van con «!»
            // hasta que 3D los entregue.
            List<string> sinModelo = new List<string>();
            foreach (var e in Module01Library.All)
                if (e.NeedsMarker)
                    sinModelo.Add(e.LibraryId);

            // 16 -> 14 el 2026-10-22: al revisar los modelos uno a uno aparecieron dos que
            // nadie habia mapeado. `rompe veintos` no era «otro rompevientos» sino una chaqueta
            // amarillo fluor con bandas reflectivas (O01), y las `gafas` de siempre son oscuras,
            // asi que son las de sol (O08) y no las transparentes.
            // 14 -> 13 el 2026-10-27: la mochila (O09) llegó en la carpeta de esenciales.
            // 13 -> 10 el 2026-10-28: kit, soporte, timbre y termo; el commit del artista dice
            // «faltan 10». Los audífonos ya tenían modelo y cambiaron a la diadema.
            // 10 -> 3 el 2026-10-28: café, alforja, AirPods, mitones, paraguas, gorra y poncho.
            // Faltan la correa cortada (H01), las gafas dañadas (H07) y la bolsa (H09), que tiene
            // FBX pero el artista no la colocó.
            // 3 -> 0 el 2026-10-28: H07 son las gafas deportivas, H09 la bolsa (que se coloca aquí)
            // y H01 el casco retro sin correa, a la espera del bueno.
            Assert.AreEqual(0, sinModelo.Count,
                "Si 3D entrega uno, baja el número: actualiza este test y la tabla. Faltan: "
                + string.Join(", ", sinModelo));
        }
    }
}
