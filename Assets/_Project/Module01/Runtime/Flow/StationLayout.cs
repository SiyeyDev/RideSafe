using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Los muebles de una mesa de EPP: donde se apoyan los signos «!» de lo que el artista todavía
    /// no ha modelado. Los EPP con modelo van donde él los dejó (2026-10-27).
    /// </summary>
    public readonly struct Station
    {
        public Station(string zoneId, params Surface[] surfaces)
        {
            ZoneId = zoneId;
            Surfaces = surfaces;
        }

        /// <summary>Zona del catálogo, p. ej. <c>protection</c>.</summary>
        public string ZoneId { get; }

        /// <summary>
        /// Sus muebles, en orden: un signo va al primero con hueco. Más de uno cuando el artista
        /// llenó el primero, como <c>otra mesa</c> con los EPP de «Visibilidad y control».
        /// </summary>
        public IReadOnlyList<Surface> Surfaces { get; }
    }

    /// <summary>
    /// Un <b>nivel</b> de un mueble del garaje: su altura y el trozo de planta que de verdad es
    /// tapa.
    /// <para>
    /// Un mueble puede tener varios: la mesa de la entrada tiene la tapa a 1,01 y un estante a
    /// 1,75, y el banco de herramientas tiene la tapa a 0,89 y dos entrepaños debajo. Se declaran
    /// como <see cref="Surface"/> distintas con el <b>mismo</b> <see cref="Name"/>: el mueble es
    /// un único renderer sin hijos, así que no hay otro objeto al que apuntar.
    /// </para>
    /// </summary>
    public readonly struct Surface
    {
        public Surface(string name, float worktopHeight, float usableFrom = float.NegativeInfinity,
                       float usableTo = float.PositiveInfinity,
                       float depthFrom = float.NegativeInfinity,
                       float depthTo = float.PositiveInfinity)
        {
            Name = name;
            WorktopHeight = worktopHeight;
            UsableFrom = usableFrom;
            UsableTo = usableTo;
            DepthFrom = depthFrom;
            DepthTo = depthTo;
        }

        /// <summary>
        /// El tramo del mueble, a lo largo de su lado largo, que de verdad es tapa. Sin declarar,
        /// todo. Hace falta porque algunos muebles traen otra cosa en la misma malla: el armario
        /// alto del rincón es parte de <c>cabinet (1)</c>, y los signos acababan flotando delante
        /// de sus puertas (PO, 2026-10-27).
        /// </summary>
        public float UsableFrom { get; }

        public float UsableTo { get; }

        /// <summary>
        /// Lo mismo para el lado <b>corto</b>, el fondo. Sin declarar, todo el fondo del mueble.
        /// <para>
        /// Hace falta para los estantes, que casi nunca son tan hondos como la tapa: el de la
        /// mesa de la entrada mide 0,38 m contra los 0,75 de la tapa, y sin recortar el reparto
        /// creía tener 0,75 y dejaba las piezas hasta 0,37 m por delante del borde, en el aire.
        /// </para>
        /// </summary>
        public float DepthFrom { get; }

        public float DepthTo { get; }

        /// <summary>La caja del mueble recortada a la planta de ESTE nivel.</summary>
        public Bounds Clip(Bounds furniture)
        {
            // El eje largo es el del mueble entero, no el del nivel: un estante estrecho dentro
            // de una mesa larga sigue siendo «largo en Z», aunque su propia caja ya no lo diga.
            bool alongX = furniture.size.x >= furniture.size.z;
            float min = Mathf.Max(alongX ? furniture.min.x : furniture.min.z, UsableFrom);
            float max = Mathf.Min(alongX ? furniture.max.x : furniture.max.z, UsableTo);
            float fondoMin = Mathf.Max(alongX ? furniture.min.z : furniture.min.x, DepthFrom);
            float fondoMax = Mathf.Min(alongX ? furniture.max.z : furniture.max.x, DepthTo);
            Vector3 lo = furniture.min, hi = furniture.max;
            if (alongX) { lo.x = min; hi.x = max; lo.z = fondoMin; hi.z = fondoMax; }
            else { lo.z = min; hi.z = max; lo.x = fondoMin; hi.x = fondoMax; }
            Bounds clipped = new Bounds();
            clipped.SetMinMax(lo, hi);
            return clipped;
        }

        /// <summary>Objeto del arte que hace de mueble, p. ej. <c>otra mesa</c>.</summary>
        public string Name { get; }

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
    }

    /// <summary>El mueble de cada parada del recorrido de EPP y desde dónde se mira.</summary>
    public static class Module01Stations
    {
        /// <summary>
        /// Lo más cerca que se planta el aprendiz de cualquier elemento de su parada. Por encima
        /// del radio de la burbuja de foco (1,6), para que al elegir uno el mueble se oscurezca y
        /// solo quede en luz la copia del pedestal.
        /// </summary>
        public const float ViewDistance = 1.90f;

        /// <summary>Altura de los ojos de pie. La misma que usan las zonas del vehículo.</summary>
        public const float EyeHeight = 1.45f;

        /// <summary>
        /// Media anchura visible por cada metro de distancia: 60° verticales a 16:9 dan un campo
        /// horizontal de 91°, o sea <c>tan(45.7°)</c>. Es lo que convierte «el mueble mide 3,7 m»
        /// en «a esta distancia entra entero».
        /// </summary>
        public const float HalfWidthPerMeter = 1.03f;

        /// <summary>
        /// El mueble de cada parada (las paradas están en <c>Module01Library.Stops</c>). Las tapas se
        /// midieron en la escena con lo que el artista dejó apoyado encima. El mostrador de la ropa
        /// son dos muebles seguidos: se mira el conjunto.
        /// </summary>
        private static readonly Station[] Table =
        {
            // Dos niveles, medidos el 2026-10-29 recorriendo los triángulos de la malla con
            // «Read/Write Enabled» puesto un momento y quitado después: caras horizontales a
            // y=1,010 (2,78 m², x[3,82..4,57], z[2,30..6,01]) y a y=1,750 (1,39 m²,
            // x[4,19..4,57], z[2,30..6,01]). El de arriba es el estante que la PO venía usando
            // —"ese me servía super bien"— y que el reparto nunca vio porque no estaba
            // declarado: lo que ella puso ahí acababa bajando a la tapa.
            // Hay dos niveles más abajo (0,850 y 0,190) que no se declaran: quedan por debajo
            // de la cintura y el recorrido mira la mesa de frente, no desde arriba.
            new Station("table_entrance",
                        new Surface("mesa", 1.01f, depthFrom: 3.82f, depthTo: 4.57f),
                        new Surface("mesa", 1.75f, depthFrom: 4.19f, depthTo: 4.57f)),
            new Station("table_middle", new Surface("otra mesa", 1.04f)),
            new Station("table_back", new Surface("mesa nueva", 1.02f)),
            // cabinet (1) lleva el armario alto del rincón, macizo de suelo a techo. Dónde acaba
            // se midió otra vez el 2026-10-29, recorriendo los vértices de su malla por franjas
            // de 25 cm: solo X[-3,95 , -3,70] no tiene tapa; desde -3,45 la hay, a cota 1,02.
            // Estaba declarado en -2,89 y eso tiraba **0,56 m** de mostrador bueno —casi un
            // octavo del total de la parada—, que es el hueco vacío que la PO señaló en la
            // captura. El dato viejo venía de las manillas de las puertas, no de la tapa.
            new Station("counter_clothes", new Surface("cabinet (1)", 1.08f, usableFrom: -3.45f),
                        new Surface("cabinetmid 1", 1.08f)),
            // La mesa de herramientas vuelve a ser parada (PO, 2026-10-29): con 61 elementos en
            // cuatro muebles la mesa del fondo iba al 94 % de su tapa, y esta estaba vacía. No
            // lleva alacena encima ni panel detrás, al contrario que las otras, cuya caja sube
            // hasta 2,43.
            // La tapa estaba declarada en 0,93, que es la cota más ALTA de su caja; midiendo las
            // caras horizontales sale 0,890 (2,75 m², x[-4,83..-3,96], z[0,77..3,91]), o sea que
            // todo lo de esta mesa flotaba 4 cm. Tiene además dos entrepaños, a 0,570 y a 0,320,
            // que de momento no se declaran: en el de abajo están las tres reglas que mueve
            // `Module01ArtEpp.Moves` a y=0,30 y chocarían.
            new Station("table_tools", new Surface("metal table", 0.89f)),
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
    /// La aritmética de las mesas de EPP: dónde cabe un signo sin pisar nada y desde dónde se
    /// mira. Aparte del montador de escena a propósito, para poder afirmarla sin abrir nada.
    /// </summary>
    public static class StationLayout
    {
        /// <summary>
        /// Lo que se deja libre en cada punta de la tapa. Un elemento con su borde justo en el
        /// canto del mueble se lee como que se va a caer.
        /// </summary>
        public const float EdgeInset = .10f;

        /// <summary>Paso de la rejilla con la que se buscan huecos y puntos de vista.</summary>
        private const float SearchStep = .05f, ViewStep = .10f;

        /// <summary>Aire que se deja alrededor de un signo para que no se lea pegado a nada.</summary>
        private const float SpotMargin = .04f;

        /// <summary>El aprendiz de pie, como caja: 60 cm de ancho y 1,80 de alto.</summary>
        private const float BodyRadius = .30f, BodyHeight = 1.80f;

        /// <summary>
        /// Hasta <paramref name="count"/> sitios libres sobre la tapa para objetos de
        /// <paramref name="size"/>, del más cercano a <paramref name="near"/> al más lejano.
        /// <para>
        /// Es para lo que el artista no colocó —el signo «!» de lo que aún no tiene modelo—: la
        /// mesa ya trae sus EPP puestos, así que no se reparte una fila, se buscan los huecos que
        /// quedan. Devuelve menos de los pedidos si no caben; inventarse sitio sería ponerlos
        /// encima de algo.
        /// </para>
        /// </summary>
        public static List<Vector3> FreeSpots(Bounds surface, float worktop, Vector3 size, int count,
            IReadOnlyList<Bounds> occupied, Vector3 near)
        {
            var taken = new List<Bounds>();
            var spots = new List<Vector3>();
            Vector3 padded = size + Vector3.one * (2f * SpotMargin);
            foreach (Vector3 c in FreeCandidates(surface, worktop, size, occupied, near))
            {
                if (spots.Count >= count)
                    break;
                if (Hits(new Bounds(c, padded), taken))
                    continue;
                spots.Add(c);
                taken.Add(new Bounds(c, size));
            }
            return spots;
        }

        /// <summary>
        /// Todos los sitios de la tapa donde cabe un objeto de <paramref name="size"/> sin pisar
        /// <paramref name="occupied"/>, del más cercano a <paramref name="near"/> al más lejano.
        /// Se pisan entre ellos: son para elegir uno que además cumpla otra cosa.
        /// </summary>
        public static List<Vector3> FreeCandidates(Bounds surface, float worktop, Vector3 size,
            IReadOnlyList<Bounds> occupied, Vector3 near)
        {
            var candidates = new List<Vector3>();
            float y = worktop + size.y * .5f;
            float minX = surface.min.x + EdgeInset + size.x * .5f, maxX = surface.max.x - EdgeInset - size.x * .5f;
            float minZ = surface.min.z + EdgeInset + size.z * .5f, maxZ = surface.max.z - EdgeInset - size.z * .5f;
            Vector3 padded = size + Vector3.one * (2f * SpotMargin);
            for (float x = minX; x <= maxX + 1e-4f; x += SearchStep)
                for (float z = minZ; z <= maxZ + 1e-4f; z += SearchStep)
                {
                    Vector3 c = new Vector3(x, y, z);
                    if (!Hits(new Bounds(c, padded), occupied))
                        candidates.Add(c);
                }

            candidates.Sort((a, b) => Flat(a - near).sqrMagnitude.CompareTo(Flat(b - near).sqrMagnitude));
            return candidates;
        }

        /// <summary>
        /// Desde dónde se mira un mueble: <b>de frente y centrado</b>, a la distancia justa para
        /// que entre entero en pantalla (PO, 2026-10-27: «prefiero que me muestre centrado cada
        /// mesa»).
        /// <para>
        /// De frente quiere decir por su lado largo, el que da al garaje: el eje corto de la caja
        /// es el fondo del mueble, y se mira desde el lado de <paramref name="floor"/>. Se aleja
        /// lo que haga falta para que ningún elemento quede a menos de
        /// <paramref name="minDistance"/> —la burbuja de foco— y para que el cuerpo no caiga dentro
        /// de nada de <paramref name="blocked"/>, sin salirse de la planta.
        /// </para>
        /// </summary>
        public static bool TryFrontView(Bounds furniture, IReadOnlyList<Bounds> items, IReadOnlyList<Bounds> blocked,
            Bounds floor, float minDistance, float eyeHeight, out Pose pose)
        {
            bool depthAlongX = furniture.size.x < furniture.size.z;
            Vector3 outward = depthAlongX
                ? new Vector3(Mathf.Sign(floor.center.x - furniture.center.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(floor.center.z - furniture.center.z));
            float width = depthAlongX ? furniture.size.z : furniture.size.x;
            float depth = depthAlongX ? furniture.size.x : furniture.size.z;

            Vector3 front = new Vector3(furniture.center.x, eyeHeight, furniture.center.z) + outward * (depth * .5f);
            Quaternion look = Quaternion.LookRotation(-outward, Vector3.up);

            for (float d = width * .5f * FrameMargin / Module01Stations.HalfWidthPerMeter; ; d += ViewStep)
            {
                Vector3 eye = front + outward * d;
                if (eye.x < floor.min.x + BodyRadius || eye.x > floor.max.x - BodyRadius ||
                    eye.z < floor.min.z + BodyRadius || eye.z > floor.max.z - BodyRadius)
                {
                    pose = default;
                    return false;
                }

                bool tooClose = false;
                if (items != null)
                    foreach (Bounds item in items)
                        if (Vector3.Distance(eye, item.ClosestPoint(eye)) < minDistance) { tooClose = true; break; }
                if (tooClose)
                    continue;

                Bounds body = new Bounds(new Vector3(eye.x, BodyHeight * .5f, eye.z),
                    new Vector3(2f * BodyRadius, BodyHeight, 2f * BodyRadius));
                if (Hits(body, blocked))
                    continue;

                pose = new Pose(eye, look);
                return true;
            }
        }

        /// <summary>Holgura del encuadre: el mueble ocupa el 90 % del ancho, no llega al borde.</summary>
        private const float FrameMargin = 1.1f;

        /// <summary>
        /// Cuántos elementos quedan tapados desde <paramref name="eye"/>: algo de
        /// <paramref name="occluders"/> u otro elemento corta la línea hasta su centro. Lo que
        /// toca al propio elemento no cuenta —son sus piezas o su vecino pegado—. Deja de contar
        /// al llegar a <paramref name="stopAt"/>: con eso ya pierde.
        /// </summary>
        public static int Hidden(Vector3 eye, IReadOnlyList<Bounds> items, IReadOnlyList<Bounds> occluders) =>
            Hidden(eye, items, occluders, int.MaxValue);

        /// <summary>
        /// Holgura alrededor de lo que tapa: 1 cm. Un rayo que pasa a milímetros de un borde se lo come
        /// en cuanto el aprendiz mueve la cabeza: medido, el de los audífonos rozaba las gafas.
        /// </summary>
        private const float OcclusionMargin = .01f;

        private static int Hidden(Vector3 eye, IReadOnlyList<Bounds> items, IReadOnlyList<Bounds> occluders, int stopAt)
        {
            int hidden = 0;
            foreach (Bounds item in items)
            {
                bool seen = false;
                foreach (Vector3 point in SamplePoints(item))
                {
                    Vector3 to = point - eye;
                    var ray = new Ray(eye, to);
                    if (!Blocks(ray, to.magnitude, item, occluders) && !Blocks(ray, to.magnitude, item, items))
                    {
                        seen = true;
                        break;
                    }
                }
                if (!seen && ++hidden > stopAt)
                    break;
            }
            return hidden;
        }

        /// <summary>
        /// Los puntos de un objeto a los que se mira para pulsarlo: el centro, el de arriba y
        /// los cuatro costados. Con que se vea uno se puede pulsar, que es lo que hace una
        /// persona cuando otro objeto le tapa parte.
        /// </summary>
        public static IEnumerable<Vector3> SamplePoints(Bounds b)
        {
            Vector3 c = b.center, e = b.extents * .7f;
            yield return c;
            yield return c + new Vector3(0f, e.y, 0f);
            yield return c + new Vector3(e.x, 0f, 0f);
            yield return c - new Vector3(e.x, 0f, 0f);
            yield return c + new Vector3(0f, 0f, e.z);
            yield return c - new Vector3(0f, 0f, e.z);
        }

        private static bool Blocks(Ray ray, float reach, Bounds target, IReadOnlyList<Bounds> boxes)
        {
            if (boxes == null)
                return false;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Intersects(target))
                    continue;
                Bounds padded = boxes[i];
                padded.Expand(2f * OcclusionMargin);
                if (padded.IntersectRay(ray, out float d) && d < reach)
                    return true;
            }
            return false;
        }

        private static bool Hits(Bounds probe, IReadOnlyList<Bounds> boxes)
        {
            if (boxes == null)
                return false;
            for (int i = 0; i < boxes.Count; i++)
                if (boxes[i].Intersects(probe))
                    return true;
            return false;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
