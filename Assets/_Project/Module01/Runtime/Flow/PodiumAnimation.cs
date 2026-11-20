using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Lo que se mueve en el podio de cada punto del vehículo (Excel 11-13, pestaña «Componentes
    /// vehiculos sección 2»): las manetas se aprietan, la dirección gira a un lado y al otro, el
    /// display se enciende y la transmisión pedalea. Las luces no: sus texturas ya las traen
    /// encendidas (delantera blanca, trasera roja). Por nombre de pieza, que es el del FBX.
    /// </summary>
    public sealed class PodiumAnimation : MonoBehaviour
    {
        public const float SqueezeDegrees = 18f;
        public const float SqueezePeriod = 2f;
        public const float SteerDegrees = 30f;
        public const float SteerPeriod = 3.2f;
        public const float DisplayPeriod = 2.4f;   // apagado el primer tercio, encendido el resto
        public const float CrankDegreesPerSecond = 120f;
        public const float RearRatio = 1.23f;       // plato 8,7 cm / piñón 7,1 cm
        private const float FacingDegrees = 35f;    // la dirección, de tres cuartos y no de frente

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color DisplayOn = new Color(.85f, 1f, 1f) * 1.4f;

        private readonly List<(Transform Part, Quaternion Pose, float Sign)> _levers = new List<(Transform, Quaternion, float)>();
        private readonly List<(Transform Part, Quaternion Pose, float Speed)> _spinners = new List<(Transform, Quaternion, float)>();
        private readonly List<Material> _screens = new List<Material>();
        private Quaternion _steerRest;
        private Vector3 _steerAxis;
        private bool _steers;
        private float _time;

        public bool Animates => _levers.Count > 0 || _spinners.Count > 0 || _screens.Count > 0 || _steers;
        public bool ScreenOn { get; private set; }

        /// <summary>Pone a moverse lo presentado para <paramref name="pointId"/>. Null si al punto no le toca nada.</summary>
        public static PodiumAnimation Attach(GameObject model, string pointId, ItemInspector inspector)
        {
            if (model == null || string.IsNullOrEmpty(pointId))
                return null;
            PodiumAnimation anim = model.AddComponent<PodiumAnimation>();
            switch (pointId)
            {
                case "point_brakes": anim.FindLevers(); break;
                case "point_steering": anim.SetUpSteering(inspector); break;
                case "point_electrics": anim.FindScreens(); break;
                case "point_drivetrain": anim.FindDrivetrain(); break;
            }
            if (anim.Animates)
                return anim;
            Gone(anim);
            return null;
        }

        /// <summary>
        /// Las piezas de lo presentado, <b>sin la raíz</b>. Las piezas de la patineta llegan al podio
        /// en un contenedor que se llama como la primera («freno_de»), con el pivote donde estaba el
        /// vehículo: tomado por maneta, giraba 18° sobre ese pivote, a más de 2 m, y sacaba las dos
        /// manetas del podio (prueba de la PO, 2026-11-19).
        /// </summary>
        private IEnumerable<Transform> All()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t != transform)
                    yield return t;
        }

        private void FindLevers()
        {
            // El giro de cada maneta está en su bisagra (frenos.fbx, E_Scooter.fbx) y su eje es el
            // «arriba» del vehículo; la derecha y la izquierda giran al revés para ir hacia el puño.
            foreach (Transform t in All())
                if (t.name == "freno_de" || t.name == "freno_iz")
                    _levers.Add((t, t.localRotation, t.name == "freno_de" ? 1f : -1f));
        }

        private void FindDrivetrain()
        {
            foreach (Transform t in All())
            {
                if (t.name.StartsWith("piñon"))
                    _spinners.Add((t, t.localRotation, CrankDegreesPerSecond));
                else if (t.name == "anilli_de_cadena" || t.name == "manzana" || t.name == "freno_de_disco")
                    _spinners.Add((t, t.localRotation, CrankDegreesPerSecond * RearRatio));
            }
        }

        private void FindScreens()
        {
            // Solo la pantalla: en la e-bike la 'pantalla' es carcasa (e_bike) más pantalla (Pantalla).
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == InspectionHighlight.OutlineName)
                    continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                        continue;
                    string n = mats[i].name.ToLowerInvariant();
                    if (mats.Length > 1 && !n.Contains("display") && !n.Contains("pantalla"))
                        continue;
                    Material screen = new Material(mats[i]) { name = mats[i].name + " (podio)" };
                    screen.EnableKeyword("_EMISSION");
                    if (screen.HasProperty(BaseMapId))
                        screen.SetTexture(EmissionMapId, screen.GetTexture(BaseMapId));
                    mats[i] = screen;
                    _screens.Add(screen);
                    changed = true;
                }
                if (changed)
                    r.sharedMaterials = mats;
            }
            SetScreen(false);
        }

        private void SetUpSteering(ItemInspector inspector)
        {
            Transform wheel = null;
            foreach (Transform t in All())
                if (t.name == "llanta_delantera") { wheel = t; break; }
            if (wheel == null)
                return;
            Transform stage = inspector != null ? inspector.Stage : null;
            Vector3 up = stage != null ? stage.up : Vector3.up;

            // De tres cuartos hacia quien mira: el frente es hacia donde queda la rueda.
            Camera cam = Camera.main;
            Vector3 front = Vector3.ProjectOnPlane(wheel.position - transform.position, up);
            if (cam != null && front.sqrMagnitude > 1e-6f)
            {
                Vector3 toCam = Vector3.ProjectOnPlane(cam.transform.position - transform.position, up);
                float yaw = Vector3.SignedAngle(front, toCam, up) + FacingDegrees;
                Renderer[] rs = GetComponentsInChildren<Renderer>();
                Bounds bounds = rs.Length > 0 ? rs[0].bounds : new Bounds(transform.position, Vector3.zero);
                foreach (Renderer r in rs) bounds.Encapsulate(r.bounds);
                transform.RotateAround(bounds.center, up, yaw);
            }
            if (inspector != null)
                inspector.HoldStill(); // sin el giro del podio: que se vea el manubrio ir y volver

            // Eje de la dirección: del pivote del tenedor/manubrio (arriba) al eje de la rueda.
            Vector3 axisWorld = (transform.position - wheel.position).normalized;
            _steerAxis = transform.parent != null ? transform.parent.InverseTransformDirection(axisWorld) : axisWorld;
            _steerRest = transform.localRotation;
            _steers = true;
        }

        private void Update() => Tick(_time + Time.deltaTime);

        /// <summary>La pose de la animación en el instante <paramref name="time"/>. Público para los tests.</summary>
        public void Tick(float time)
        {
            _time = time;
            // Apretar y soltar: sube, se sostiene y baja; luego descansa.
            float squeeze = Mathf.Clamp01(Mathf.Sin(time / SqueezePeriod * Mathf.PI * 2f) * 1.6f);
            foreach (var (part, pose, sign) in _levers)
                if (part != null)
                    part.localRotation = pose * Quaternion.AngleAxis(sign * SqueezeDegrees * squeeze, Vector3.up);

            foreach (var (part, pose, speed) in _spinners)
                if (part != null)
                    part.localRotation = pose * Quaternion.AngleAxis(speed * time, Vector3.forward);

            if (_steers)
                transform.localRotation = Quaternion.AngleAxis(
                    SteerDegrees * Mathf.Sin(time / SteerPeriod * Mathf.PI * 2f), _steerAxis) * _steerRest;

            if (_screens.Count > 0)
                SetScreen(Mathf.Repeat(time, DisplayPeriod) > DisplayPeriod / 3f);
        }

        private void SetScreen(bool on)
        {
            ScreenOn = on;
            foreach (Material m in _screens)
                if (m != null)
                    m.SetColor(EmissionColorId, on ? DisplayOn : Color.black);
        }

        private void OnDestroy()
        {
            foreach (Material m in _screens)
                if (m != null)
                    Gone(m);
        }

        private static void Gone(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
