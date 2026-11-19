using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Afirma, contra los FBX de verdad, que cada perfil delinea piezas que existen y que el
    /// giro de cada zona las pone donde el aprendiz puede verlas.
    /// <para>
    /// <b>Cada FBX viene orientado a su manera</b>: la e-bike con el eje largo en X y el morro
    /// en la X baja, la patineta con el eje largo en Z y el morro en la Z alta. Desde que el aviso
    /// es el contorno de la pieza real, el giro decide si esa pieza se ve o queda de espaldas, y
    /// una tabla de giros compartida presentaba uno de los dos de espaldas.
    /// </para>
    /// <para>
    /// Por eso el invariante no se escribe como «el giro del patinete vale −90»: eso solo
    /// repetiría el número. Se escribe como lo que tiene que pasar —la pieza queda de cara al
    /// aprendiz— y se deriva de la geometría del FBX, así que una entrega que reexporte una
    /// malla girada lo rompe y avisa en vez de pasar en silencio.
    /// </para>
    /// </summary>
    public class VehiclePartsTests
    {
        private const string EbikePath = "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset";
        private const string ScooterPath = "Assets/_Project/Module01/Data/SO_VehicleProfile_Escooter.asset";

        private const string Missing =
            "Corre el menu RideSafe > Modulo 1 > Montar el punto de exhibicion del vehiculo.";

        /// <summary>
        /// El aprendiz se planta en +Z y mira hacia −Z, así que lo que tiene delante es lo que
        /// cae en +Z. Y su derecha es −X —<c>LookRotation(-Z)</c> deja <c>right</c> en −X—, que
        /// es por donde vive el checklist: media pantalla a la derecha.
        /// </summary>
        private const float TowardLearner = 1f;

        private static VehicleProfileSO Profile(string path)
        {
            VehicleProfileSO profile = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(path);
            Assert.IsNotNull(profile, Missing);
            Assert.IsNotNull(profile.Model, "El perfil tiene que apuntar a su FBX.");
            return profile;
        }

        /// <summary>Caja de toda la malla en el espacio local de su raíz, como la calcula el montador.</summary>
        private static Bounds LocalBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers, $"'{model.name}' no tiene ningun renderer.");

            bool first = true;
            Bounds local = default;
            foreach (Renderer r in renderers)
            {
                Vector3 p = model.transform.InverseTransformPoint(r.bounds.center);
                if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                else local.Encapsulate(p);
            }
            return local;
        }

        private static Transform Part(GameObject model, string partName)
        {
            Transform part = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t != model.transform && t.name == partName);
            Assert.IsNotNull(part,
                $"'{model.name}' no tiene la pieza '{partName}' que declara su perfil. " +
                "Si una entrega de arte la renombro, hay que actualizar VehicleProfiles.");
            return part;
        }

        /// <summary>
        /// Dónde queda la pieza respecto al centro del vehículo, ya girado para esa zona. Es
        /// exactamente la cuenta que hace <c>VehicleStage.FaceYaw</c>: rotación local absoluta
        /// en Y, sin tocar al aprendiz.
        /// </summary>
        private static Vector3 OffsetAfterYaw(VehicleProfileSO profile, string partName, float yaw)
        {
            GameObject model = profile.Model;
            Bounds local = LocalBounds(model);
            Vector3 part = model.transform.InverseTransformPoint(
                Part(model, partName).GetComponent<Renderer>().bounds.center);

            return Quaternion.Euler(0f, yaw, 0f) * (part - local.center);
        }

        private static ZoneSO Zone(VehicleProfileSO profile, string zoneId)
        {
            ZoneSO zone = profile.InspectionZones.FirstOrDefault(z => z != null && z.ZoneId == zoneId);
            Assert.IsNotNull(zone, $"El perfil '{profile.name}' no recorre la zona '{zoneId}'.");
            return zone;
        }

        /// <summary>
        /// Lo primero que se rompe cuando llega un FBX nuevo: Daniel reexporta y una pieza
        /// cambia de nombre. El perfil se queda apuntando al nombre viejo, <c>FindPart</c> no la
        /// encuentra y ese punto cae al signo <b>sin que falle nada</b>: solo un warning en una
        /// consola que nadie lee. Esto lo convierte en rojo.
        /// </summary>
        [Test]
        public void Cada_pieza_que_declara_un_perfil_existe_en_su_malla_y_se_puede_delinear()
        {
            foreach (string path in new[] { EbikePath, ScooterPath })
            {
                VehicleProfileSO profile = Profile(path);
                foreach (string itemId in PointIds)
                foreach (string partName in profile.PartNamesFor(itemId))
                {
                    Transform part = Part(profile.Model, partName);
                    MeshFilter filter = part.GetComponent<MeshFilter>();
                    Assert.IsNotNull(filter?.sharedMesh,
                        $"'{partName}' no tiene malla, asi que InspectionHighlight.Bind devolveria false " +
                        "y el punto se quedaria con el signo sin que nadie lo note.");
                }
            }
        }

        private static readonly string[] PointIds =
        {
            "point_steering", "point_brakes", "point_lights", "point_wheels", "point_electrics",
            "point_deck",
        };

        /// <summary>
        /// La patineta llega despiezada en dos piezas anidadas: <c>Manubrio</c>
        /// (con <c>freno_iz</c>, <c>freno_de</c> y <c>llanta_delantera</c> dentro) y
        /// <c>cuerpo</c> (con <c>llanta_tracera</c> y <c>soporte</c>). Frenos y ruedas tienen
        /// dos piezas y se delinean las dos, como en la bici; la plataforma es el <c>cuerpo</c>.
        /// </summary>
        [Test]
        public void El_patinete_delinea_sus_piezas()
        {
            VehicleProfileSO scooter = Profile(ScooterPath);

            CollectionAssert.AreEqual(new[] { "Manubrio" }, scooter.PartNamesFor("point_steering"));
            CollectionAssert.AreEqual(new[] { "freno_de", "freno_iz" }, scooter.PartNamesFor("point_brakes"));
            // La delantera, solo el neumático (ef550e3); la trasera sigue entera.
            CollectionAssert.AreEqual(new[] { "llanta_tracera", "neumatico" }, scooter.PartNamesFor("point_wheels"));
            CollectionAssert.IsEmpty(scooter.PartNamesFor("point_deck"), "La plataforma ya no se revisa (cliente, 2026-11-11).");
        }

        /// <summary>
        /// En la bici la transmisión se delinea en 'pedales', que es lo que trae su FBX, y al podio va
        /// la transmisión separada de Daniel (ef550e3): cadena, plato y piñones.
        /// </summary>
        [Test]
        public void La_bici_delinea_su_transmision()
        {
            CollectionAssert.AreEqual(new[] { "pedales" }, Profile(EbikePath).PartNamesFor("point_drivetrain"));
            GameObject podium = Profile(EbikePath).PodiumModelFor("point_drivetrain");
            Assert.IsNotNull(podium, "La transmisión no lleva su modelo al podio.");
            string[] names = podium.GetComponentsInChildren<MeshFilter>(true).Select(f => f.name).ToArray();
            CollectionAssert.IsSubsetOf(new[] { "cadena", "anilli_de_cadena" }, names);
        }

        /// <summary>
        /// Luces y eléctrico: desde ef550e3 (Daniel, 2026-11-18) el FBX trae luz trasera y display, y
        /// lo eléctrico es el display, no las direccionales.
        /// </summary>
        [Test]
        public void El_patinete_ensena_sus_piezas_de_luces_y_electrico()
        {
            VehicleProfileSO scooter = Profile(ScooterPath);

            CollectionAssert.AreEqual(new[] { "luz_freontal", "luz_trasera" }, scooter.PartNamesFor("point_lights"));
            CollectionAssert.AreEqual(new[] { "display" }, scooter.PartNamesFor("point_electrics"));
        }

        /// <summary>
        /// Las luces se revisan de costado, como las llantas: desde atrás no se ve la delantera
        /// (PO, 2026-11-06).
        /// </summary>
        [TestCase(EbikePath)]
        [TestCase(ScooterPath)]
        public void Las_luces_se_revisan_desde_donde_se_revisan_las_llantas(string path)
        {
            VehicleProfileSO profile = Profile(path);
            Assert.AreEqual(profile.YawFor(Zone(profile, "wheels")), profile.YawFor(Zone(profile, "visibility")));
        }

        /// <summary>
        /// La e-bike nueva (2026-10-21) llega con las piezas que faltaban para revisar: los cinco
        /// puntos tienen pieza, y frenos, ruedas y luces tienen dos (Cindy: delinear las dos).
        /// Ya no hay <c>manubrio</c>: la dirección es el <c>tenedor</c>, horquilla y manubrio.
        /// La rueda de atrás va primera porque es la que lleva la esfera y la que el giro de la
        /// zona de llantas deja fuera del checklist.
        /// </summary>
        [Test]
        public void La_bici_delinea_los_cinco_puntos_con_sus_piezas()
        {
            VehicleProfileSO ebike = Profile(EbikePath);

            CollectionAssert.AreEqual(new[] { "tenedor" }, ebike.PartNamesFor("point_steering"));
            CollectionAssert.AreEqual(new[] { "freno_de", "freno_iz" }, ebike.PartNamesFor("point_brakes"));
            // Solo las llantas, sin rines ni radios (cliente, 2026-11-11).
            CollectionAssert.AreEqual(new[] { "rueda_tracera1", "caucho" }, ebike.PartNamesFor("point_wheels"));
            CollectionAssert.AreEqual(new[] { "luz_delantera", "luz_tracera" }, ebike.PartNamesFor("point_lights"));
            CollectionAssert.AreEqual(new[] { "display", "pantalla" }, ebike.PartNamesFor("point_electrics"));
        }

        /// <summary>
        /// Las llantas de la bici son más grandes que la rueda de la que cuelgan: si un FBX nuevo
        /// cambia los nombres, se delinearía el rin y no la llanta sin que nada fallara.
        /// </summary>
        [Test]
        public void Las_llantas_de_la_bici_rodean_a_su_rueda()
        {
            GameObject model = Profile(EbikePath).Model;
            foreach ((string tire, string wheel) in new[] { ("caucho", "llanta_delantera"), ("rueda_tracera1", "rueda_tracera") })
            {
                Mesh tireMesh = model.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == tire).sharedMesh;
                Mesh wheelMesh = model.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == wheel).sharedMesh;
                Assert.Greater(tireMesh.bounds.size.y, wheelMesh.bounds.size.y, $"{tire} no rodea a {wheel}");
            }
        }

        /// <summary>
        /// Los frenos de la bici van al podio con el manubrio entero (frenos.fbx) y en él se delinean
        /// solo las manetas, que se llaman igual que en la bici: solas, el borde del podio las tapa
        /// (captura 2026-11-18). Las manetas están separadas en los dos vehículos.
        /// </summary>
        [Test]
        public void Los_frenos_de_la_bici_van_al_podio_con_el_manubrio()
        {
            GameObject podium = Profile(EbikePath).PodiumModelFor("point_brakes");
            Assert.IsNotNull(podium, "La bici no declara modelo del podio para los frenos.");
            string[] names = podium.GetComponentsInChildren<MeshFilter>(true).Select(f => f.name).ToArray();
            foreach (string lever in Profile(EbikePath).PartNamesFor("point_brakes"))
                CollectionAssert.Contains(names, lever, "El modelo del podio no trae la maneta " + lever);
            Assert.IsTrue(podium.GetComponentsInChildren<MeshRenderer>(true).All(r => r.sharedMaterials.All(m => m != null && m.name == "e_bike")),
                "frenos.fbx sin el material de la bici: sale gris en el podio.");
            foreach (string path in new[] { EbikePath, ScooterPath })
            {
                string[] meshes = Profile(path).Model.GetComponentsInChildren<MeshFilter>(true).Select(f => f.name).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "freno_de", "freno_iz" }, meshes, path + ": las manetas no están separadas.");
            }
        }

        /// <summary>
        /// El invariante que caza el espejo: en la zona del manubrio, la pieza de la dirección
        /// tiene que acabar <b>entre el centro del vehículo y el aprendiz</b>: de espaldas, el
        /// aprendiz revisaría la dirección mirando la parte de atrás.
        /// </summary>
        [Test]
        public void El_giro_de_la_zona_de_manubrio_pone_la_direccion_de_cara_al_aprendiz()
        {
            foreach (string path in new[] { EbikePath, ScooterPath })
            {
                VehicleProfileSO profile = Profile(path);
                float yaw = profile.YawFor(Zone(profile, "cockpit"));
                Vector3 offset = OffsetAfterYaw(profile, profile.PartNameFor("point_steering"), yaw);

                Assert.Greater(offset.z * TowardLearner, 0f,
                    $"'{profile.name}' gira {yaw:F0}° en la zona de manubrio y deja la direccion a " +
                    $"z={offset.z:F2}: el aprendiz la ve de espaldas.");
            }
        }

        /// <summary>
        /// Y en la zona de llantas, la rueda que se delinea tiene que caer a la <b>izquierda</b>
        /// del aprendiz —X positiva del mundo—, porque su derecha se la come el checklist.
        /// <para>
        /// Esto ya pasó con la bici: a 180° la rueda trasera quedaba detrás de la lista y el
        /// clic chocaba con la fila «Frenos» en vez de llegar a la rueda. El test pasaba igual
        /// —el clic iba bien antes del contorno— y lo cazó una captura. Ahora es rojo.
        /// </para>
        /// </summary>
        [Test]
        public void El_giro_de_la_zona_de_llantas_deja_la_rueda_fuera_del_checklist()
        {
            foreach (string path in new[] { EbikePath, ScooterPath })
            {
                VehicleProfileSO profile = Profile(path);
                float yaw = profile.YawFor(Zone(profile, "wheels"));
                Vector3 offset = OffsetAfterYaw(profile, profile.PartNameFor("point_wheels"), yaw);

                Assert.Greater(offset.x, 0f,
                    $"'{profile.name}' gira {yaw:F0}° en la zona de llantas y deja la rueda a " +
                    $"x={offset.x:F2}: cae por la derecha, debajo del checklist.");
            }
        }

        /// <summary>
        /// Cada FBX viene orientado a su manera —la bici con el eje largo en X y el morro en la
        /// X baja, el scooter de 2026-10-26 con el eje largo en <b>Z</b> y el morro en la Z alta—,
        /// así que la tabla de giros es por vehículo. Si alguien la unifica, alguno de los
        /// invariantes de arriba deja de cumplirse; esto lo dice con el motivo delante.
        /// </summary>
        [Test]
        public void Cada_vehiculo_tiene_su_tabla_de_giros()
        {
            VehicleProfileSO ebike = Profile(EbikePath);
            VehicleProfileSO scooter = Profile(ScooterPath);

            List<string> iguales = new List<string>();
            foreach (string zoneId in new[] { "cockpit", "wheels", "visibility" })
            {
                float a = ebike.YawFor(Zone(ebike, zoneId));
                float b = scooter.YawFor(Zone(scooter, zoneId));
                if (Mathf.Approximately(a, b))
                    iguales.Add($"{zoneId}={a:F0}°");
            }

            CollectionAssert.IsEmpty(iguales,
                "Los dos FBX no estan orientados igual: cada zona necesita su giro por vehiculo. " +
                "Zonas con el giro compartido: " + string.Join(", ", iguales));
        }

    }
}
