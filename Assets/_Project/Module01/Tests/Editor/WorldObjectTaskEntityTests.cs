using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Tests
{
    public class WorldObjectTaskEntityTests
    {
        private GameObject _go;
        private WorldObjectTaskEntity _entity;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("prop", typeof(BoxCollider));
            _go.SetActive(false);
            _entity = _go.AddComponent<WorldObjectTaskEntity>();
            _entity.Configure(new EntityId("module01.head.helmet_ok"), "helmet_ok");
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private static PointerEventData Pointer() => new PointerEventData(EventSystem.current);

        [Test]
        public void Configure_fija_el_id_y_el_elemento()
        {
            Assert.AreEqual("module01.head.helmet_ok", _entity.Id.Value);
            Assert.AreEqual("helmet_ok", _entity.ItemId);
        }

        [Test]
        public void Apuntar_y_salir_cambia_el_foco_una_sola_vez_por_transicion()
        {
            int changes = 0;
            _entity.FocusChanged += (e, focused) => changes++;

            PointerEventData pointer = Pointer();
            _entity.OnPointerEnter(pointer);
            Assert.IsTrue(_entity.IsFocused);
            _entity.OnPointerExit(pointer);
            Assert.IsFalse(_entity.IsFocused);

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void Dos_punteros_mantienen_el_foco_hasta_que_salen_los_dos()
        {
            PointerEventData a = Pointer();
            PointerEventData b = Pointer();

            _entity.OnPointerEnter(a);
            _entity.OnPointerEnter(b);
            _entity.OnPointerExit(a);

            Assert.IsTrue(_entity.IsFocused, "Sigue habiendo un puntero dentro.");

            _entity.OnPointerExit(b);
            Assert.IsFalse(_entity.IsFocused);
        }

        [Test]
        public void SetSelected_avisa_solo_cuando_el_estado_cambia()
        {
            int changes = 0;
            _entity.SelectionChanged += (e, selected) => changes++;

            _entity.SetSelected(true);
            _entity.SetSelected(true);
            _entity.SetSelected(false);

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void Confirm_levanta_SelectionConfirmed()
        {
            int confirms = 0;
            _entity.SelectionConfirmed += e => confirms++;

            _entity.Confirm();

            Assert.AreEqual(1, confirms);
        }
    }
}
