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
    /// Esto nace de un fallo que ningún test tocaba. <b>Las dos mallas están en espejo</b>: la
    /// e-bike trae el morro en la X local baja (<c>manubrio</c> en u=0.19) y el patinete en la
    /// alta (<c>control_de_direccion</c> en u=0.74). Hasta el garaje v8 daba igual, porque el
    /// aviso era un signo flotante colocado a mano en un hueco despejado; desde que el aviso es
    /// el contorno de la pieza real, el giro decide si esa pieza se ve o queda de espaldas. Con
    /// la tabla de giros compartida entre los dos vehículos, el patinete se presentaba <b>de
    /// culo</b> en la zona de manubrio y con la rueda detrás del checklist en la de llantas.
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
                "Si una entrega de arte la renombro, hay que actualizar Module01VehicleStaging.Parts.");
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
        };

        /// <summary>
        /// El patinete llega despiezado en siete el 2026-10-14 y trae <b>la maneta de freno
        /// como pieza propia</b>, que la e-bike no tiene: en la bici el freno vive dentro del
        /// <c>manubrio</c>. Así que aquí son tres de cinco los puntos con contorno, no dos.
        /// </summary>
        [Test]
        public void El_patinete_delinea_direccion_freno_y_rueda()
        {
            VehicleProfileSO scooter = Profile(ScooterPath);

            Assert.AreEqual("control_de_direccion", scooter.PartNameFor("point_steering"));
            Assert.AreEqual("freno", scooter.PartNameFor("point_brakes"));
            Assert.AreEqual("rueda_tracera", scooter.PartNameFor("point_wheels"));
        }

        /// <summary>
        /// Y los otros dos se quedan con el signo <b>a propósito</b>, por el mismo motivo que
        /// en la bici: la batería va dentro del <c>cuerpo</c>, que es la plataforma entera, y
        /// delinear 1,16 m de patinete para decir «revisa el sistema eléctrico» enseña algo
        /// falso. El faro no existe como pieza en el FBX.
        /// </summary>
        [Test]
        public void El_patinete_deja_en_el_signo_lo_que_no_tiene_pieza_propia()
        {
            VehicleProfileSO scooter = Profile(ScooterPath);

            Assert.AreEqual(string.Empty, scooter.PartNameFor("point_electrics"),
                "La bateria vive dentro de 'cuerpo'; delinear la plataforma entera enseña algo falso.");
            Assert.AreEqual(string.Empty, scooter.PartNameFor("point_lights"),
                "El patinete no trae faro como pieza.");
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
            CollectionAssert.AreEqual(new[] { "rueda_tracera", "llanta_delantera" }, ebike.PartNamesFor("point_wheels"));
            CollectionAssert.AreEqual(new[] { "luz_delantera", "luz_tracera" }, ebike.PartNamesFor("point_lights"));
            CollectionAssert.AreEqual(new[] { "pantalla" }, ebike.PartNamesFor("point_electrics"));
        }

        /// <summary>
        /// El invariante que caza el espejo: en la zona del manubrio, la pieza de la dirección
        /// tiene que acabar <b>entre el centro del vehículo y el aprendiz</b>. Con la tabla de
        /// giros compartida, el patinete cumplía lo contrario —el morro a −Z, de espaldas— y el
        /// aprendiz revisaba la dirección mirándole el guardabarros de atrás.
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
        /// Las dos mallas están en espejo, así que los giros <b>no pueden</b> ser los mismos.
        /// Esto es lo que fija que la tabla de giros sea por vehículo y no una compartida: si
        /// alguien la vuelve a unificar, los dos invariantes de arriba no pueden cumplirse a la
        /// vez y esto lo dice con el motivo delante.
        /// </summary>
        [Test]
        public void Los_dos_vehiculos_no_pueden_compartir_la_tabla_de_giros()
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
                "La bici tiene el morro en la X local baja y el patinete en la alta: " +
                "cada zona necesita su giro por vehiculo. Zonas con el giro compartido: " +
                string.Join(", ", iguales));
        }
    }
}
