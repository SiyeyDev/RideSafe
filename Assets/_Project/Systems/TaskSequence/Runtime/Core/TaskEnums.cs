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
        /// <summary>Never self-advances; an external caller must call CompleteCurrentStep.</summary>
        Manual = 2
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

    public enum SkipPolicy
    {
        NotSkippable = 0,
        SkipStep = 1,
        SkipWholeSequence = 2
    }

    public enum RestartPolicy
    {
        /// <summary>Re-running a completed sequence starts from the first step.</summary>
        FromBeginning = 0,
        /// <summary>Re-running resumes at the last incomplete step.</summary>
        Resume = 1,
        /// <summary>A completed sequence refuses to run again in the same session.</summary>
        RunOnce = 2
    }
}
