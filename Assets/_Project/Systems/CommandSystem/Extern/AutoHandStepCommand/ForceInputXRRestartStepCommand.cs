using StepCommand;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[Serializable]
public class ForceInputXRRestartStepCommand : StepCommandClass
{
    #region ICommand Methods

    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        RestarXR();
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }

    #endregion
    #region Main Methods
    private void RestarXR()
    {
        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (XRInputSubsystem subsystem in subsystems)
        {
            CoroutineCaller.Instance.WaitForNextFrame(() =>
            {
                if (!subsystem.running)
                    return;
                subsystem.Stop();
                subsystem.Start();
            });
        }
    }
    #endregion
}
