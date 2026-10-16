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

        [Test]
        public void La_pregunta_de_esta_presentacion_manda_sobre_la_de_la_zona()
        {
            // Un EPP ya elegido pregunta si quitarlo, no si añadirlo. Va por presentación y no
            // con SetQuestion porque esa es la de la zona entera y se queda puesta: pisarla
            // dejaría «¿quitarlo?» en el siguiente elemento, que no está elegido.
            _inspector.SetQuestion("M1/Confirm_Question");
            SafetyItemSO item = Item("helmet_ok", SafetyItemCategory.Core);

            _inspector.Present(item, null, "M1/Remove_Question");

            Assert.AreEqual("M1/Remove_Question", _inspector.QuestionKey);
        }

        [Test]
        public void Al_cerrar_la_presentacion_la_pregunta_vuelve_a_la_de_la_zona()
        {
            _inspector.SetQuestion("M1/Confirm_Question");
            SafetyItemSO item = Item("helmet_ok", SafetyItemCategory.Core);

            _inspector.Present(item, null, "M1/Remove_Question");
            _inspector.Decline();

            Assert.AreEqual("M1/Confirm_Question", _inspector.QuestionKey);
        }

        [Test]
        public void Sin_pregunta_propia_se_usa_la_de_la_zona()
        {
            _inspector.SetQuestion("M1/Inspect_Question");
            SafetyItemSO item = Item("point_brakes", SafetyItemCategory.Core);

            _inspector.Present(item);

            Assert.AreEqual("M1/Inspect_Question", _inspector.QuestionKey);
        }

        // ------------------------------------------------------------------
        // Lo que el pedestal enseña.
        //
        // Medido en la escena el 2026-10-16: instanciar el FBX del catalogo da una pose
        // DISTINTA a la del objeto que el arte dejo en la mesa en 12 de los 13 EPP —el casco
        // bueno y las gafas a 270 en Y, el jean ajustado a 270 en X y con escala 0.1—, y en los
        // pares (coderas, rodilleras) ensena una sola pieza de las dos. Por eso el pedestal
        // copia el objeto de la escena, no el asset.
        // ------------------------------------------------------------------

        /// <summary>Un objeto de mesa con <paramref name="meshes"/> mallas y una pose puesta por el arte.</summary>
        private Transform Mesa(int meshes, Quaternion rotation)
        {
            GameObject root = new GameObject("epp de la mesa");
            Track(root);
            root.transform.rotation = rotation;
            for (int i = 0; i < meshes; i++)
            {
                GameObject piece = new GameObject("pieza" + i, typeof(MeshFilter), typeof(MeshRenderer));
                piece.transform.SetParent(root.transform, false);
                piece.transform.localPosition = new Vector3(i * .2f, 0f, 0f);
                piece.GetComponent<MeshFilter>().sharedMesh = Cube;
            }
            return root.transform;
        }

        private Transform Stage()
        {
            GameObject stage = new GameObject("stage");
            Track(stage);
            _inspector.ConfigureForTests(stage.transform);
            return stage.transform;
        }

        [Test]
        public void El_pedestal_ensena_tantas_mallas_como_el_objeto_de_la_mesa()
        {
            // El par de coderas es UN elemento con DOS mallas. Hasta hoy el pedestal ensenaba
            // una codera suelta y la mesa un par, y eso es lo que el cliente vio distinto.
            Stage();
            SafetyItemSO item = Item("elbow_pads", SafetyItemCategory.Optional);

            _inspector.Present(item, Mesa(2, Quaternion.identity));

            Assert.AreEqual(2, _inspector.Presented.GetComponentsInChildren<MeshRenderer>(true).Length);
        }

        [Test]
        public void El_pedestal_conserva_la_pose_del_objeto_de_la_mesa()
        {
            // Un jean doblado que en la mesa se ve plano, instanciado desde el FBX sale de canto.
            Stage();
            SafetyItemSO item = Item("trousers_fitted", SafetyItemCategory.Core);
            Quaternion puesta = Quaternion.Euler(270f, 0f, 0f);

            _inspector.Present(item, Mesa(1, puesta));

            Quaternion copia = _inspector.Presented.GetComponentInChildren<MeshRenderer>().transform.rotation;
            Assert.AreEqual(0f, Quaternion.Angle(puesta, copia), .5f,
                "La copia del pedestal tiene que verse como el objeto de la mesa.");
        }

        [Test]
        public void El_objeto_de_la_mesa_manda_sobre_el_prefab_del_catalogo()
        {
            Stage();
            SafetyItemSO item = Item("helmet_ok", SafetyItemCategory.Core);
            GameObject prefab = new GameObject("prefab del catalogo", typeof(MeshFilter), typeof(MeshRenderer));
            Track(prefab);
            prefab.GetComponent<MeshFilter>().sharedMesh = Cube;
            item.ConfigureDisplayForTests(prefab);

            _inspector.Present(item, Mesa(2, Quaternion.identity));

            Assert.AreEqual(2, _inspector.Presented.GetComponentsInChildren<MeshRenderer>(true).Length,
                "Con objeto en la escena, el prefab del catalogo no se usa.");
        }

        [Test]
        public void Sin_mallas_visibles_el_pedestal_se_queda_con_el_signo()
        {
            // Los cinco puntos del vehiculo son esferas de clic con el renderer apagado: de ahi
            // no se puede copiar nada, y el signo es lo correcto.
            Stage();
            SafetyItemSO item = Item("point_brakes", SafetyItemCategory.Core);
            Transform esfera = Mesa(1, Quaternion.identity);
            esfera.GetComponentInChildren<MeshRenderer>().enabled = false;

            _inspector.Present(item, esfera);

            Assert.IsFalse(_inspector.PresentsSolid,
                "Sin nada que copiar y sin prefab, lo que toca es el signo de exclamacion.");
        }

        private static Mesh _cube;

        private static Mesh Cube
        {
            get
            {
                if (_cube == null)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cube = cube.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(cube);
                }
                return _cube;
            }
        }
    }
}
