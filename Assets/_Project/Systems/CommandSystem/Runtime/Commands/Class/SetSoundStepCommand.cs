using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    public class SetSoundStepCommand : StepCommandClass
    {
        [Header("Sound Settings")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Active for play sound, enactive for stop sound")]
        [SerializeField] private bool _state;
        #region ICommand Methods
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            if (_state)
                _audioSource.Play();
            else
                _audioSource.Stop();
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
        }
        #endregion
    }
}