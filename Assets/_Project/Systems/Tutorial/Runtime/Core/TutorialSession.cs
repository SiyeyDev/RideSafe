using System;
using System.Collections.Generic;
using Cachacos;
using RideSafe.TaskSequence;
using UnityEngine;
using UnityEngine.Events;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Scene entry point for the guided part of a session: owns the session scope
    /// (assistance guard, learned actions, chosen language/location/vehicle) and runs its
    /// stages one after another through <see cref="ITaskSequenceService"/>.
    /// <para>
    /// A stage is a sequence plus the assistance mode it runs under and what the scene does
    /// when it completes (show the next panel, hide a prop). That is also the entry pattern
    /// for Module 1: an intro stage in Tutorial mode, then the real stage in Assessment mode,
    /// where the guard blocks every hint.
    /// </para>
    /// <para>
    /// Knows nothing about presentation or XR: presentation listens to the service's
    /// runner, and everything the steps need is resolved by id and service.
    /// </para>
    /// </summary>
    [AddComponentMenu("RideSafe/Tutorial Session")]
    public class TutorialSession : MonoBehaviour
    {
        [Serializable]
        public class Stage
        {
            [Tooltip("Steps reference scene objects only by EntityId.")]
            public TaskSequenceSO Sequence;

            [Tooltip("Assessment/Transfer block all tutorial help while this stage runs.")]
            public TutorialMode Mode = TutorialMode.Tutorial;

            [Tooltip("Scene reaction once the stage completes, e.g. hide the gate and show Language.")]
            public UnityEvent OnCompleted = new UnityEvent();
        }

        [SerializeField] private List<Stage> _stages = new List<Stage>();

        [Tooltip("Start the first stage automatically when the scene starts.")]
        [SerializeField] private bool _runOnStart = true;

        private TutorialSessionScope _scope;
        private TutorialAssistanceGuard _guard;
        private TaskSequenceHandle _handle;
        private int _stageIndex = -1;

        public TaskSequenceHandle CurrentRun => _handle;
        public TutorialProgress Progress { get; private set; }
        public TutorialContext Context { get; private set; }

        protected virtual void Awake()
        {
            _scope = new TutorialSessionScope(gameObject.scene, "tutorial-" + name);

            _guard = new TutorialAssistanceGuard();
            Progress = new TutorialProgress();
            Context = new TutorialContext();

            _scope.AddService(_guard).AddService(Progress).AddService(Context);
            _scope.AddCleanup(_guard.Reset).AddCleanup(Progress.Clear).AddCleanup(Context.Clear);
        }

        protected virtual void Start()
        {
            if (_runOnStart)
                RunStage(0);
        }

        protected virtual void OnDestroy()
        {
            Abort();
            if (_scope != null)
                _scope.Dispose();
        }

        #region Flow

        /// <summary>Stops the current stage. The runner cleans up validators; presentation clears on finish.</summary>
        [ContextMenu("Abort")]
        public void Abort()
        {
            TaskSequenceHandle handle = _handle;
            _handle = null;
            if (handle != null && handle.IsRunning)
                handle.Abort("tutorial session aborted");
        }

        /// <summary>Starts over from the first stage. Learned actions are kept for the session.</summary>
        [ContextMenu("Restart")]
        public void Restart()
        {
            Abort();
            RunStage(0);
        }

        private void RunStage(int index)
        {
            if (index < 0 || index >= _stages.Count)
                return;

            ITaskSequenceService service = ServiceLocator.Instance.RequestService<ITaskSequenceService>();
            if (service == null)
            {
                Debug.LogError("[Tutorial] No ITaskSequenceService in the scene; '" + name + "' cannot run.", this);
                return;
            }

            Stage stage = _stages[index];
            _stageIndex = index;
            _guard.SetMode(stage.Mode);

            TaskSequenceHandle handle = service.Run(stage.Sequence);
            _handle = handle;
            if (handle != null)
                handle.OnFinished(status => HandleStageFinished(handle, status));
        }

        private void HandleStageFinished(TaskSequenceHandle handle, TaskSequenceStatus status)
        {
            // An aborted or replaced run is not this stage's outcome.
            if (!ReferenceEquals(handle, _handle) || status != TaskSequenceStatus.Completed)
                return;

            Stage stage = _stages[_stageIndex];
            Progress.MarkCompleted(handle.SequenceId);
            stage.OnCompleted.Invoke();
            RunStage(_stageIndex + 1);
        }

        #endregion

        #region Choices (wired from the real Language / Location / Vehicle panels)

        /// <summary>Switches the whole session's language at once (I2 remembers it).</summary>
        public void SetLanguage(string languageCode)
        {
            Context.Language = languageCode;
            ILocalizationProvider localization = ServiceLocator.Instance.RequestService<ILocalizationProvider>();
            if (localization == null || !localization.SetLanguage(languageCode))
                Debug.LogWarning("[Tutorial] Language '" + languageCode + "' could not be applied to localization.", this);
        }

        public void SetJurisdiction(string jurisdictionId) => Context.Jurisdiction = jurisdictionId;

        public void SetVehicle(string vehicleId) => Context.Vehicle = vehicleId;

        #endregion
    }
}
