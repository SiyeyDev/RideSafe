using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    public class ItemInspectorTests : CatalogFixture
    {
        private GameObject _go;
        private ItemInspector _inspector;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("inspector");
            _inspector = _go.AddComponent<ItemInspector>();
        }

        [TearDown]
        public void TearDownInspector() => Object.DestroyImmediate(_go);

        [Test]
        public void Present_deja_el_elemento_en_curso()
        {
            SafetyItemSO item = Item("helmet_ok", SafetyItemCategory.Core);

            _inspector.Present(item);

            Assert.IsTrue(_inspector.IsPresenting);
            Assert.AreSame(item, _inspector.Current);
        }

        [Test]
        public void Accept_avisa_una_vez_y_cierra_la_presentacion()
        {
            SafetyItemSO item = Item("helmet_ok", SafetyItemCategory.Core);
            int accepted = 0;
            _inspector.Accepted += i => accepted++;

            _inspector.Present(item);
            _inspector.Accept();

            Assert.AreEqual(1, accepted);
            Assert.IsFalse(_inspector.IsPresenting);
        }

        [Test]
        public void Decline_no_registra_nada_y_cierra_la_presentacion()
        {
            SafetyItemSO item = Item("helmet_cracked", SafetyItemCategory.Inappropriate);
            int accepted = 0;
            int declined = 0;
            _inspector.Accepted += i => accepted++;
            _inspector.Declined += i => declined++;

            _inspector.Present(item);
            _inspector.Decline();

            Assert.AreEqual(0, accepted);
            Assert.AreEqual(1, declined);
            Assert.IsFalse(_inspector.IsPresenting);
        }

        [Test]
        public void Accept_sin_nada_presentado_no_hace_nada()
        {
            int accepted = 0;
            _inspector.Accepted += i => accepted++;

            _inspector.Accept();

            Assert.AreEqual(0, accepted);
        }

        [Test]
        public void Present_con_null_no_abre_presentacion()
        {
            _inspector.Present(null);

            Assert.IsFalse(_inspector.IsPresenting);
            Assert.IsNull(_inspector.Current);
        }
    }
}
