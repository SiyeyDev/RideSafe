using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>Por qué eje se reparten en fila los elementos de una estación.</summary>
    public enum StationAxis
    {
        /// <summary>A lo ancho. Para los muebles de la pared del fondo.</summary>
        X,

        /// <summary>En profundidad. Para los muebles de las paredes laterales.</summary>
        Z,
    }

    /// <summary>
    /// Dónde vive una zona de inspección dentro del garaje: sobre qué mueble se apoyan sus
    /// elementos y desde dónde los mira el aprendiz.
    /// </summary>
    public readonly struct Station
    {
        public Station(string zoneId, string surfaceName, float worktopHeight,
                       StationAxis axis, Vector3 outward)
        {
            ZoneId = zoneId;
            SurfaceName = surfaceName;
            WorktopHeight = worktopHeight;
            Axis = axis;
            Outward = outward.normalized;
        }

        /// <summary>Zona del catálogo, p. ej. <c>clothing</c>.</summary>
        public string ZoneId { get; }

        /// <summary>Objeto del arte que hace de mueble, p. ej. <c>metal table</c>.</summary>
        public string SurfaceName { get; }

        /// <summary>
        /// Altura de la tapa, en metros de mundo.
        /// <para>
        /// Es dato y no se deriva porque <b>cada mueble del garaje es un único renderer</b>:
        /// no hay una sub-malla «tapa» que medir, y la caja del mueble incluye las alacenas de
        /// arriba —la del banco de la derecha llega a 2.43—. Los números salen de medir qué
        /// altura tiene lo que el arte ya dejó apoyado encima; un test comprueba que la tapa
        /// cae dentro de la caja del mueble, para que una entrega nueva que cambie el
        /// mobiliario no pase en silencio.
        /// </para>
        /// </summary>
        public float WorktopHeight { get; }

        /// <summary>Eje por el que se reparten los elementos en fila.</summary>
        public StationAxis Axis { get; }

        /// <summary>Hacia qué lado del mueble se planta el aprendiz. Horizontal y unitario.</summary>
        public Vector3 Outward { get; }
    }

    /// <summary>
    /// El reparto de las tres zonas personales por el garaje.
    /// <para>
    /// Hasta ahora las tres estaban en el mismo mostrador del fondo y el aprendiz se
    /// teletransportaba tres veces para mirar siempre a la misma pared. Lo pidió Charlie el
    /// 2026-10-14: «tenemos un entorno bonito, podemos usarlo».
    /// </para>
    /// </summary>
    public static class Module01Stations
    {
        /// <summary>Hueco entre dos elementos de la misma estación, en metros.</summary>
        public const float Gap = .34f;

        /// <summary>A qué distancia del grupo se planta el aprendiz. La de siempre.</summary>
        public const float ViewDistance = 1.90f;

        /// <summary>Altura de los ojos de pie. La misma que usan las zonas del vehículo.</summary>
        public const float EyeHeight = 1.45f;

        /// <summary>Hueco a dejar entre el grupo y los bordes de la mitad limpia.</summary>
        public const float HudClearance = .25f;

        /// <summary>
        /// Hasta qué fracción del ancho de pantalla llega la parte limpia. El checklist empieza
        /// en el 55 % y tapa de ahí a la derecha; medido en las capturas de PlayMode.
        /// </summary>
        public const float HudClearFraction = .55f;

        /// <summary>
        /// Media anchura visible por cada metro de distancia: 60° verticales a 16:9 dan un
        /// campo horizontal de 91°, o sea <c>tan(45.7°)</c>. Es lo que convierte «el grupo mide
        /// 1.70 m» en «cabe o no cabe en lo que se ve».
        /// </summary>
        public const float HalfWidthPerMeter = 1.03f;

        private static readonly Station[] Table =
        {
            // ---- Las tres mesas de la biblioteca del cliente (2026-10-22) -------------
            //
            // Sustituyen a las cuatro estaciones de antes (head / clothing / trousers / load),
            // que no salían de ningún documento. Medido en la escena, la tapa útil de cada
            // mueble —caras horizontales a altura de encimera, no la caja del objeto—:
            //
            //   mostrador del fondo  cabinet (1) + cabinetmid 1 + cabinet = 7,69 m seguidos
            //   pared derecha        mesa nueva + otra mesa + mesa       = 9,12 m con 2 juntas
            //   pared izquierda      metal table                         = 2,61 m
            //
            // Con el ancho medio medido de los EPP (0,349 m) y los huecos de StationLayout,
            // 10 elementos en una fila piden 4,59 m y 11 piden 5,04: no cabe ninguno de los
            // tres en una sola fila sobre un mueble. En DOS filas —lo plano delante, lo alto
            // detrás, como ya se resolvió la ropa— bajan a 2,35 y 2,79, y ahí sí entran.
            //
            // Por eso cada mesa va a UN mueble con holgura para dos filas, y no repartida
            // entre varios: un grupo partido en dos muebles con una junta en medio se lee como
            // dos estaciones distintas, y el recorrido las trata como una.
            //
            // «Preparación del trayecto» se lleva el mueble más largo a propósito: ahí caen el
            // poncho, la alforja, la mochila y el paraguas, que son los objetos más grandes de
            // toda la hoja y para los que la media de 0,349 se queda corta.
            new Station("protection", "cabinet", 1.08f, StationAxis.X, Vector3.forward),
            new Station("control", "otra mesa", 1.04f, StationAxis.Z, Vector3.left),
            new Station("readiness", "mesa", 1.02f, StationAxis.Z, Vector3.left),
        };

        /// <summary>Todas las estaciones, en el orden del recorrido.</summary>
        public static IReadOnlyList<Station> All => Table;

        /// <summary>La estación de esa zona, si está declarada.</summary>
        public static bool TryGet(string zoneId, out Station station)
        {
            string wanted = string.IsNullOrWhiteSpace(zoneId) ? string.Empty : zoneId.Trim().ToLowerInvariant();
            foreach (Station s in Table)
            {
                if (s.ZoneId != wanted)
                    continue;
                station = s;
                return true;
            }
            station = default;
            return false;
        }
    }

    /// <summary>
    /// La aritmética de colocar una estación: la fila sobre la tapa y el punto desde el que se
    /// mira. Aparte del montador de escena a propósito, para poder afirmarla sin abrir nada.
    /// </summary>
    public static class StationLayout
    {
        /// <summary>
        /// Hueco mínimo al que se puede apretar la fila. Por debajo de esto dos elementos se
        /// leen como uno solo y el aprendiz no sabe cuántas cosas hay que revisar.
        /// </summary>
        public const float MinGap = .10f;

        /// <summary>
        /// Lo que se deja libre en cada punta de la tapa. Un elemento con su borde justo en el
        /// canto del mueble se lee como que se va a caer.
        /// </summary>
        public const float EdgeInset = .10f;

        /// <summary>
        /// El hueco que de verdad cabe: el declarado si la fila entra en la tapa, y si no el
        /// que reparte el sobrante entre los huecos, nunca por debajo de <see cref="MinGap"/>.
        /// <para>
        /// El hueco declarado es un <b>máximo cómodo</b>, no una medida sagrada: apretar la fila
        /// es mejor que dejarla colgando por los lados, que es lo que pasaba antes —medido, los
        /// 6 elementos de ropa pedían 2.81 m sobre una mesa de 2.62 y las botas se salían—.
        /// </para>
        /// <para>
        /// Devuelve el declarado tal cual con menos de dos elementos: sin huecos no hay nada
        /// que repartir.
        /// </para>
        /// </summary>
        public static float FittingGap(
            Bounds surface, StationAxis axis, IReadOnlyList<Vector3> sizes, float declaredGap)
        {
            if (sizes == null || sizes.Count < 2)
                return declaredGap;

            float room = (axis == StationAxis.X ? surface.size.x : surface.size.z) - 2f * EdgeInset;

            float occupied = 0f;
            for (int i = 0; i < sizes.Count; i++)
                occupied += Along(sizes[i], axis);

            float spare = room - occupied;
            if (spare <= 0f)
                return MinGap;

            return Mathf.Clamp(spare / (sizes.Count - 1), MinGap, declaredGap);
        }

        /// <summary>
        /// Altura por debajo de la cual un elemento <b>no puede tapar nada</b>: puesto delante
        /// de otro, la vista y el rayo del clic le pasan por encima.
        /// <para>
        /// Los pantalones doblados del v9 miden 5 cm de canto. Un elemento asi en la fila de
        /// delante no esconde al de detras, y eso es lo que permite usar el fondo de la tapa en
        /// vez de alargar la fila hasta que cuelgue por los lados.
        /// </para>
        /// </summary>
        public const float FlatHeight = .12f;

        /// <summary>¿Este elemento es lo bastante plano para ir en la fila de delante?</summary>
        public static bool IsFlat(Vector3 size) => size.y < FlatHeight;

        /// <summary>
        /// La misma tapa, corrida <paramref name="offset"/> metros hacia el lado del aprendiz.
        /// Sirve para sacar dos filas a distinta profundidad reusando <see cref="Arrange"/>.
        /// </summary>
        public static Bounds Rank(Bounds surface, Vector3 outward, float offset)
        {
            Vector3 flat = new Vector3(outward.x, 0f, outward.z);
            flat = flat.sqrMagnitude < .0001f ? Vector3.forward : flat.normalized;
            return new Bounds(surface.center + flat * offset, surface.size);
        }

        /// <summary>
        /// Cuánto se corre cada fila respecto al centro de la tapa, como fracción del fondo.
        /// Dos filas a 0.22 del fondo quedan separadas casi la mitad del mueble.
        /// </summary>
        public const float RankOffset = .22f;

        /// <summary>Cuánto ocupa la fila entera, con sus huecos. Para saber si cabe.</summary>
        public static float Span(IReadOnlyList<Vector3> sizes, StationAxis axis, float gap)
        {
            if (sizes == null || sizes.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < sizes.Count; i++)
            {
                total += Along(sizes[i], axis);
                if (i < sizes.Count - 1)
                    total += gap;
            }
            return total;
        }

        /// <summary>
        /// Coloca los elementos en fila sobre la tapa, centrados en el mueble.
        /// <para>
        /// Devuelve el <b>centro de la caja visible</b> de cada uno, no la posición de su
        /// transform: el pivote de un FBX casi nunca está en el centro de la malla, así que
        /// quien lo aplique tiene que mover por la diferencia, no asignar la posición.
        /// </para>
        /// </summary>
        public static Vector3[] Arrange(
            Bounds surface, float worktop, StationAxis axis, IReadOnlyList<Vector3> sizes, float gap)
        {
            if (sizes == null || sizes.Count == 0)
                return new Vector3[0];

            float span = Span(sizes, axis, gap);
            float room = axis == StationAxis.X ? surface.size.x : surface.size.z;

            // Centrada si cabe; si no, ARRANCA EN EL BORDE del mueble. Centrar una fila mas
            // larga que la tapa la deja colgando por los dos lados: medido, con los pantalones
            // en la zona de ropa las botas quedaban 32 cm fuera de la mesa, flotando. Como los
            // elementos van en el orden del catalogo y los altos -los que acaban en el suelo-
            // van al final, arrancar por el borde deja en la tapa justo a los que les toca.
            float start = span <= room
                ? Along(surface.center, axis) - span * .5f
                : Along(surface.center, axis) - room * .5f;

            float across = axis == StationAxis.X ? surface.center.z : surface.center.x;

            Vector3[] spots = new Vector3[sizes.Count];
            float cursor = start;
            for (int i = 0; i < sizes.Count; i++)
            {
                float half = Along(sizes[i], axis) * .5f;
                float at = cursor + half;
                spots[i] = axis == StationAxis.X
                    ? new Vector3(at, worktop + sizes[i].y * .5f, across)
                    : new Vector3(across, worktop + sizes[i].y * .5f, at);
                cursor = at + half + gap;
            }
            return spots;
        }

        /// <summary>
        /// Dónde se planta el aprendiz para mirar el grupo: a <paramref name="distance"/> por el
        /// lado libre del mueble, a la altura de los ojos y <b>en horizontal</b>.
        /// <para>
        /// En horizontal y no apuntando al centro del grupo porque en VR el cabeceo lo pone el
        /// visor, no el rig —inclinar el ancla no es algo que el aprendiz llegue a ver— y
        /// porque al inclinarse crece la caja alineada con los ejes del pedestal del inspector,
        /// que acaba comiéndose el techo del popup. Ya se probó y fue peor.
        /// </para>
        /// </summary>
        public static Pose ViewPoint(
            Bounds group, Vector3 outward, float minDistance, float eyeHeight, float clearance)
        {
            Vector3 flat = new Vector3(outward.x, 0f, outward.z);
            flat = flat.sqrMagnitude < .0001f ? Vector3.forward : flat.normalized;

            Quaternion look = Quaternion.LookRotation(-flat, Vector3.up);
            Vector3 right = look * Vector3.right;

            // Cuánto mide el grupo en la direccion en la que el checklist se lo comeria. Como
            // right es paralelo a un eje, basta proyectar las medias aristas.
            float half = Mathf.Abs(right.x) * group.extents.x + Mathf.Abs(right.z) * group.extents.z;

            // Alejarse lo justo para que el grupo quepa en la mitad limpia. La fila de ropa son
            // 1.70 m y a 1.90 m no cabia: las botas se salian por el borde izquierdo.
            float need = (2f * half + 2f * clearance) / ClearWidthPerMeter;
            float distance = Mathf.Max(minDistance, need);

            // Y plantarse de modo que el grupo quede CENTRADO en esa mitad limpia. Empujarlo
            // todo lo posible hacia la izquierda -que fue el intento anterior- lo saca por el
            // borde en cuanto la fila crece; centrarlo reparte el hueco a los dos lados.
            float halfWidth = Module01Stations.HalfWidthPerMeter * distance;
            float clearMax = (2f * Module01Stations.HudClearFraction - 1f) * halfWidth;   // donde empieza el panel
            float clearCenter = (-halfWidth + clearMax) * .5f;

            Vector3 eye = group.center + flat * distance - right * clearCenter;
            eye.y = eyeHeight;
            return new Pose(eye, look);
        }

        /// <summary>Anchura util —la que no tapa el checklist— por metro de distancia.</summary>
        public static float ClearWidthPerMeter =>
            (1f + (2f * Module01Stations.HudClearFraction - 1f)) * Module01Stations.HalfWidthPerMeter;

        /// <summary>
        /// Baja al suelo un elemento demasiado alto para la tapa, y lo adelanta lo justo para
        /// que no se meta dentro del mueble.
        /// <para>
        /// <b>Sigue en su sitio de la fila.</b> Se probó a ponerlos en una franja por delante de
        /// todo el mueble y era peor: un pantalón de 1.16 m plantado delante de unas botas que
        /// descansan a 0.92 las tapa. Cada elemento conserva su hueco a lo largo del eje, y lo
        /// único que cambia es a qué altura se apoya.
        /// </para>
        /// </summary>
        public static Vector3 OnFloor(
            Vector3 spot, Vector3 size, Bounds surface, float floorY, Vector3 outward, float standoff)
        {
            Vector3 flat = new Vector3(outward.x, 0f, outward.z);
            flat = flat.sqrMagnitude < .0001f ? Vector3.forward : flat.normalized;

            bool alongX = Mathf.Abs(flat.x) > Mathf.Abs(flat.z);
            float depth = alongX ? surface.size.x : surface.size.z;
            float reach = alongX ? size.x : size.z;

            // Pegado a la cara libre del mueble, no en su centro: ahi dentro estan los cajones.
            Vector3 front = surface.center + flat * (depth * .5f + standoff + reach * .5f);

            return new Vector3(
                alongX ? front.x : spot.x,
                floorY + size.y * .5f,
                alongX ? spot.z : front.z);
        }

        /// <summary>
        /// Altura a partir de la cual un elemento se planta en el suelo en vez de sobre la
        /// tapa. Los pantalones miden de 0.92 a 1.16 m; el chaleco, el más alto de los de mesa,
        /// 0.60. Decide la altura y no el nombre: una entrega nueva con otra prenda alta cae
        /// sola en el sitio correcto.
        /// </summary>
        public const float FloorHeight = .70f;

        /// <summary>¿Este elemento va de pie en el suelo?</summary>
        public static bool StandsOnFloor(Vector3 size) => size.y >= FloorHeight;

        private static float Along(Vector3 v, StationAxis axis) => axis == StationAxis.X ? v.x : v.z;
    }
}

// v9: dos filas en la estacion de ropa.
