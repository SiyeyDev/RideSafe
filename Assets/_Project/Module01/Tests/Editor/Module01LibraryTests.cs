using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RideSafe.Module01.Editor;

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
            float total = 0f;
            int n = 0;
            foreach (var e in Of(SafetyItemCategory.Inappropriate)) { total += e.Weight; n++; }

            Assert.AreEqual(12, n, "Las 11 «peligrosa visible» más la gorra condicional.");
            Assert.AreEqual(255f, total, 0.01f, "235 de las visibles más los 20 de la gorra.");
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
        public void Los_dos_grupos_compartidos_de_la_hoja_estan()
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
            CollectionAssert.AreEquivalent(new[] { "H03", "H13" }, byGroup["auditory_occlusion"],
                "Audífonos y AirPods son el mismo error.");

            foreach (var kv in byGroup)
                if (kv.Key != "helmet_integrity" && kv.Key != "auditory_occlusion")
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
                StringAssert.IsMatch("^[EOHN][0-9]{2}$", e.LibraryId);
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
        public void Lo_retirado_no_puede_seguir_en_el_catalogo()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (var e in Module01Library.All) ids.Add(e.ItemId);

            foreach (string id in Module01Library.RetiredItemIds)
                Assert.IsFalse(ids.Contains(id),
                    $"{id} está en la lista de retirados y a la vez en el catálogo.");
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
            Assert.AreEqual(14, sinModelo.Count,
                "Si 3D entrega uno, baja el número: actualiza este test y la tabla. Faltan: "
                + string.Join(", ", sinModelo));
        }
    }
}
