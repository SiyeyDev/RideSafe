using System;
using RideSafe.TaskSequence;
using UnityEngine;

namespace RideSafe.Tutorial
{
    /// <summary>
    /// Shared plumbing for tutorial validators.
    /// <para>
    /// Validators know Action, Entity and Context. They never know modules, scenes or
    /// hardware: there is no HelmetSelectValidator and no Module01TriggerValidator, and
    /// no validator touches OVRInput or an InputActionReference. Adding
    /// SteeringValidator or BrakeValidator later means adding a class here, with zero
    /// changes to the runner.
    /// </para>
    /// <para>
    /// CONTRACT: <see cref="OnPrepare"/> must reset all state. The same instance is reused
    /// on every run of its sequence.
    /// </para>
    /// </summary>
    [Serializable]
    public abstract class TutorialValidatorBase : ITaskValidator
    {
        protected TaskStepContext Context { get; private set; }

        /// <summary>Set when the validator cannot possibly succeed, so it fails fast.</summary>
        protected bool IsBroken { get; private set; }

        public void Prepare(TaskStepContext context)
        {
            Context = context;
            IsBroken = false;
            OnPrepare();
        }

        public bool Evaluate(float deltaTime)
        {
            if (IsBroken)
                return false;
            return OnEvaluate(deltaTime);
        }

        public void Cleanup()
        {
            OnCleanup();
            Context = null;
            IsBroken = false;
        }

        public abstract string Describe();

        protected abstract void OnPrepare();
        protected abstract bool OnEvaluate(float deltaTime);
        protected virtual void OnCleanup() { }

        /// <summary>Marks the validator unusable and logs why, with sequence and step ids.</summary>
        protected void Break(string reason)
        {
            IsBroken = true;
            if (Context != null)
                Context.LogError(GetType().Name + " cannot run: " + reason);
            else
                Debug.LogError("[Tutorial] " + GetType().Name + " cannot run: " + reason);
        }

        /// <summary>Resolves the input service, breaking the validator when absent.</summary>
        protected ITutorialInputService ResolveInput()
        {
            if (Context == null)
                return null;

            ITutorialInputService input = Context.GetService<ITutorialInputService>();
            if (input == null)
                Break("no ITutorialInputService is registered. Add a RideSafeInputService to the scene.");
            return input;
        }

        /// <summary>Resolves the pose provider, breaking the validator when absent.</summary>
        protected IXRPoseProvider ResolvePose()
        {
            if (Context == null)
                return null;

            IXRPoseProvider pose = Context.GetService<IXRPoseProvider>();
            if (pose == null)
                Break("no IXRPoseProvider is registered.");
            return pose;
        }

        /// <summary>
        /// Resolves the step entity as <typeparamref name="T"/>. A step that names an
        /// entity which does not support the needed capability is an authoring bug, so it
        /// breaks loudly instead of waiting forever.
        /// </summary>
        protected T ResolveEntity<T>() where T : class, ITaskEntity
        {
            if (Context == null)
                return null;

            if (Context.Entity == null)
            {
                Break("the step has no resolved entity but this validator requires one.");
                return null;
            }

            T typed = Context.Entity as T;
            if (typed == null)
            {
                Break("entity '" + Context.Entity.Id + "' does not implement " + typeof(T).Name +
                      " (it is " + Context.Entity.GetType().Name + ").");
            }
            return typed;
        }

        /// <summary>Verifies an action is bound before waiting on it (CASE 04).</summary>
        protected bool RequireMappedAction(ITutorialInputService input, ActionId action)
        {
            if (input == null)
                return false;

            if (!action.IsValid)
            {
                Break("ActionId is empty.");
                return false;
            }
            if (!input.IsMapped(action))
            {
                Break("ActionId '" + action + "' is not mapped by input profile '" + input.ProfileId + "'.");
                return false;
            }
            return true;
        }
    }
}
