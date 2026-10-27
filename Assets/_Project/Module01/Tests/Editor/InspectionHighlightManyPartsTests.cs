using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// La e-bike nueva (2026-10-21) trae frenos, ruedas y luces en <b>dos</b> piezas cada uno:
    /// <c>freno_de</c> y <c>freno_iz</c>, <c>llanta_delantera</c> y <c>rueda_tracera</c>,
    /// <c>luz_delantera</c> y <c>luz_tracera</c>. Cindy pidió delinear las dos: revisar los frenos
    /// es revisar los dos, y delinear uno solo enseña que el otro no cuenta.
    /// </summary>
    public class InspectionHighlightManyPartsTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private InspectionHighlight _highlight;
        private Transform _rear, _front;

        private static readonly Color Active = new Color(1f, .42f, .05f, 1f);
        private static readonly Color Resolved = new Color(.46f, .83f, .54f, .9f);

        [SetUp]
        public void SetUp()
        {
            Mesh mesh = TestMeshes.Tetrahedron();
            _objects.Add(mesh);
            Material material = new Material(Shader.Find("Unlit/Color"));
            _objects.Add(material);
            _rear = Part("rueda_tracera", new Vector3(1f, 0f, 0f), mesh);
            _front = Part("llanta_delantera", new Vector3(-1f, 0f, 0f), mesh);

            GameObject point = new GameObject("point_wheels");
            _objects.Add(point);
            _highlight = point.AddComponent<InspectionHighlight>();
            _highlight.ConfigureForTests(material, Active, Resolved);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _objects)
                Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private Transform Part(string name, Vector3 position, Mesh mesh)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            _objects.Add(go);
            go.transform.position = position;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            return go.transform;
        }

        [Test]
        public void Cada_pieza_lleva_su_propio_contorno()
        {
            Assert.IsTrue(_highlight.BindParts(new[] { _rear, _front }));

            Assert.IsNotNull(_rear.Find(InspectionHighlight.OutlineName), "la rueda de atrás, delineada");
            Assert.IsNotNull(_front.Find(InspectionHighlight.OutlineName), "y la de delante también");
            CollectionAssert.AreEqual(new[] { _rear, _front }, _highlight.Parts);
            Assert.AreSame(_rear, _highlight.Part, "la primera manda: ahí va la esfera del punto");
        }

        [Test]
        public void El_punto_en_revision_enciende_todos_sus_contornos_en_naranja()
        {
            _highlight.BindParts(new[] { _rear, _front });
            _highlight.SetHighlighted(true);

            foreach (Transform part in new[] { _rear, _front })
            {
                MeshRenderer outline = part.Find(InspectionHighlight.OutlineName).GetComponent<MeshRenderer>();
                Assert.IsTrue(outline.enabled, part.name);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                outline.GetPropertyBlock(block);
                Color color = block.GetColor("_OutlineColor");
                Assert.AreEqual(0f, Vector4.Distance(Active, color), 1e-3f, part.name);
            }
        }

        [Test]
        public void Cada_pieza_tiene_su_area_de_clic_encima()
        {
            _highlight.BindParts(new[] { _rear, _front });

            List<Vector3> areas = new List<Vector3>();
            foreach (Transform child in _highlight.transform)
                if (child.name == InspectionHighlight.PartAreaName)
                    areas.Add(child.position);
            Assert.AreEqual(2, areas.Count, "una por pieza: una caja que abarcara las dos ruedas taparía toda la bici");
            CollectionAssert.Contains(areas, _rear.position);
            CollectionAssert.Contains(areas, _front.position);
        }

        [Test]
        public void Soltar_se_lleva_todos_los_contornos_y_las_areas()
        {
            _highlight.BindParts(new[] { _rear, _front });
            _highlight.Release();

            Assert.IsNull(_rear.Find(InspectionHighlight.OutlineName));
            Assert.IsNull(_front.Find(InspectionHighlight.OutlineName));
            Assert.IsNull(_highlight.transform.Find(InspectionHighlight.PartAreaName));
            Assert.IsEmpty(_highlight.Parts);
        }

        [Test]
        public void Una_pieza_sin_malla_no_deja_sin_contorno_a_las_demas()
        {
            GameObject empty = new GameObject("hueco");
            _objects.Add(empty);

            Assert.IsTrue(_highlight.BindParts(new[] { empty.transform, _front }));

            CollectionAssert.AreEqual(new[] { _front }, _highlight.Parts);
            Assert.AreSame(_front, _highlight.Part);
        }

        /// <summary>
        /// El <c>Manubrio</c> del scooter y la <c>pantalla</c> de la e-bike traen dos materiales.
        /// Con un solo material en el contorno, Unity solo dibuja la primera submalla y la
        /// pantalla se queda sin contorno.
        /// </summary>
        [Test]
        public void El_contorno_cubre_todas_las_submallas_de_la_pieza()
        {
            Mesh twoParts = new Mesh { subMeshCount = 2 };
            _objects.Add(twoParts);
            twoParts.SetVertices(new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward });
            twoParts.SetTriangles(new[] { 0, 1, 2 }, 0);
            twoParts.SetTriangles(new[] { 0, 2, 3 }, 1);
            Transform handlebar = Part("Manubrio", Vector3.zero, twoParts);

            Assert.IsTrue(_highlight.BindParts(new[] { handlebar }));

            Material[] materials = handlebar.Find(InspectionHighlight.OutlineName)
                .GetComponent<MeshRenderer>().sharedMaterials;
            Assert.AreEqual(2, materials.Length, "Un material por submalla.");
            Assert.AreSame(materials[0], materials[1], "Los dos con el material del contorno.");
        }

        [Test]
        public void Una_pieza_de_un_solo_material_sigue_con_uno()
        {
            Assert.IsTrue(_highlight.BindParts(new[] { _rear }));
            Assert.AreEqual(1, _rear.Find(InspectionHighlight.OutlineName)
                .GetComponent<MeshRenderer>().sharedMaterials.Length);
        }
    }
}
