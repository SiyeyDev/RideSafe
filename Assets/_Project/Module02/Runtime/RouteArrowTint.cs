using UnityEngine;

namespace RideSafe.Module02
{
    /// <summary>Speed: verde/amarillo/rojo según velocidad (práctica). Neutral: solo dirección (prueba, pendiente de decidir).</summary>
    public enum ArrowColorMode { Speed, Neutral }

    /// <summary>
    /// Tiñe las flechas del suelo de un tramo según la velocidad que le pasen desde fuera.
    /// Va por <see cref="MaterialPropertyBlock"/> para no instanciar los materiales del arte.
    /// </summary>
    [AddComponentMenu("RideSafe/Module 02/Route Arrow Tint")]
    public class RouteArrowTint : MonoBehaviour
    {
        // Propiedades expuestas en "Animated racing arrows.shadergraph".
        private static readonly int ColorId = Shader.PropertyToID("_Color_arrow");
        private static readonly int FresnelId = Shader.PropertyToID("_Fresnel_Color");

        [SerializeField] private Renderer[] arrows = new Renderer[0];
        [SerializeField] private float speedLimitKmh = 15f;
        [Tooltip("Margen por encima del límite que todavía es amarillo.")]
        [SerializeField] private float toleranceKmh = 5f;
        [SerializeField] private ArrowColorMode mode = ArrowColorMode.Speed;

        // HDR como en los materiales del artista (verde 0.1, 1.5, 0).
        [SerializeField, ColorUsage(false, true)] private Color green = new Color(0.1f, 1.5f, 0f);
        [SerializeField, ColorUsage(false, true)] private Color yellow = new Color(1.5f, 1.1f, 0f);
        [SerializeField, ColorUsage(false, true)] private Color red = new Color(1.5f, 0.05f, 0f);
        [SerializeField, ColorUsage(false, true)] private Color neutral = new Color(1.2f, 1.2f, 1.2f);
        [Tooltip("En el material el fresnel es el color de la flecha x16; sin teñirlo, una flecha roja seguiría brillando verde.")]
        [SerializeField] private float fresnelBoost = 16f;

        private MaterialPropertyBlock _block;
        private Color? _applied;
        private float _lastKmh;

        public SpeedBand Band => SpeedBands.Classify(_lastKmh, speedLimitKmh, toleranceKmh);

        public ArrowColorMode Mode
        {
            get => mode;
            set { mode = value; SetSpeed(_lastKmh); }
        }

        private void Start() => SetSpeed(_lastKmh);

        /// <summary>Velocidad actual del vehículo en km/h. Solo toca los renderers si cambia el color.</summary>
        public void SetSpeed(float kmh)
        {
            _lastKmh = kmh;
            Color target = mode == ArrowColorMode.Neutral ? neutral : Band switch
            {
                SpeedBand.Green => green,
                SpeedBand.Yellow => yellow,
                _ => red,
            };
            if (_applied == target) return;
            _applied = target;

            _block ??= new MaterialPropertyBlock();
            foreach (var r in arrows)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(ColorId, target);
                _block.SetColor(FresnelId, target * fresnelBoost);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
