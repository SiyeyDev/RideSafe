using System.Reflection;
using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// The focus bubble in the v5 scene (PO, 2026-10-23): its radius has to leave the pedestal, its
    /// part and the popup inside —in full light— and every vehicle outside —dimmed— in each zone of
    /// the vehicle section, with the zone's turn applied. Measured 2026-10-23: popup up to 1.48 m,
    /// e-bike from 1.70 m (rear view); radius 1.6.
    /// </summary>
    public class FocusDomeSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
        private const string MaterialPath = "Assets/_Project/UI/Materials/M_FocusDome.mat";
        private static readonly string[] Profiles =
        {
            "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset",
            "Assets/_Project/Module01/Data/SO_VehicleProfile_Escooter.asset",
        };

        private Scene _scene;
        private bool _openedHere;

        [SetUp]
        public void SetUp()
        {
            _scene = SceneManager.GetSceneByPath(ScenePath);
            _openedHere = !_scene.isLoaded;
            if (_openedHere)
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (_openedHere)
                EditorSceneManager.CloseScene(_scene, true);
        }

        private T Find<T>(string name = null) where T : Component
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
                foreach (T c in root.GetComponentsInChildren<T>(true))
                    if (name == null || c.name == name)
                        return c;
            return null;
        }

        private static float Farthest(Bounds b, Vector3 p) =>
            new Vector3(Mathf.Max(Mathf.Abs(p.x - b.min.x), Mathf.Abs(p.x - b.max.x)),
                        Mathf.Max(Mathf.Abs(p.y - b.min.y), Mathf.Abs(p.y - b.max.y)),
                        Mathf.Max(Mathf.Abs(p.z - b.min.z), Mathf.Abs(p.z - b.max.z))).magnitude;

        [Test]
        public void TheExperienceHasItsFocusBubble()
        {
            FocusDome dome = Find<FocusDome>();
            Assert.IsNotNull(dome, "run RideSafe > Módulo 1 > Componer paneles de la escena");
            Module01Experience experience = Find<Module01Experience>();
            Object wired = new SerializedObject(experience).FindProperty("_focus").objectReferenceValue;
            Assert.AreSame(dome, wired, "Module01Experience switches it on and off");
            Assert.IsNull(dome.GetComponent<Collider>(), "no collider: it must not block the pointer");
        }

        [Test]
        public void ThePedestalItsPartAndThePopup_StayInsideTheBubble()
        {
            FocusDome dome = Find<FocusDome>();
            Assume.That(dome, Is.Not.Null);
            ItemInspector inspector = Find<ItemInspector>();
            Transform head = (Transform)new SerializedObject(dome).FindProperty("_head").objectReferenceValue;
            Assert.IsNotNull(head, "the bubble follows the presentation's camera");
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            GameObject pedestal = (GameObject)typeof(ItemInspector).GetField("_pedestal", flags).GetValue(inspector);
            Transform stage = (Transform)typeof(ItemInspector).GetField("_stage", flags).GetValue(inspector);
            GameObject popup = (GameObject)typeof(ItemInspector).GetField("_confirmRoot", flags).GetValue(inspector);

            foreach (Renderer r in pedestal.GetComponentsInChildren<Renderer>(true))
                Assert.Less(Farthest(r.bounds, head.position), dome.Radius, "pedestal " + r.name);
            float part = Vector3.Distance(head.position, stage.position) + ItemInspector.DisplaySize * .5f * Mathf.Sqrt(3f);
            Assert.Less(part, dome.Radius, "the part on the pedestal, worst case");
            var corners = new Vector3[4];
            foreach (RectTransform rt in popup.GetComponentsInChildren<RectTransform>(true))
            {
                rt.GetWorldCorners(corners);
                foreach (Vector3 c in corners)
                    Assert.Less(Vector3.Distance(head.position, c), dome.Radius, "popup " + rt.name);
            }
        }

        [Test]
        public void EveryVehicle_StaysOutsideTheBubble_InEveryZone()
        {
            FocusDome dome = Find<FocusDome>();
            Assume.That(dome, Is.Not.Null);
            Vector3 head = GameObject.Find("anchor_vehicle").transform.position;
            foreach (string path in Profiles)
            {
                VehicleProfileSO profile = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(path);
                GameObject model = null;
                foreach (GameObject root in SceneFolder.TopLevelObjects(_scene))
                    if (root.name == profile.Model.name)
                        model = root;
                Assert.IsNotNull(model, profile.name);
                Quaternion keep = model.transform.localRotation;
                bool active = model.activeSelf;
                model.SetActive(true);
                try
                {
                    foreach (ZoneSO zone in profile.InspectionZones)
                    {
                        model.transform.localRotation = Quaternion.Euler(0f, profile.YawFor(zone), 0f);
                        foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>(false))
                        {
                            if (!r.enabled || r.GetComponentInParent<WorldObjectTaskEntity>() != null)
                                continue; // the points' click spheres are not the vehicle
                            float near = Vector3.Distance(head, r.bounds.ClosestPoint(head));
                            Assert.Greater(near, dome.Radius, $"{profile.name} {zone.ZoneId}: '{r.name}' comes inside the bubble");
                        }
                    }
                }
                finally
                {
                    model.transform.localRotation = keep;
                    model.SetActive(active);
                }
            }
        }

        /// <summary>
        /// The EPP tables too (PO, 2026-10-26: «que el fondo se ponga oscuro y que destaque el elemento
        /// seleccionado, así como se hizo con las partes de la e-bike»): from each station's viewpoint
        /// every object of its table stays outside the bubble, so the counter dims and the copy on the
        /// pedestal is the one thing in full light.
        /// </summary>
        [Test]
        public void EveryTableObject_StaysOutsideTheBubble_AtItsStation()
        {
            FocusDome dome = Find<FocusDome>();
            Assume.That(dome, Is.Not.Null);
            Module01Director director = Find<Module01Director>();
            Module01CatalogSO catalog = (Module01CatalogSO)new SerializedObject(director).FindProperty("_catalog").objectReferenceValue;
            Assert.IsNotNull(catalog);
            var report = new System.Text.StringBuilder();
            bool inside = false;
            foreach (ZoneSO zone in catalog.Zones)
            {
                Vector3 head = GameObject.Find("anchor_" + zone.ZoneId).transform.position;
                float nearest = float.MaxValue;
                string nearestName = null;
                foreach (SafetyItemSO item in zone.Items)
                    foreach (WorldObjectTaskEntity entity in FindAll<WorldObjectTaskEntity>())
                    {
                        if (entity.ItemId != item.ItemId)
                            continue;
                        foreach (Renderer r in entity.GetComponentsInChildren<Renderer>(false))
                        {
                            if (!r.enabled)
                                continue;
                            float near = Vector3.Distance(head, r.bounds.ClosestPoint(head));
                            if (near < nearest) { nearest = near; nearestName = item.ItemId + "/" + r.name; }
                        }
                    }
                Assert.IsNotNull(nearestName, "nothing measured at " + zone.ZoneId);
                report.AppendLine($"  {zone.ZoneId}: nearest {nearestName} at {nearest:0.00} m");
                inside |= nearest <= dome.Radius;
            }
            Assert.IsFalse(inside, $"radius {dome.Radius:0.00} m leaves table objects in full light:\n{report}");
            Assert.Pass($"radius {dome.Radius:0.00} m:\n{report}");
        }

        private System.Collections.Generic.IEnumerable<T> FindAll<T>() where T : Component
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
                foreach (T c in root.GetComponentsInChildren<T>(true))
                    yield return c;
        }

        [Test]
        public void TheMaterial_IsSeenFromInside_AndDrawsBeforeTheUi()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.IsNotNull(material, MaterialPath);
            Assert.AreEqual("Universal Render Pipeline/Unlit", material.shader.name);
            Assert.AreEqual(1f, material.GetFloat("_Surface"), "transparent");
            Assert.AreEqual(1f, material.GetFloat("_Cull"), "front faces culled: the inside is drawn");
            Assert.Greater(material.renderQueue, 2500, "after the opaque world");
            Assert.Less(material.renderQueue, 3000, "before the UI canvases: the popup inside is never darkened");
            Assert.AreSame(material, Find<FocusDome>()?.GetComponent<MeshRenderer>()?.sharedMaterial);
        }
    }
}
