using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Lo que se mueve en el podio (Excel 11-13, «Componentes vehiculos sección 2»), contra los
    /// modelos que van al podio de verdad: los del perfil de cada vehículo.
    /// </summary>
    public class PodiumAnimationTests
    {
        private const string EbikePath = "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset";
        private const string ScooterPath = "Assets/_Project/Module01/Data/SO_VehicleProfile_Escooter.asset";

        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private static VehicleProfileSO Profile(string path) => AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(path);

        private GameObject Podium(string path, string point)
        {
            GameObject prefab = Profile(path).PodiumModelFor(point);
            Assert.IsNotNull(prefab, $"{path}: {point} no tiene modelo del podio.");
            _root = new GameObject("PresentedItem");
            GameObject copy = Object.Instantiate(prefab, _root.transform);
            copy.name = prefab.name;
            return copy;
        }

        /// <summary>La punta de la maneta, en el espacio del vehículo, en el instante dado.</summary>
        private static Vector3 Tip(Transform lever, Transform vehicle)
        {
            Mesh mesh = lever.GetComponent<MeshFilter>().sharedMesh;
            return vehicle.InverseTransformPoint(lever.TransformPoint(mesh.bounds.center * 2f));
        }

        /// <summary>
        /// Las dos manetas se aprietan hacia el puño y vuelven: en la bici el puño está detrás de la
        /// maneta (+X, el morro es −X); en la patineta, hacia quien maneja (−Z, el morro es +Z).
        /// </summary>
        [TestCase(EbikePath, 0, 1)]
        public void Las_manetas_de_la_bici_se_aprietan_hacia_el_punio(string path, int axis, int sign)
        {
            GameObject podium = Podium(path, "point_brakes");
            AssertSqueeze(podium, podium.transform, axis, sign);
        }

        [Test]
        public void Las_manetas_de_la_patineta_se_aprietan_hacia_el_punio()
        {
            GameObject vehicle = Object.Instantiate(Profile(ScooterPath).Model);
            _root = vehicle;
            AssertSqueeze(vehicle, vehicle.transform, 2, -1);
        }

        private static void AssertSqueeze(GameObject model, Transform vehicle, int axis, int sign)
        {
            Transform[] levers = model.GetComponentsInChildren<Transform>().Where(t => t.name == "freno_de" || t.name == "freno_iz").ToArray();
            Assert.AreEqual(2, levers.Length);
            Vector3[] rest = levers.Select(l => Tip(l, vehicle)).ToArray();
            PodiumAnimation anim = PodiumAnimation.Attach(model, "point_brakes", null);
            Assert.IsNotNull(anim);

            anim.Tick(PodiumAnimation.SqueezePeriod / 4f);
            for (int i = 0; i < levers.Length; i++)
            {
                float moved = (Tip(levers[i], vehicle) - rest[i])[axis] * sign;
                Assert.Greater(moved, .01f, $"{levers[i].name} no va hacia el puño: {moved:F3} m");
            }
            anim.Tick(PodiumAnimation.SqueezePeriod * .75f);
            for (int i = 0; i < levers.Length; i++)
                Assert.Less(Vector3.Distance(Tip(levers[i], vehicle), rest[i]), .001f, levers[i].name + " no vuelve a su sitio.");
        }

        /// <summary>
        /// Lo que va de verdad al podio con los frenos de la patineta: una copia de las dos manetas en
        /// un contenedor que se llama como la primera («freno_de») y tiene el pivote donde estaba el
        /// vehículo. Tomarlo por maneta lo giraba sobre ese pivote y sacaba las manetas del podio
        /// (PO, 2026-11-19).
        /// </summary>
        [Test]
        public void Las_manetas_de_la_patineta_no_se_salen_del_podio()
        {
            GameObject vehicle = Object.Instantiate(Profile(ScooterPath).Model);
            vehicle.transform.position = new Vector3(0f, 0f, 2.5f);
            _root = new GameObject("PresentedItem");
            try
            {
                MeshRenderer[] parts = vehicle.GetComponentsInChildren<MeshRenderer>()
                    .Where(r => r.name == "freno_de" || r.name == "freno_iz").OrderByDescending(r => r.name).ToArray();
                GameObject copy = VisibleMeshCopy.Of(parts, parts[0].name, _root.transform);
                copy.transform.position = new Vector3(-.3f, -1.4f, -2.3f); // el contenedor lejos de las manetas, como en Play
                Quaternion rest = copy.transform.localRotation;
                Vector3[] before = copy.GetComponentsInChildren<Renderer>().Select(r => r.bounds.center).ToArray();

                PodiumAnimation anim = PodiumAnimation.Attach(copy, "point_brakes", null);
                Assert.IsNotNull(anim);
                anim.Tick(PodiumAnimation.SqueezePeriod / 4f);

                Assert.AreEqual(0f, Quaternion.Angle(rest, copy.transform.localRotation), .01f, "El contenedor gira como si fuera una maneta.");
                Vector3[] after = copy.GetComponentsInChildren<Renderer>().Select(r => r.bounds.center).ToArray();
                for (int i = 0; i < before.Length; i++)
                    Assert.Less(Vector3.Distance(before[i], after[i]), .05f, "Una maneta se va " + Vector3.Distance(before[i], after[i]).ToString("F2") + " m.");
            }
            finally
            {
                Object.DestroyImmediate(vehicle);
            }
        }

        /// <summary>
        /// La dirección: el manubrio, el tenedor y la rueda delantera juntos, girando a un lado y al
        /// otro sobre el pivote, y la rueda se va de lado (no solo da vueltas en su sitio).
        /// </summary>
        [TestCase(EbikePath, "tenedor")]
        [TestCase(ScooterPath, "Manubrio")]
        public void La_direccion_gira_con_la_rueda_delantera(string path, string root)
        {
            GameObject podium = Podium(path, "point_steering");
            Assert.AreEqual(root, podium.name);
            Transform wheel = podium.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "llanta_delantera");
            Assert.IsNotNull(wheel, "Al podio de la dirección no va la rueda delantera.");
            PodiumAnimation anim = PodiumAnimation.Attach(podium, "point_steering", null);
            Assert.IsNotNull(anim);
            // Después de Attach: con cámara en la escena, primero se pone de tres cuartos hacia ella.
            Vector3 pivot = podium.transform.position;
            anim.Tick(0f);
            Quaternion rest = wheel.rotation;
            anim.Tick(PodiumAnimation.SteerPeriod / 4f);
            Quaternion left = wheel.rotation;
            anim.Tick(PodiumAnimation.SteerPeriod * .75f);
            Quaternion right = wheel.rotation;

            Assert.Less(Vector3.Distance(podium.transform.position, pivot), 1e-4f, "El pivote se mueve.");
            Assert.AreEqual(2f * PodiumAnimation.SteerDegrees, Quaternion.Angle(left, right), 1f, "La rueda no gira de un lado al otro.");
            Assert.AreEqual(Quaternion.Angle(rest, left), Quaternion.Angle(rest, right), 1f, "No gira lo mismo a cada lado.");
            // Sobre un eje casi vertical: es girar el manubrio, no voltear la rueda.
            Vector3 axis = (podium.transform.position - wheel.position).normalized;
            Assert.Greater(Vector3.Dot(axis, Vector3.up), .85f, "El eje de la dirección está tumbado.");
        }

        /// <summary>El display pasa de apagado a encendido, y en la pantalla de la bici solo la pantalla, no su carcasa.</summary>
        [Test]
        public void El_display_se_enciende()
        {
            GameObject bike = Object.Instantiate(Profile(EbikePath).Model);
            _root = bike;
            Transform[] parts = bike.GetComponentsInChildren<Transform>().Where(t => t.name == "display" || t.name == "pantalla").ToArray();
            Assert.AreEqual(2, parts.Length);
            var holder = new GameObject("Electrics");
            holder.transform.SetParent(bike.transform);
            foreach (Transform p in parts) p.SetParent(holder.transform, true);
            Material shell = parts.First(p => p.name == "pantalla").GetComponent<Renderer>().sharedMaterials[0];

            PodiumAnimation anim = PodiumAnimation.Attach(holder, "point_electrics", null);
            Assert.IsNotNull(anim);
            anim.Tick(.1f);
            Assert.IsFalse(anim.ScreenOn, "Empieza encendido: no se ve el cambio.");
            anim.Tick(PodiumAnimation.DisplayPeriod * .8f);
            Assert.IsTrue(anim.ScreenOn);
            foreach (Transform p in parts)
            {
                Material[] mats = p.GetComponent<Renderer>().sharedMaterials;
                Assert.IsTrue(mats.Any(m => m.IsKeywordEnabled("_EMISSION") && m.GetColor("_EmissionColor").maxColorComponent > .5f), p.name + " no brilla encendido.");
            }
            Assert.AreSame(shell, parts.First(p => p.name == "pantalla").GetComponent<Renderer>().sharedMaterials[0], "Se encendió la carcasa de la pantalla.");
        }

        /// <summary>La transmisión pedalea: el plato hacia delante y el piñón de atrás más rápido.</summary>
        [Test]
        public void La_transmision_pedalea()
        {
            GameObject podium = Podium(EbikePath, "point_drivetrain");
            Transform ring = podium.GetComponentsInChildren<Transform>().First(t => t.name.StartsWith("piñon"));
            Transform cog = podium.GetComponentsInChildren<Transform>().First(t => t.name == "anilli_de_cadena");
            PodiumAnimation anim = PodiumAnimation.Attach(podium, "point_drivetrain", null);
            Assert.IsNotNull(anim);
            anim.Tick(.25f);
            // Hacia delante: lo de arriba del plato va hacia el morro (−X).
            Vector3 top = podium.transform.InverseTransformDirection(ring.TransformDirection(Vector3.up));
            Assert.Less(top.x, -.1f, "El plato pedalea hacia atrás.");
            Assert.Greater(Quaternion.Angle(Quaternion.identity, cog.localRotation), Quaternion.Angle(Quaternion.identity, ring.localRotation),
                "El piñón de atrás gira más despacio que el plato.");
        }

        /// <summary>Los EPP y los puntos sin animación no se tocan.</summary>
        [Test]
        public void Lo_que_no_se_anima_no_lleva_componente()
        {
            _root = new GameObject("Casco");
            Assert.IsNull(PodiumAnimation.Attach(_root, "point_lights", null));
            Assert.IsNull(_root.GetComponent<PodiumAnimation>());
        }
    }
}
