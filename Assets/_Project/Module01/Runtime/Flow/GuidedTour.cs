using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RideSafe.Module01
{
    /// <summary>
    /// Mueve al aprendiz entre zonas. El aprendiz nunca se desplaza por su cuenta:
    /// el módulo lo lleva, con fade para que el salto sea cómodo también en VR.
    /// <para>
    /// La reposición ocurre con la pantalla en negro, así que no hay movimiento de
    /// cámara que pueda provocar mareo. Fuera de Play el fade se omite y el salto
    /// es inmediato, para que los tests no dependan del reloj.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Module 01/Guided Tour")]
    public class GuidedTour : MonoBehaviour
    {
        [Tooltip("Raíz del jugador que se reposiciona en cada ancla.")]
        [SerializeField] private Transform _rig;

        [Tooltip("Pantalla de fade. Opcional: sin ella el salto es inmediato.")]
        [SerializeField] private CanvasGroup _fade;

        [SerializeField, Min(0f)] private float _fadeSeconds = 0.35f;

        private readonly Dictionary<string, Transform> _anchors = new Dictionary<string, Transform>();
        [SerializeField] private Transform _anchorRoot;
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            if (_anchorRoot != null)
                foreach (Transform anchor in _anchorRoot)
                    RegisterAnchor(anchor.name.Replace("anchor_", ""), anchor);
            if (_fade != null) { _fade.alpha = 0; _fade.blocksRaycasts = false; }
        }

        public event Action<string> ZoneReached;

        public string CurrentZoneId { get; private set; }

        public void RegisterAnchor(string zoneId, Transform anchor)
        {
            if (string.IsNullOrWhiteSpace(zoneId) || anchor == null)
                return;
            _anchors[Normalize(zoneId)] = anchor;
        }

        /// <summary>¿Hay ancla registrada para esa zona? Permite decidir sin provocar un aviso.</summary>
        public bool HasAnchor(string zoneId) =>
            !string.IsNullOrWhiteSpace(zoneId)
            && _anchors.TryGetValue(Normalize(zoneId), out Transform anchor)
            && anchor != null;

        public bool TryGoTo(string zoneId)
        {
            if (IsMoving) return false;
            if (string.IsNullOrWhiteSpace(zoneId))
                return false;

            string id = Normalize(zoneId);
            if (!_anchors.TryGetValue(id, out Transform anchor) || anchor == null)
            {
                Debug.LogWarning($"[Module01] No hay ancla registrada para la zona '{zoneId}'.", this);
                return false;
            }

            if (_fade != null && Application.isPlaying && isActiveAndEnabled)
                StartCoroutine(FadeAndPlace(id, anchor));
            else
                Place(id, anchor);

            return true;
        }

        /// <summary>
        /// Funde a negro y se queda así, para una transición que no es un salto de zona —por
        /// ejemplo pasar del módulo 0 al garaje—. Es la misma pantalla que usa el recorrido,
        /// para que el aprendiz no vea dos negros distintos.
        /// <para>
        /// Fuera de Play no hace nada: los tests no deben depender del reloj.
        /// </para>
        /// </summary>
        public IEnumerator FadeToBlack() => FadeScreen(1f);

        /// <summary>Devuelve la imagen desde el negro.</summary>
        public IEnumerator FadeFromBlack() => FadeScreen(0f);

        private IEnumerator FadeScreen(float target)
        {
            if (_fade == null || !Application.isPlaying || !isActiveAndEnabled)
            {
                if (_fade != null)
                {
                    _fade.alpha = target;
                    _fade.blocksRaycasts = target > .5f;
                }
                yield break;
            }

            _fade.blocksRaycasts = true;
            yield return Fade(_fade.alpha, target);
            _fade.blocksRaycasts = target > .5f;
        }

        private IEnumerator FadeAndPlace(string id, Transform anchor)
        {
            IsMoving = true;
            _fade.blocksRaycasts = true;
            yield return Fade(0f, 1f);
            Place(id, anchor);
            yield return Fade(1f, 0f);
            _fade.blocksRaycasts = false;
            IsMoving = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeSeconds <= 0f)
            {
                _fade.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < _fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _fade.alpha = Mathf.Lerp(from, to, elapsed / _fadeSeconds);
                yield return null;
            }
            _fade.alpha = to;
        }

        private void Place(string id, Transform anchor)
        {
            if (_rig != null)
                _rig.SetPositionAndRotation(anchor.position, anchor.rotation);

            CurrentZoneId = id;
            ZoneReached?.Invoke(id);
        }

        private static string Normalize(string value) => value.Trim().ToLowerInvariant();

        /// <summary>Solo para tests: inyecta el rig sin pasar por el inspector.</summary>
        public void ConfigureForTests(Transform rig) => _rig = rig;
    }
}
