namespace RideSafe.TaskSequence
{
    /// <summary>Lifecycle a single step walks through. Exposed for logging and presentation.</summary>
    public enum TaskStepPhase
    {
        None = 0,
        Prepare = 1,
        Present = 2,
        WaitForInteraction = 3,
        Validate = 4,
        Feedback = 5,
        Complete = 6,
        Cleanup = 7
    }

    public enum TaskSequenceStatus
    {
        Idle = 0,
        Running = 1,
        Completed = 2,
        Skipped = 3,
        Aborted = 4,
        Failed = 5
    }

    /// <summary>How a step decides it is done once its validator reports satisfaction.</summary>
    public enum StepCompletionMode
    {
        /// <summary>Advance as soon as the validator is satisfied.</summary>
        Immediate = 0,
        /// <summary>Hold on Feedback for the step's feedback duration, then advance.</summary>
        AfterFeedback = 1,
        /// <summary>
        /// Never self-advances on success; an external caller must call CompleteCurrentStep.
        /// A failure (timeout, broken validator) still goes through the step's failure policy.
        /// </summary>
        Manual = 2
    }

    /// <summary>What a step does when it fails (timeout, broken or throwing validator).</summary>
    public enum StepFailurePolicy
    {
        /// <summary>
        /// Re-arm the same step. Default, because a tutorial teaches by letting the learner
        /// try again. Only applies to recoverable failures (timeout); a broken validator
        /// can never succeed, so it falls back to <see cref="Skip"/>.
        /// </summary>
        Retry = 0,
        /// <summary>Move on to the next step. The sequence will finish as Failed.</summary>
        Skip = 1,
        /// <summary>Stop the whole sequence as Failed.</summary>
        FailSequence = 2
    }

    /// <summary>What to do when a step's EntityId cannot be resolved. Never throws.</summary>
    public enum MissingEntityPolicy
    {
        /// <summary>Log an error, skip the step, keep the sequence alive. Default (CASE 02).</summary>
        SkipStep = 0,
        /// <summary>Log an error and run the step without an entity (input-only validators).</summary>
        ContinueWithoutEntity = 1,
        /// <summary>Log an error and fail the whole sequence.</summary>
        FailSequence = 2
    }

    /// <summary>
    /// On a step: what SkipCurrentStep does. On a sequence: whether SkipSequence is allowed
    /// (anything other than NotSkippable allows it).
    /// </summary>
    public enum SkipPolicy
    {
        NotSkippable = 0,
        SkipStep = 1,
        SkipWholeSequence = 2
    }

    /// <summary>What happens when a sequence is started again in the same session (same runner).</summary>
    public enum RestartPolicy
    {
        /// <summary>Every run starts from the first step.</summary>
        FromBeginning = 0,
        /// <summary>A run that was aborted resumes at the step it was aborted on.</summary>
        Resume = 1,
        /// <summary>A sequence that already completed refuses to run again.</summary>
        RunOnce = 2
    }
}
