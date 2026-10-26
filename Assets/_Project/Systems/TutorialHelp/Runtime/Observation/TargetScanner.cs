using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RideSafe.Help
{
    /// <summary>
    /// What the learner could select right now, without knowing any module: interactable
    /// controls on world-space canvases, and objects with a collider that receive clicks, all
    /// within view and ray reach. Objects are listed once a second; controls on every scan.
    /// </summary>
    public sealed class TargetScanner
    {
        public const float MaxAngleFromGaze = 45f; // about half the PC view (91° wide) and inside Quest 3's
        public const float MaxDistance = 8f; // AutoHand's HandCanvasPointer.raycastLength
        /// <summary>
        /// Objects count only within reach: the ray goes 8 m, but a module presents what it wants
        /// selected near the learner, while far objects (e.g. module 1's EPP behind the garage door,
        /// which has no collider) are still in ray reach long before they matter.
        /// <para>
        /// 5.5 and not 4: the client's EPP tables (2026-10-23) put objects up to 4.88 m from their
        /// station's viewpoint, and those past 4 m never got a halo (PO, 2026-10-26). From outside the
        /// garage the nearest EPP is still over 7 m away (Module01 TutorialHaloReachTests).
        /// </para>
        /// </summary>
        public const float MaxObjectDistance = 5.5f;
        public const float ObjectRefreshSeconds = 1f;

        private readonly HashSet<Transform> _seen = new HashSet<Transform>();
        private readonly Collider[] _nearby = new Collider[256];
        private Selectable[] _selectables = new Selectable[64];

        /// <summary>
        /// Controls come from uGUI's own list of selectables (copied into a reused buffer); objects
        /// from the colliders physics finds within reach. Nothing searches the whole scene and
        /// nothing allocates per scan, which on Quest meant a spike and garbage every second.
        /// </summary>
        public void Scan(Pose viewer, float elapsed, Transform ignore, List<HelpTarget> results)
        {
            results.Clear();
            _seen.Clear();

            if (_selectables.Length < Selectable.allSelectableCount)
                _selectables = new Selectable[Mathf.NextPowerOfTwo(Selectable.allSelectableCount)];
            int selectables = Selectable.AllSelectablesNoAlloc(_selectables);
            for (int i = 0; i < selectables; i++)
            {
                Selectable selectable = _selectables[i];
                if (!IsUsable(selectable) || IsUnder(selectable.transform, ignore) || !InView(viewer, selectable.transform.position))
                    continue;
                if (_seen.Add(selectable.transform))
                    results.Add(new HelpTarget(selectable.transform, InteractionKind.Interface, selectable.targetGraphic));
            }

            int hits = Physics.OverlapSphereNonAlloc(viewer.position, MaxObjectDistance + 0.5f, _nearby,
                                                     Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits; i++)
            {
                Collider collider = _nearby[i];
                if (collider == null || !collider.enabled)
                    continue;
                // Whoever receives this collider's click: the same lookup uGUI does for the pointer.
                GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(collider.gameObject);
                if (handler == null)
                    continue;
                Transform target = handler.transform;
                if (_seen.Contains(target) || IsUnder(target, ignore) || handler.GetComponent<Selectable>() != null ||
                    handler.GetComponentInParent<Canvas>() != null)
                    continue;
                // The collider's transform, not its bounds: bounds lag until physics syncs transforms.
                if (!InView(viewer, collider.transform.position) ||
                    Vector3.Distance(viewer.position, collider.transform.position) > MaxObjectDistance ||
                    !Reachable(viewer, collider, target))
                    continue;
                _seen.Add(target);
                results.Add(new HelpTarget(target, InteractionKind.Object3D, null, collider));
            }
        }

        public static bool InView(Pose viewer, Vector3 point)
        {
            Vector3 toPoint = point - viewer.position;
            float distance = toPoint.magnitude;
            if (distance < 0.05f || distance > MaxDistance)
                return false;
            return Vector3.Angle(viewer.rotation * Vector3.forward, toPoint) <= MaxAngleFromGaze;
        }

        /// <summary>
        /// The pointer's own rule: a ray from the eyes reaches the object before anything else.
        /// Objects behind a closed door or a wall are in view but cannot be selected yet.
        /// </summary>
        public static bool Reachable(Pose viewer, Collider collider, Transform target)
        {
            Vector3 point = collider.bounds.size.sqrMagnitude > 0f && collider.bounds.Contains(collider.transform.position)
                ? collider.bounds.center
                : collider.transform.position;
            Vector3 toPoint = point - viewer.position;
            if (!Physics.Raycast(viewer.position, toPoint.normalized, out RaycastHit hit, toPoint.magnitude + 0.5f,
                                 Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true; // nothing in the way (the target itself may be a trigger)
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        public static bool IsUsable(Selectable selectable)
        {
            if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsInteractable())
                return false;
            Canvas canvas = selectable.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled || canvas.rootCanvas.renderMode != RenderMode.WorldSpace)
                return false;
            return GroupAlpha(selectable.transform) > 0.01f;
        }

        private static float GroupAlpha(Transform transform)
        {
            float alpha = 1f;
            for (Transform t = transform; t != null; t = t.parent)
            {
                CanvasGroup group = t.GetComponent<CanvasGroup>();
                if (group == null || !group.enabled)
                    continue;
                alpha *= group.alpha;
                if (group.ignoreParentGroups)
                    break;
            }
            return alpha;
        }

        private static bool IsUnder(Transform transform, Transform root) => root != null && transform.IsChildOf(root);


    }
}
