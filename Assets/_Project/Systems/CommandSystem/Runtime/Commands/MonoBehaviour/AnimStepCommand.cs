using Cachacos;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace StepCommand
{
    [Serializable]
    [AddComponentMenu("StepCommand/AnimStepCommand")]
    public class AnimStepCommand : ParralleBaseMonoBehaviourStepCommand
    {
        [Header("Anim Settings")]
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _waitForEndAnim;
        [SerializeField] private bool _hideAnimatorOnStart;
        [SerializeField] private bool _hideAnimatorOnEnd;
        [SelectAnimationState][SerializeField] private int _clipHash;
        [SerializeField] private bool _customSpeed;
        [SerializeField] private bool _customCrossFade;
        [ShowIf("_customCrossFade")][SerializeField] private float _crossFade;

        private bool _playingAnimation;
        #region ICommand Methods
        public override IStepCommand Initialize()
        {
            if (_hideAnimatorOnStart)
                _animator.gameObject.SetActive(false);
            return base.Initialize();
        }
        public override void Execute(Action<bool, IStepCommand> onComplete)
        {
            base.Execute(onComplete);
            if (!_animator.gameObject.activeInHierarchy)
                _animator.gameObject.SetActive(true);
            _playingAnimation = true;
            if (_waitForEndAnim)
                _animator?.ExcuteAfterAnim(this, () => Complete(true, this), _clipHash, _customCrossFade ? _crossFade : Constants.k_crossfadeDuration, _customSpeed);
            else
            {
                _animator.CrossFade(_clipHash, _customCrossFade ? _crossFade : Constants.k_crossfadeDuration);
                Complete(true, this);
            }

        }
        public override void Exit()
        {
            if (_playingAnimation)
            {
                _animator.StopPlayback();
                if (_hideAnimatorOnEnd)
                    _animator.gameObject.SetActive(false);
            }
            base.Exit();
        }
        public override void InternalUndo()
        {
            if (_hideAnimatorOnEnd && !_hideAnimatorOnStart)
                _animator.gameObject.SetActive(true);
            base.InternalUndo();
        }
        protected override void Complete(bool right, IStepCommand stepCommand, bool callDelegate = true)
        {
            Exit();
            _playingAnimation = false;
            base.Complete(right, stepCommand, callDelegate);
        }
        #endregion
        #region ParralleBaseMonoBehaviour Methods
        public override bool IsExecuted()
        {
            if (!_waitForEndAnim)
                return true;
            return !_playingAnimation;
        }
        #endregion
    }
}