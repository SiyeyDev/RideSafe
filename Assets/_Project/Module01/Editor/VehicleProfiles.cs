using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Los vehículos del módulo 1, declarados una sola vez: perfil, modelo, escala en la escena,
    /// zonas que recorre con el giro de cada una, piezas que delinea, video de la revisión, y
    /// dónde cuelga cada punto de inspección.
    /// <para>
    /// <b>Por qué existe.</b> Antes cada herramienta cargaba sus perfiles a mano y guardaba su
    /// parte del vehículo en tablas propias —los giros y las piezas en
    /// <c>Module01VehicleStaging</c>, las zonas en <c>Module01Greybox</c>—, y dos de ellas solo
    /// cargaban el de la e-bike. Aquí está todo lo que distingue a un vehículo de otro: la bici y
    /// la patineta comparten la manera de revisar, pero son vehículos distintos.
    /// </para>
    /// </summary>
    public static class VehicleProfiles
    {
        public const string DataFolder = "Assets/_Project/Module01/Data";

        /// <summary>El video de prueba, hasta que lleguen los de cada vehículo.</summary>
        private const string PlaceholderVideo = "Assets/_Project/Video/VideoTest.mp4";

        public readonly struct Definition
        {
            public Definition(string assetSuffix, string contextValue, string modelFile, float sceneScale,
                              string videoPath, (string Id, float Yaw)[] zones, (string Item, string Part)[] parts)
            {
                AssetSuffix = assetSuffix;
                ContextValue = contextValue;
                ModelFile = modelFile;
                SceneScale = sceneScale;
                VideoPath = videoPath;
                Zones = zones;
                Parts = parts;
            }

            /// <summary>Sufijo del asset: <c>SO_VehicleProfile_{AssetSuffix}</c>.</summary>
            public string AssetSuffix { get; }

            /// <summary>Valor de la clave <c>vehicle</c> que elige este perfil.</summary>
            public string ContextValue { get; }

            /// <summary>FBX dentro de <see cref="Module01Library.Models"/>.</summary>
            public string ModelFile { get; }

            /// <summary>
            /// Escala de la instancia en la escena. Es la que dejó el artista en <c>Garage</c>: la
            /// patineta llega a 1,00 m de largo y él la puso a ×1.4 (1,40 m). Va en la instancia y
            /// no en el importador para no cambiar la escena de arte.
            /// </summary>
            public float SceneScale { get; }

            /// <summary>El video que explica la revisión de este vehículo.</summary>
            public string VideoPath { get; }

            /// <summary>
            /// Zonas del vehículo en el orden en que se recorren, con el giro del vehículo en cada
            /// una: grados <b>absolutos</b>, porque <c>VehicleStage</c> asigna la rotación local.
            /// <para>
            /// Por vehículo, y no hay forma de evitarlo: cada FBX viene orientado a su manera. La
            /// bici trae el eje largo en la X local y el morro en la X baja —a 0° se ve de costado
            /// y a 90° de frente—; la patineta, el eje largo en la Z y el morro en la Z alta —a 0°
            /// ya mira al aprendiz—. Desde que el aviso es el contorno de la pieza real, el giro
            /// decide si esa pieza se ve o queda detrás. Lo guarda <c>VehiclePartsTests</c>,
            /// derivándolo de la geometría del FBX en vez de repetir estos números.
            /// </para>
            /// </summary>
            public IReadOnlyList<(string Id, float Yaw)> Zones { get; }

            /// <summary>
            /// Qué pieza de la malla delinea cada punto. Un punto se declara una vez por pieza; la
            /// primera lleva la esfera del punto. Se busca por nombre en toda la jerarquía del
            /// modelo (<c>VehicleStage.FindPart</c>), así que los nombres van como vienen en el FBX
            /// —'tracera', con la falta, incluida—.
            /// </summary>
            public IReadOnlyList<(string Item, string Part)> Parts { get; }

            public IEnumerable<string> ZoneIds => Zones.Select(z => z.Id);

            public string ModelPath => Module01Library.Models + ModelFile;
        }

        public static readonly Definition[] Definitions =
        {
            // La e-bike llegó el 2026-10-21 con las piezas para revisar anidadas: 'marco' con
            // 'pedales', 'rueda_tracera', 'luz_tracera' y el 'tenedor'; del tenedor 'freno_de',
            // 'freno_iz', 'llanta_delantera', 'luz_delantera' y 'pantalla'. Los cinco puntos tienen
            // pieza y frenos, ruedas y luces tienen dos: se delinean las dos (Cindy). La dirección
            // es el tenedor. En las ruedas va primero la trasera, la que el giro deja fuera del
            // checklist.
            new Definition("Ebike", "ebike", "ebike.fbx", 1f, PlaceholderVideo,
                new[]
                {
                    ("cockpit", 90f),      // de frente: manubrio, frenos, electrico
                    // De costado. A 180 la rueda trasera quedaba detrás del checklist, que vive a
                    // la derecha; el test pasaba igual y lo cazó una captura.
                    ("wheels", 0f),
                    ("visibility", -90f),  // por detras: luces y reflectivos
                },
                new[]
                {
                    ("point_steering", "tenedor"),
                    ("point_brakes", "freno_de"),
                    ("point_brakes", "freno_iz"),
                    ("point_wheels", "rueda_tracera"),
                    ("point_wheels", "llanta_delantera"),
                    ("point_lights", "luz_delantera"),
                    ("point_lights", "luz_tracera"),
                    ("point_electrics", "pantalla"),
                }),
            // La patineta viene en dos piezas anidadas: 'Manubrio' (con 'freno_iz', 'freno_de' y
            // 'llanta_delantera') y 'cuerpo' (con 'llanta_tracera' y 'soporte'). La plataforma
            // —solo la revisa la patineta, narrativa §4.8.2— es el 'cuerpo'. Lo eléctrico y las
            // luces se quedan con el signo: la pantalla es un material del Manubrio y no hay faro.
            new Definition("Escooter", "escooter", "E_Scooter.fbx", 1.4f, PlaceholderVideo,
                new[]
                {
                    ("cockpit", 0f),       // de frente: Manubrio, frenos y pantalla
                    ("wheels", -90f),      // de costado, con la llanta de atras a la izquierda
                    // La plataforma va entre las ruedas y las luces: de costado a tres cuartos por
                    // detras a por detras, el vehiculo gira siempre hacia el mismo lado.
                    ("deck", -135f),
                    ("visibility", 180f),  // por detras
                },
                new[]
                {
                    ("point_steering", "Manubrio"),
                    ("point_brakes", "freno_iz"),
                    ("point_brakes", "freno_de"),
                    ("point_wheels", "llanta_tracera"),
                    ("point_wheels", "llanta_delantera"),
                    ("point_deck", "cuerpo"),
                }),
        };

        /// <summary>
        /// Dónde cuelga cada punto, en fracciones de la caja del vehículo: <c>u</c> a lo largo,
        /// <c>v</c> en altura, <c>w</c> en anchura. Una sola tabla para los dos: el eje largo es el
        /// horizontal mayor de la caja (ver <c>Module01VehicleStaging.MountPosition</c>), y el punto
        /// que tiene pieza se muda luego encima de ella, al sitio más cercano
        /// (<c>VehicleStage.MoveToPart</c>).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Vector3> Mounts = new Dictionary<string, Vector3>
        {
            // Los tres de la zona 'cockpit' van separados EN ANCHURA, no solo en altura: con los
            // tres centrados, al girar el vehiculo de frente la profundidad deja de separar en
            // pantalla y los signos se montan. La dirección va a un puño y el freno al lado de su
            // maneta: quedan puño, pantalla y maneta, de izquierda a derecha.
            { "point_steering",  new Vector3(.86f, .94f, .15f) }, // manubrio, en un puño
            { "point_brakes",    new Vector3(.79f, .78f, .82f) }, // maneta de freno, al otro lado
            { "point_lights",    new Vector3(.95f, .62f, .50f) }, // faro delantero
            // Por encima de la rueda: dentro, el signo naranja caía sobre los radios naranjas del
            // contorno y no se veía.
            { "point_wheels",    new Vector3(.10f, .80f, .50f) }, // sobre la rueda
            { "point_electrics", new Vector3(.45f, .36f, .82f) }, // bateria, al otro lado
            { "point_deck",      new Vector3(.35f, .15f, .50f) }, // sobre la plataforma, atras
        };

        public static string AssetPath(Definition definition) =>
            $"{DataFolder}/SO_VehicleProfile_{definition.AssetSuffix}.asset";

        public static VehicleProfileSO Load(Definition definition) =>
            AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(AssetPath(definition));

        /// <summary>La definición del vehículo que elige ese valor de contexto.</summary>
        public static bool TryGet(string contextValue, out Definition definition)
        {
            foreach (Definition candidate in Definitions)
            {
                if (candidate.ContextValue == contextValue)
                {
                    definition = candidate;
                    return true;
                }
            }
            definition = default;
            return false;
        }

        /// <summary>Los perfiles que existen en disco, en el orden de <see cref="Definitions"/>.</summary>
        public static IEnumerable<VehicleProfileSO> LoadAll()
        {
            foreach (Definition definition in Definitions)
            {
                VehicleProfileSO profile = Load(definition);
                if (profile != null)
                    yield return profile;
            }
        }

        /// <summary>
        /// Las zonas de todos los vehículos, cada una una vez: las tres comunes y la plataforma,
        /// que solo es del scooter. Quien la arme solo con la e-bike deja la plataforma sin
        /// entidad, sin nombre en el reporte o sin colocar.
        /// </summary>
        public static IEnumerable<ZoneSO> AllInspectionZones()
        {
            HashSet<ZoneSO> seen = new HashSet<ZoneSO>();
            foreach (VehicleProfileSO profile in LoadAll())
                foreach (ZoneSO zone in profile.InspectionZones)
                    if (zone != null && seen.Add(zone))
                        yield return zone;
        }
    }
}
