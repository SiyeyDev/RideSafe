using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Ninguna esfera blanca, en ningún sitio.
    /// <para>
    /// Los cinco puntos de inspección del vehículo no tienen modelo propio —no existe «el
    /// freno» como asset—, y el inspector les fabricaba una <c>PrimitiveType.Sphere</c>: al
    /// pulsar «llantas» lo que giraba en el pedestal era una bola blanca. Charlie, 2026-10-14:
    /// «no quiero ver más esferas blancas; si en alguna no hay, usemos el `!`».
    /// </para>
    /// <para>
    /// Desde que los vehículos llegan despiezados, algunos de esos puntos <b>sí</b> tienen
    /// pieza real —los frenos, las ruedas, la dirección—, así que el pedestal puede enseñar la
    /// pieza de verdad. Los demás se
    /// quedan con el signo, que es el mismo idioma que ya usan en el mundo.
    /// </para>
    /// </summary>
    public class ItemInspectorFallbackTests : CatalogFixture
    {
        private GameObject _go;
        private GameObject _stage;
        private GameObject _part;
        private ItemInspector _inspector;
        private Mesh _mesh;
        private SafetyItemSO _sinModelo;

        [SetUp]
        public void SetUp()
        {
            _mesh = TestMeshes.Tetrahedron();
            _part = new GameObject("manubrio", typeof(MeshFilter), typeof(MeshRenderer));
            _part.GetComponent<MeshFilter>().sharedMesh = _mesh;

            _stage = new GameObject("ItemStage");
            _go = new GameObject("inspector");
            _inspector = _go.AddComponent<ItemInspector>();
            _inspector.ConfigureForTests(_stage.transform);

            _sinModelo = Item("point_brakes", SafetyItemCategory.Core);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_stage);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_mesh);
        }

        private static bool HayEsfera(GameObject root) => root
            .GetComponentsInChildren<MeshFilter>(true)
            .Any(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Sphere"));

        [Test]
        public void Un_elemento_sin_modelo_no_pone_una_esfera_en_el_pedestal()
        {
            _inspector.Present(_sinModelo, null);

            Assert.IsFalse(HayEsfera(_stage),
                "El pedestal sigue fabricando la bola blanca de siempre.");
        }

        [Test]
        public void Un_punto_sin_pieza_ensena_el_signo_de_exclamacion()
        {
            _inspector.Present(_sinModelo, null);

            TMP_Text glifo = _stage.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
            Assert.IsNotNull(glifo, "Sin pieza y sin signo, el pedestal se queda vacio.");
            Assert.AreEqual("!", glifo.text);
        }

        [Test]
        public void Un_punto_con_pieza_real_ensena_esa_pieza()
        {
            _inspector.Present(_sinModelo, _part.transform);

            MeshFilter[] mallas = _stage.GetComponentsInChildren<MeshFilter>(true);
            Assert.AreEqual(1, mallas.Length, "Deberia haber una sola copia de la pieza.");
            Assert.AreSame(_mesh, mallas[0].sharedMesh,
                "El pedestal tiene que ensenar la malla de la pieza, no otra cosa.");
            Assert.IsFalse(HayEsfera(_stage));
        }

        /// <summary>
        /// La pieza del vehículo sigue viva en el garaje: lo del pedestal es una copia. Si se
        /// moviera el original, el manubrio desaparecería de la bici al inspeccionarlo.
        /// </summary>
        [Test]
        public void La_pieza_del_vehiculo_no_se_mueve_al_inspeccionarla()
        {
            Transform padreAntes = _part.transform.parent;
            Vector3 sitioAntes = _part.transform.position;

            _inspector.Present(_sinModelo, _part.transform);

            Assert.AreSame(padreAntes, _part.transform.parent);
            Assert.AreEqual(sitioAntes, _part.transform.position);
        }

        /// <summary>
        /// El signo es texto plano: al dar media vuelta se queda de canto y luego del revés. Lo
        /// que gira en el pedestal es lo que tiene volumen.
        /// </summary>
        [Test]
        public void El_signo_no_gira_y_un_objeto_si()
        {
            _inspector.Present(_sinModelo, null);
            Assert.IsFalse(_inspector.PresentsSolid, "El signo no es un solido: no debe girar.");

            _inspector.Present(_sinModelo, _part.transform);
            Assert.IsTrue(_inspector.PresentsSolid, "La pieza real si gira, como cualquier EPP.");
        }

        [Test]
        public void Un_elemento_con_su_propio_modelo_sigue_ensenandolo()
        {
            SafetyItemSO conModelo = Item("helmet_ok", SafetyItemCategory.Core);
            conModelo.ConfigureDisplayForTests(_part);

            _inspector.Present(conModelo, null);

            Assert.AreSame(_mesh, _stage.GetComponentsInChildren<MeshFilter>(true)[0].sharedMesh);
        }
    }

    /// <summary>
    /// Los pantalones miden más de un metro: encima de la mesa quedarían como torres. Se
    /// plantan en el suelo, pero <b>sin salirse de su hueco en la fila</b>.
    /// </summary>
    public class FloorStandingTests
    {
        private static Bounds Mesa() =>
            new Bounds(new Vector3(-4.4f, .46f, 3.2f), new Vector3(.88f, .92f, 2.6f));

        private static readonly Vector3 Pantalon = new Vector3(.53f, 1.16f, .33f);

        [Test]
        public void Un_elemento_alto_se_apoya_en_el_suelo_y_no_en_la_tapa()
        {
            Vector3 enLaTapa = new Vector3(-4.4f, .92f + .58f, 3.0f);

            Vector3 sitio = StationLayout.OnFloor(enLaTapa, Pantalon, Mesa(), .04f, Vector3.right, .30f);

            Assert.AreEqual(.04f, sitio.y - Pantalon.y * .5f, .001f, "La base tiene que tocar el suelo.");
        }

        [Test]
        public void Un_elemento_alto_conserva_su_hueco_en_la_fila()
        {
            Vector3 enLaTapa = new Vector3(-4.4f, 1.5f, 3.0f);

            Vector3 sitio = StationLayout.OnFloor(enLaTapa, Pantalon, Mesa(), .04f, Vector3.right, .30f);

            Assert.AreEqual(3.0f, sitio.z, .001f,
                "Si se sale de su hueco, se planta delante de otro elemento y lo tapa.");
        }

        [Test]
        public void Un_elemento_alto_no_se_mete_dentro_del_mueble()
        {
            Vector3 sitio = StationLayout.OnFloor(
                new Vector3(-4.4f, 1.5f, 3.0f), Pantalon, Mesa(), .04f, Vector3.right, .30f);

            Assert.GreaterOrEqual(sitio.x - Pantalon.x * .5f, Mesa().max.x + .30f - .001f,
                "Se queda por delante de la cara libre del mueble, no dentro de los cajones.");
        }

        /// <summary>
        /// Lo que decide si algo va al suelo es su altura, no su nombre: una entrega nueva puede
        /// traer otra prenda alta y tiene que caer sola en el sitio correcto.
        /// </summary>
        [Test]
        public void Se_va_al_suelo_lo_que_no_cabe_de_pie_sobre_la_tapa()
        {
            Assert.IsTrue(StationLayout.StandsOnFloor(Pantalon),
                "Un pantalon de 1.16 m encima de la mesa es una torre.");
            Assert.IsFalse(StationLayout.StandsOnFloor(new Vector3(.45f, .60f, .30f)),
                "El chaleco mide 60 cm: va en la tapa.");
            Assert.IsFalse(StationLayout.StandsOnFloor(new Vector3(.30f, .15f, .35f)),
                "Las botas van en la tapa.");
        }
    }
}
