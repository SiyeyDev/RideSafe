using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Una malla de verdad para las piezas de prueba. Tiene que tener volumen: el escenario le
    /// pone a la pieza delineada un <c>MeshCollider</c> convexo para que reciba el clic, y uno
    /// convexo sobre una malla sin vértices se queja.
    /// </summary>
    internal static class TestMeshes
    {
        public static Mesh Tetrahedron()
        {
            Mesh mesh = new Mesh { name = "pieza" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 3, 1, 2, 3 };
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    /// <summary>
    /// Qué pieza de la malla le toca a cada punto de inspección es <b>dato del perfil</b>, no
    /// una referencia de escena: una sola entidad por punto sirve a los dos vehículos, y cada
    /// vehículo despieza su malla a su manera, y no coinciden —la e-bike trae <c>manubrio</c>
    /// y <c>rueda_trasera</c>; el patinete, <c>control_de_direccion</c>, <c>freno</c> y
    /// <c>rueda_tracera</c>—.
    /// </summary>
    public class VehiclePartBindingTests
    {
        private VehicleProfileSO _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<VehicleProfileSO>();
            _profile.Configure("ebike", null, new List<ZoneSO>());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_profile);

        [Test]
        public void Un_perfil_declara_que_pieza_delinea_cada_punto()
        {
            _profile.ConfigurePartBindings(new[]
            {
                ("point_steering", "manubrio"),
                ("point_wheels", "rueda_trasera"),
            });

            Assert.AreEqual("manubrio", _profile.PartNameFor("point_steering"));
            Assert.AreEqual("rueda_trasera", _profile.PartNameFor("point_wheels"));
        }

        [Test]
        public void Un_punto_sin_pieza_declarada_no_tiene_nombre_de_pieza()
        {
            _profile.ConfigurePartBindings(new[] { ("point_steering", "manubrio") });

            Assert.AreEqual(string.Empty, _profile.PartNameFor("point_lights"),
                "Los puntos sin pieza real —freno, batería, faro— caen al signo de exclamación.");
            Assert.AreEqual(string.Empty, _profile.PartNameFor(null));
        }

        [Test]
        public void El_id_del_punto_se_compara_sin_espacios_ni_mayusculas()
        {
            _profile.ConfigurePartBindings(new[] { ("point_steering", "manubrio") });

            Assert.AreEqual("manubrio", _profile.PartNameFor("  Point_Steering "));
        }
    }

    /// <summary>
    /// El delineado de la pieza que toca revisar, que es lo que la reunión pidió y hasta ahora
    /// no se podía hacer: las mallas que entregó el equipo de arte eran una pieza única.
    /// <para>
    /// El contorno es un <b>hijo</b> con la misma malla y un material de casco invertido, nunca
    /// un material añadido al <c>MeshRenderer</c> del arte: así cambiar de vehículo no deja que
    /// restaurar nada, y las piezas con dos submallas no se delinean a medias.
    /// </para>
    /// </summary>
    public class InspectionHighlightTests
    {
        private GameObject _point;
        private GameObject _part;
        private InspectionHighlight _highlight;
        private Material _material;
        private Mesh _mesh;

        private static readonly Color Active = new Color(1f, .42f, .05f, 1f);
        private static readonly Color Resolved = new Color(.46f, .83f, .54f, .9f);

        [SetUp]
        public void SetUp()
        {
            _mesh = TestMeshes.Tetrahedron();
            _material = new Material(Shader.Find("Unlit/Color"));

            _part = new GameObject("manubrio", typeof(MeshFilter), typeof(MeshRenderer));
            _part.GetComponent<MeshFilter>().sharedMesh = _mesh;

            _point = new GameObject("point_steering");
            _highlight = _point.AddComponent<InspectionHighlight>();
            _highlight.ConfigureForTests(_material, Active, Resolved);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_point);
            Object.DestroyImmediate(_part);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_mesh);
        }

        /// <summary>
        /// Por canal y con margen: el color va y vuelve por un <c>MaterialPropertyBlock</c>, y
        /// comparar <c>Color</c> con igualdad exacta falla por el último bit aunque los cuatro
        /// canales impriman lo mismo.
        /// </summary>
        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, .001f, "canal rojo");
            Assert.AreEqual(expected.g, actual.g, .001f, "canal verde");
            Assert.AreEqual(expected.b, actual.b, .001f, "canal azul");
            Assert.AreEqual(expected.a, actual.a, .001f, "canal alfa");
        }

        [Test]
        public void Delinear_una_pieza_le_cuelga_un_contorno_con_su_misma_malla()
        {
            Assert.IsTrue(_highlight.Bind(_part.transform));

            Assert.IsNotNull(_highlight.Outline, "Sin contorno no hay nada que ver.");
            Assert.AreSame(_part.transform, _highlight.Outline.transform.parent,
                "El contorno cuelga de la pieza para viajar con ella cuando el vehículo gira.");
            Assert.AreSame(_mesh, _highlight.Outline.GetComponent<MeshFilter>().sharedMesh);
        }

        // ------------------------------------------------------------------
        // Toda la pieza clicable. Prueba en Quest 2026-10-16: en «Ruedas y estructura» brillaba
        // la rueda entera y lo unico que respondia era una esfera invisible de 15 cm en el
        // borde de la llanta.
        // ------------------------------------------------------------------

        private BoxCollider Area(InspectionHighlight highlight) =>
            highlight.transform.Find(InspectionHighlight.PartAreaName)?.GetComponent<BoxCollider>();

        [Test]
        public void Delinear_la_pieza_le_da_un_area_de_clic_del_tamano_de_la_pieza()
        {
            _part.transform.position = new Vector3(1f, 2f, 3f);
            _part.transform.localScale = Vector3.one * 2f;
            _highlight.Bind(_part.transform);

            BoxCollider area = Area(_highlight);
            Assert.IsNotNull(area, "Sin area, lo que brilla no se puede tocar.");
            Assert.AreSame(_point.transform, area.transform.parent,
                "Cuelga del punto: es el punto el que recibe el clic.");
            // Por la transformada y no por area.bounds: fuera de Play la fisica no se entera de
            // que el collider se movio y devuelve la caja de antes.
            Bounds world = _part.GetComponent<MeshRenderer>().bounds;
            Vector3 centre = area.transform.TransformPoint(area.center);
            Vector3 size = Vector3.Scale(area.size, area.transform.lossyScale);
            Assert.AreEqual(world.center.x, centre.x, .01f, "centro");
            Assert.AreEqual(world.size.y, size.y, .01f, "alto");
        }

        [Test]
        public void El_area_se_queda_sobre_la_pieza_aunque_el_punto_se_mueva_despues()
        {
            // VehicleStage.MoveToPart mueve el punto encima de la pieza DESPUES de Bind. En la
            // escena el area acabo 0,83 m al lado de la rueda.
            _highlight.Bind(_part.transform);
            _point.transform.SetParent(_part.transform, true);
            _point.transform.position += new Vector3(.83f, 0f, 0f);

            _highlight.SetHighlighted(true);

            BoxCollider area = Area(_highlight);
            Vector3 centre = area.transform.TransformPoint(area.center);
            Assert.AreEqual(_part.GetComponent<MeshRenderer>().bounds.center.x, centre.x, .01f);
        }

        [Test]
        public void El_area_solo_responde_mientras_el_punto_es_el_unico_por_revisar()
        {
            _highlight.Bind(_part.transform);
            Assert.IsFalse(Area(_highlight).enabled, "Un punto que no toca no ocupa la pieza.");

            _highlight.SetHighlighted(true);
            Assert.IsTrue(Area(_highlight).enabled);

            _highlight.SetObscured(true);
            Assert.IsFalse(Area(_highlight).enabled, "Tapado por un panel no se toca.");
            _highlight.SetObscured(false);

            _highlight.SetResolved(true);
            Assert.IsFalse(Area(_highlight).enabled, "Resuelto vuelve a su esfera.");
        }

        [Test]
        public void Con_varios_puntos_a_la_vez_cada_uno_se_queda_con_su_esfera()
        {
            // La cabina: el manubrio es todo el tren delantero y su area enterraria los frenos.
            GameObject otherPart = new GameObject("rueda", typeof(MeshFilter), typeof(MeshRenderer));
            otherPart.GetComponent<MeshFilter>().sharedMesh = _mesh;
            GameObject otherPoint = new GameObject("point_brakes");
            try
            {
                InspectionHighlight other = otherPoint.AddComponent<InspectionHighlight>();
                other.ConfigureForTests(_material, Active, Resolved);
                _highlight.Bind(_part.transform);
                other.Bind(otherPart.transform);

                _highlight.SetHighlighted(true);
                other.SetHighlighted(true);

                Assert.IsFalse(Area(_highlight).enabled);
                Assert.IsFalse(Area(other).enabled);

                other.SetResolved(true);
                Assert.IsTrue(Area(_highlight).enabled, "Solo queda uno por revisar: toda su pieza responde.");
            }
            finally
            {
                Object.DestroyImmediate(otherPoint);
                Object.DestroyImmediate(otherPart);
            }
        }

        [Test]
        public void Soltar_la_pieza_se_lleva_el_area()
        {
            _highlight.Bind(_part.transform);
            _highlight.Release();

            Assert.IsNull(Area(_highlight));
        }

        [Test]
        public void El_contorno_nace_apagado_porque_el_punto_todavia_no_toca()
        {
            _highlight.Bind(_part.transform);

            Assert.IsFalse(_highlight.Outline.enabled,
                "Delinear las cinco piezas a la vez no señala ninguna.");
        }

        [Test]
        public void El_punto_en_revision_enciende_el_contorno_en_naranja()
        {
            _highlight.Bind(_part.transform);

            _highlight.SetHighlighted(true);

            Assert.IsTrue(_highlight.Outline.enabled);
            AssertColor(Active, _highlight.CurrentColor);
        }

        [Test]
        public void Un_punto_resuelto_deja_el_contorno_en_verde_y_visible()
        {
            _highlight.Bind(_part.transform);
            _highlight.SetHighlighted(true);

            _highlight.SetResolved(true);

            Assert.IsTrue(_highlight.Outline.enabled, "Lo ya revisado se queda marcado, como el signo.");
            AssertColor(Resolved, _highlight.CurrentColor);
        }

        [Test]
        public void Un_punto_de_otra_zona_no_se_delinea()
        {
            _highlight.Bind(_part.transform);
            _highlight.SetHighlighted(true);

            _highlight.SetHighlighted(false);

            Assert.IsFalse(_highlight.Outline.enabled);
        }

        [Test]
        public void Tapar_el_punto_apaga_el_contorno_aunque_este_en_revision()
        {
            _highlight.Bind(_part.transform);
            _highlight.SetHighlighted(true);

            _highlight.SetObscured(true);

            Assert.IsFalse(_highlight.Outline.enabled,
                "Con un objeto en el pedestal, el popup manda y el garaje calla.");
        }

        [Test]
        public void Una_pieza_sin_malla_no_se_puede_delinear_y_lo_dice()
        {
            GameObject hueco = new GameObject("marco");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("marco"));
            bool bound = _highlight.Bind(hueco.transform);

            Assert.IsFalse(bound, "Sin malla no hay contorno; el punto tiene que caer al signo.");
            Assert.IsNull(_highlight.Outline);

            Object.DestroyImmediate(hueco);
        }

        [Test]
        public void Delinear_sin_pieza_no_revienta_y_devuelve_falso()
        {
            Assert.IsFalse(_highlight.Bind(null));
            Assert.IsNull(_highlight.Outline);
        }

        /// <summary>
        /// Al cambiar de vehículo el punto se recuelga de la otra malla. Si el contorno viejo
        /// se quedara, la bici apagada seguiría con su manubrio delineado —y se vería, porque
        /// apagar la malla del vehículo no apaga un hijo que no cuelgue de ella—.
        /// </summary>
        [Test]
        public void Soltar_la_pieza_se_lleva_el_contorno()
        {
            _highlight.Bind(_part.transform);

            _highlight.Release();

            Assert.IsNull(_highlight.Outline);
            Assert.AreEqual(0, _part.transform.childCount,
                "La pieza del arte tiene que quedar como estaba.");
        }

        [Test]
        public void Delinear_dos_veces_no_deja_dos_contornos()
        {
            _highlight.Bind(_part.transform);
            _highlight.Bind(_part.transform);

            Assert.AreEqual(1, _part.transform.childCount);
        }
    }
}
