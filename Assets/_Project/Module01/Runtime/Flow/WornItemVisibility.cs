using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Decide si el objeto de un elemento se ve en la mesa o no.
    /// <para>
    /// Dos reglas, las dos pedidas en la reunión:
    /// </para>
    /// <list type="bullet">
    /// <item>Mientras un elemento está girando en el inspector, su gemelo de la mesa se
    /// esconde: se presenta una copia sobre el pedestal, así que sin esto se ven dos.</item>
    /// <item>Un elemento que entra en el libro se queda en la mesa <b>marcado en verde</b>
    /// —«este ya lo llevas»— y pierde la marca en cuanto lo quita del checklist. Hasta el
    /// 2026-10-16 desaparecía; lo cambió el cliente, y de paso lo arregla: un objeto apagado
    /// tiene el collider apagado, así que no se podía volver a pulsar para deseleccionarlo.</item>
    /// </list>
    /// <para>
    /// La visibilidad se <b>deriva</b> del estado (¿lo lleva puesto? ¿lo está mirando?) en vez
    /// de encenderse y apagarse paso a paso. Así el orden en que llegan los avisos da igual, y
    /// eso importa: al aceptar, el inspector cierra la presentación y el libro registra la
    /// elección casi a la vez, y según quién llegue primero una secuencia de apagar/encender
    /// dejaría el objeto visible cuando ya debería estar puesto.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Worn Item Visibility")]
    public class WornItemVisibility : MonoBehaviour
    {
        [Tooltip("De dónde sale el libro de selecciones.")]
        [SerializeField] private ZoneRunner _runner;

        [Tooltip("Quién presenta los elementos sobre el pedestal.")]
        [SerializeField] private ItemInspector _inspector;

        [Tooltip("Opcional: solo estos elementos se marcan al llevarse puestos. Vacío = todos.")]
        [SerializeField] private Module01CatalogSO _wearable;

        [Tooltip("Material del holograma verde que marca lo ya seleccionado. M_SelectedHolo.")]
        [SerializeField] private Material _holoMaterial;

        [Tooltip("Verde de «ya seleccionado». El mismo que el de un punto resuelto.")]
        [SerializeField] private Color _holoColor = new Color(.46f, .83f, .54f, .9f);

        private readonly Dictionary<string, GameObject> _world = new Dictionary<string, GameObject>();
        private readonly HashSet<string> _worn = new HashSet<string>();
        private HashSet<string> _wearableIds;
        private SelectionLedger _ledger;
        private string _inspecting;

        private void Awake()
        {
            Rescan();
            if (_runner != null)
                Bind(_runner.Ledger);
            if (_inspector != null)
            {
                _inspector.Presenting += BeginInspecting;
                _inspector.Dismissed += EndInspecting;
            }
        }

        private void OnDestroy()
        {
            Unbind();
            if (_inspector != null)
            {
                _inspector.Presenting -= BeginInspecting;
                _inspector.Dismissed -= EndInspecting;
            }
        }

        /// <summary>
        /// Rehace el mapa de id a objeto a partir de las entidades de la escena. Público
        /// porque al cambiar de garaje los objetos son otros.
        /// </summary>
        public void Rescan()
        {
            _world.Clear();
            foreach (WorldObjectTaskEntity entity in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (string.IsNullOrEmpty(entity.ItemId))
                    continue;
                _world[entity.ItemId] = entity.gameObject;
                EnsureHolo(entity.gameObject);
            }

            _wearableIds = null;
            if (_wearable != null)
            {
                _wearableIds = new HashSet<string>();
                foreach (SafetyItemSO item in _wearable.AllItems)
                    if (item != null)
                        _wearableIds.Add(item.ItemId);
            }
        }

        /// <summary>El objeto de mundo de un elemento, o null si la escena no lo tiene.</summary>
        public GameObject WorldObject(string itemId) =>
            itemId != null && _world.TryGetValue(itemId, out GameObject go) ? go : null;

        /// <summary>
        /// El holograma de «ya seleccionado» de un elemento, o null si la escena no tiene su
        /// objeto.
        /// </summary>
        public SelectedHolo HoloOf(string itemId)
        {
            GameObject go = WorldObject(itemId);
            return go == null ? null : EnsureHolo(go);
        }

        /// <summary>
        /// El objeto trae el componente, o se le pone. Así no hay que cablear trece objetos a
        /// mano, y un garaje nuevo no llega sin marca.
        /// <para>
        /// <b>Sin <c>??</c>:</b> en el Editor, <c>GetComponent</c> de un componente ausente
        /// devuelve un objeto de diagnóstico que solo es null para el <c>==</c> sobrecargado de
        /// Unity. Para C#, y por tanto para <c>??</c>, es un objeto válido, así que
        /// <c>AddComponent</c> no se llamaría nunca.
        /// </para>
        /// </summary>
        private SelectedHolo EnsureHolo(GameObject go)
        {
            SelectedHolo holo = go.GetComponent<SelectedHolo>();
            if (holo == null)
            {
                holo = go.AddComponent<SelectedHolo>();
                holo.ConfigureForTests(_holoMaterial, _holoColor);
            }
            return holo;
        }

        /// <summary>El elemento pasa a estar sobre el pedestal: su gemelo de la mesa se esconde.</summary>
        public void BeginInspecting(SafetyItemSO item) => BeginInspecting(item == null ? null : item.ItemId);

        /// <inheritdoc cref="BeginInspecting(SafetyItemSO)"/>
        public void BeginInspecting(string itemId)
        {
            string previous = _inspecting;
            _inspecting = itemId;
            Apply(previous);
            Apply(_inspecting);
        }

        /// <summary>Se cerró la presentación. El objeto vuelve salvo que ya se lo lleve puesto.</summary>
        public void EndInspecting()
        {
            string previous = _inspecting;
            _inspecting = null;
            Apply(previous);
        }

        private void Bind(SelectionLedger ledger)
        {
            Unbind();
            _ledger = ledger;
            if (_ledger == null)
                return;
            _ledger.Added += Wear;
            _ledger.Removed += Unwear;
            _ledger.Cleared += UnwearAll;
        }

        private void Unbind()
        {
            if (_ledger == null)
                return;
            _ledger.Added -= Wear;
            _ledger.Removed -= Unwear;
            _ledger.Cleared -= UnwearAll;
            _ledger = null;
        }

        private void Wear(string itemId)
        {
            if (_wearableIds != null && !_wearableIds.Contains(itemId))
                return;
            _worn.Add(itemId);
            Apply(itemId);
        }

        private void Unwear(string itemId)
        {
            if (!_worn.Remove(itemId))
                return;
            Apply(itemId);
        }

        private void UnwearAll()
        {
            List<string> was = new List<string>(_worn);
            _worn.Clear();
            foreach (string id in was)
                Apply(id);
        }

        /// <summary>
        /// La única regla, en tres estados: apagado mientras gira en el pedestal, verde si ya
        /// lo lleva puesto, y normal en cualquier otro caso.
        /// <para>
        /// El verde se apaga también mientras está en el pedestal, aunque ya esté elegido: lo
        /// que gira ahí es una copia, y ver las dos cosas a la vez son otra vez dos cascos.
        /// </para>
        /// </summary>
        private void Apply(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return;
            if (!_world.TryGetValue(itemId, out GameObject go) || go == null)
                return;

            bool inspecting = _inspecting == itemId;
            if (go.activeSelf != !inspecting)
                go.SetActive(!inspecting);

            SelectedHolo holo = EnsureHolo(go);
            if (holo != null)
                holo.SetOn(!inspecting && _worn.Contains(itemId));
        }

        /// <summary>Solo para tests: libro y mapa de objetos sin pasar por la escena.</summary>
        public void ConfigureForTests(SelectionLedger ledger, Dictionary<string, GameObject> world)
        {
            _world.Clear();
            foreach (KeyValuePair<string, GameObject> pair in world)
            {
                _world[pair.Key] = pair.Value;
                if (pair.Value != null)
                    EnsureHolo(pair.Value);
            }
            _worn.Clear();
            _wearableIds = null;
            _inspecting = null;
            Bind(ledger);
        }
    }
}
