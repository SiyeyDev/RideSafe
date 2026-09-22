using Cachacos;
using UnityEngine;

namespace RideSafe.TaskSequence
{
    /// <summary>
    /// Sweeps its subtree for <see cref="TaskEntityBase"/> components INCLUDING inactive
    /// GameObjects and registers them.
    /// <para>
    /// This exists because <c>Awake</c> never runs on an object that starts disabled, so
    /// relying on the component alone would make it impossible to address hidden UI that
    /// a step is supposed to reveal. Registration is idempotent, so entities that already
    /// registered themselves are untouched.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TaskEntitySceneRegistrar : MonoBehaviour
    {
        [Tooltip("Root to sweep. Defaults to this GameObject when empty.")]
        [SerializeField] private Transform _root;

        [Tooltip("Sweep again in OnEnable, for subtrees built at runtime.")]
        [SerializeField] private bool _rescanOnEnable = true;

        private void Awake() => Scan();

        private void OnEnable()
        {
            if (_rescanOnEnable)
                Scan();
        }

        /// <summary>Registers every entity under the root, inactive ones included.</summary>
        [ContextMenu("Scan now")]
        public void Scan()
        {
            if (ServiceLocator.Instance.RequestService<TaskContextService>() == null)
            {
                TaskLog.Warn(null, null,
                    $"'{name}' found no TaskContextService. Entities will register themselves once a " +
                    "tutorial session starts; call Scan() again afterwards if ids are missing.", this);
                return;
            }

            Transform root = _root != null ? _root : transform;
            TaskEntityBase[] entities = root.GetComponentsInChildren<TaskEntityBase>(includeInactive: true);

            int registered = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == null)
                    continue;
                entities[i].EnsureRegistered();
                registered++;
            }

            TaskLog.Info(null, null, $"'{name}' swept {registered} entities (inactive included).");
        }
    }
}
