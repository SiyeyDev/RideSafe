using Cachacos;
using UnityEngine;

public class PlayingAnimSpeedI2Translate : MonoBehaviour 
{
    [Header("Anim Settings")]
    [SerializeField] private Animator _animator;
    [SelectAnimationState][SerializeField] private int _clipHash;
    [SerializeField] private LocalizedEntries<float> _speeds;
    private float _lastSpeed;
    private bool _isPlaying;

    private void Update()
    {
        if (_animator.IsPlayingState(_clipHash))
        {
            if (!_isPlaying)
                SetSpeed();
            return;
        }
        if (_isPlaying)
            ResetSpeed();
    }
    private void SetSpeed()
    {
        _isPlaying = true;
        _lastSpeed = _animator.speed;
        _animator.speed = _speeds.GetValue();
    }
    private void ResetSpeed()
    {
        _isPlaying = false;
        _animator.speed = _lastSpeed;
    }
}
