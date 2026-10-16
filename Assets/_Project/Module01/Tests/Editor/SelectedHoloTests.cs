using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// El verde de «ya seleccionado» sobre el EPP de la mesa. Lo pidió el cliente: un elemento
    /// elegido deja de desaparecer y se queda a la vista marcado, para poder volver a pulsarlo
    /// y quitarlo.
    /// <para>
    /// Se monta como <b>hijos con las mismas mallas</b> y apagando los renderers originales, el
    /// mismo camino que <see cref="InspectionHighlight"/>, para no tocar ni un material del
    /// arte: cambiarlos en caliente es lo que deja regresiones que no salen en un diff.
    /// </para>
    /// </summary>
    public class SelectedHoloTests
    {
        private readonly System.Collections.Generic.List<Object> _created =
            new System.Collections.Generic.List<Object>();

        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = new Material(Shader.Find("Unlit/Color"));
            _created.Add(_material);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
                if (o != null)
                    Object.DestroyImmediate(o);
            _created.Clear();
        }

        /// <summary>Un objeto con <paramref name="meshes"/> mallas, como los entrega el arte.</summary>
        private GameObject Prop(int meshes)
        {
            GameObject root = new GameObject("epp");
            _created.Add(root);
            for (int i = 0; i < meshes; i++)
            {
                GameObject piece = new GameObject("pieza" + i, typeof(MeshFilter), typeof(MeshRenderer));
                piece.transform.SetParent(root.transform, false);
                piece.GetComponent<MeshFilter>().sharedMesh = Mesh;
            }
            return root;
        }

        private static Mesh _mesh;

        private static Mesh Mesh
        {
            get
            {
                if (_mesh == null)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _mesh = cube.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(cube);
                }
                return _mesh;
            }
        }

        private SelectedHolo Holo(GameObject go)
        {
            SelectedHolo holo = go.AddComponent<SelectedHolo>();
            holo.ConfigureForTests(_material, Color.green);
            return holo;
        }

        [Test]
        public void Al_encenderlo_el_objeto_queda_marcado()
        {
            SelectedHolo holo = Holo(Prop(1));

            holo.SetOn(true);

            Assert.IsTrue(holo.IsOn);
        }

        [Test]
        public void Nace_apagado()
        {
            Assert.IsFalse(Holo(Prop(1)).IsOn);
        }

        [Test]
        public void El_par_de_coderas_se_marca_entero()
        {
            // El v9 entrega coderas y rodilleras POR PARES, y el par es UN elemento del
            // catalogo. Marcar solo la primera malla deja media codera en verde.
            GameObject par = Prop(2);
            SelectedHolo holo = Holo(par);

            holo.SetOn(true);

            Assert.AreEqual(2, holo.Marks.Count,
                "El par son dos mallas: las dos tienen que ponerse verdes.");
        }

        [Test]
        public void Mientras_esta_marcado_no_se_dibuja_el_original()
        {
            // Si el original siguiera encendido, el holograma se mezclaria con el y no se
            // leeria como holograma, se leeria como un objeto con un tinte raro.
            GameObject prop = Prop(1);
            SelectedHolo holo = Holo(prop);

            holo.SetOn(true);

            Assert.IsFalse(prop.GetComponentInChildren<MeshRenderer>().enabled);
        }

        [Test]
        public void Al_apagarlo_el_original_vuelve_a_dibujarse()
        {
            GameObject prop = Prop(1);
            SelectedHolo holo = Holo(prop);

            holo.SetOn(true);
            holo.SetOn(false);

            Assert.IsTrue(prop.GetComponentInChildren<MeshRenderer>().enabled);
            Assert.AreEqual(0, holo.Marks.Count, "Al apagarlo no debe quedar geometria suelta.");
        }

        [Test]
        public void Encenderlo_dos_veces_no_duplica_la_geometria()
        {
            SelectedHolo holo = Holo(Prop(1));

            holo.SetOn(true);
            holo.SetOn(true);

            Assert.AreEqual(1, holo.Marks.Count);
        }

        [Test]
        public void El_color_que_se_va_a_dibujar_es_el_configurado()
        {
            // Se lee del bloque de propiedades del renderer, no de un campo guardado: lo que
            // importa es lo que llega al shader.
            SelectedHolo holo = Holo(Prop(1));

            holo.SetOn(true);

            Assert.AreEqual(Color.green, holo.CurrentColor);
        }

        [Test]
        public void Un_objeto_sin_mallas_no_revienta()
        {
            // Los cinco puntos del vehiculo son esferas de clic con el renderer apagado.
            GameObject vacio = new GameObject("sin mallas");
            _created.Add(vacio);
            SelectedHolo holo = Holo(vacio);

            Assert.DoesNotThrow(() => holo.SetOn(true));
            Assert.AreEqual(0, holo.Marks.Count);
        }

        [Test]
        public void No_se_marca_a_si_mismo_al_reencenderse()
        {
            // La marca es un hijo con un MeshRenderer: si el barrido no la excluyera, la
            // segunda vuelta marcaria la marca.
            GameObject prop = Prop(1);
            SelectedHolo holo = Holo(prop);

            holo.SetOn(true);
            holo.SetOn(false);
            holo.SetOn(true);

            Assert.AreEqual(1, holo.Marks.Count);
        }
    }
}
