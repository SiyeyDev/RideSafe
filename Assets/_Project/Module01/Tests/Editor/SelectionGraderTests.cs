using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Base de los tests que necesitan catálogo. Los ScriptableObject se crean en
    /// memoria y se destruyen al terminar, así ningún test toca el disco.
    /// </summary>
    public abstract class CatalogFixture
    {
        private readonly List<Object> _created = new List<Object>();

        protected SafetyItemSO Item(string id, SafetyItemCategory category)
        {
            SafetyItemSO item = ScriptableObject.CreateInstance<SafetyItemSO>();
            item.ConfigureForTests(id, "Module1/" + id, "Module1/" + id + "_Risk", category);
            _created.Add(item);
            return item;
        }

        protected ZoneSO Zone(string id, params SafetyItemSO[] items)
        {
            ZoneSO zone = ScriptableObject.CreateInstance<ZoneSO>();
            zone.ConfigureForTests(id, "Module1/Zone_" + id, items);
            _created.Add(zone);
            return zone;
        }

        protected Module01CatalogSO Catalog(params ZoneSO[] zones)
        {
            Module01CatalogSO catalog = ScriptableObject.CreateInstance<Module01CatalogSO>();
            catalog.ConfigureForTests(zones);
            _created.Add(catalog);
            return catalog;
        }

        [TearDown]
        public void DestroyCreatedAssets()
        {
            foreach (Object o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }
    }

    public class CatalogTests : CatalogFixture
    {
        [Test]
        public void AllItems_devuelve_los_elementos_en_orden_de_zona()
        {
            Module01CatalogSO catalog = Catalog(
                Zone("head", Item("helmet_ok", SafetyItemCategory.Core)),
                Zone("load", Item("bag_ok", SafetyItemCategory.Core)));

            CollectionAssert.AreEqual(
                new[] { "helmet_ok", "bag_ok" },
                new List<SafetyItemSO>(catalog.AllItems).ConvertAll(i => i.ItemId));
        }

        [Test]
        public void TryGetItem_encuentra_por_id_y_falla_con_id_desconocido()
        {
            Module01CatalogSO catalog = Catalog(Zone("head", Item("helmet_ok", SafetyItemCategory.Core)));

            Assert.IsTrue(catalog.TryGetItem("helmet_ok", out SafetyItemSO found));
            Assert.AreEqual("helmet_ok", found.ItemId);
            Assert.IsFalse(catalog.TryGetItem("no_existe", out _));
        }

        [Test]
        public void Validate_reporta_ids_duplicados()
        {
            Module01CatalogSO catalog = Catalog(
                Zone("head", Item("helmet_ok", SafetyItemCategory.Core)),
                Zone("load", Item("helmet_ok", SafetyItemCategory.Core)));

            List<string> problems = catalog.Validate();

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("helmet_ok", problems[0]);
        }
    }
}
