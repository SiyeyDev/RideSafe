using RideSafe.UI;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Las decisiones que tomaría una pasada por el módulo 0, escritas en un asset para que
    /// una demo o un test puedan saltarse sus pantallas y entrar al módulo 1 con parámetros
    /// reales en vez de valores improvisados en código.
    /// <para>
    /// Asignado a <c>Module01Experience._quickStart</c>, el botón Begin va directo a la puerta
    /// del garaje. Con ese campo vacío corre el onboarding completo.
    /// </para>
    /// <para>
    /// <see cref="Vehicle"/> y <see cref="Jurisdiction"/> son los <c>Id</c> de las tarjetas de
    /// sus <c>ChoicePanel</c>, no texto libre: <c>ContextRequirement</c> los compara tal cual.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "RideSafe/Modulo 0/Parametros de prueba", fileName = "SO_Module00_TestSettings")]
    public class Module00SettingsSO : ScriptableObject
    {
        [Tooltip("Código que recibe I2. Acepta regional ('es-CO') o corto ('es', 'en').")]
        [SerializeField] private string _language = "es-CO";
        [Tooltip("Id de tarjeta del panel Jurisdiction: bogota, florida.")]
        [SerializeField] private string _jurisdiction = "bogota";
        [Tooltip("Id de tarjeta del panel Vehicle: ebike, escooter. Decide qué secuencias corren.")]
        [SerializeField] private string _vehicle = "ebike";
        [SerializeField] private RidingPosture _posture = RidingPosture.Seated;
        [SerializeField] private bool _subtitles = true;
        [SerializeField, Range(50f, 200f)] private float _textScalePercent = 100f;
        [SerializeField] private DominantHand _hand = DominantHand.Right;

        public string Language => _language;
        public string Jurisdiction => _jurisdiction;
        public string Vehicle => _vehicle;

        /// <summary>El mismo paquete que emite <c>ComfortSettingsPanel.onStart</c>.</summary>
        public ComfortSettings Comfort => new ComfortSettings
        {
            Posture = _posture,
            Subtitles = _subtitles,
            TextScalePercent = _textScalePercent,
            Hand = _hand
        };
    }
}
