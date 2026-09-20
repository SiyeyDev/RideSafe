using Sirenix.OdinInspector;
using System;
using UnityEngine.SpatialTracking;
using UnityEngine;

public abstract class BaseKinematic : MonoBehaviour
{
    [BoxGroup("BaseKinematic Settings")]
    [SerializeField] private bool _moveCamera = true;
    [BoxGroup("BaseKinematic Settings")]
    [ShowIf("_moveCamera")][SerializeField] internal Transform _targetTransform;
    [ShowIf("_moveCamera")][SerializeField] internal CameraPcController _cameraPCController;
    [BoxGroup("BaseKinematic Settings")]
    [SerializeField] protected float duration;
    [BoxGroup("BaseKinematic Settings/ AutoHand")]
    [SerializeField] protected bool _supportAutoHand = true;

    [BoxGroup("BaseKinematic Settings/ AutoHand")]
    [ShowIf(nameof(_supportAutoHand))]
    [SerializeField] private SetHands _setHands;
    [BoxGroup("BaseKinematic Settings/ AutoHand")]
    [ShowIf(nameof(_supportAutoHand))]
    [SerializeField] internal bool _disableCameraDriver = true;
    [BoxGroup("BaseKinematic Settings/ AutoHand")]
    [ShowIf(nameof(_supportAutoHand))]
    [SerializeField] private TrackedPoseDriver _cameraDriver;

    [BoxGroup("BaseKinematic Settings")]
    [SerializeField] private bool _useKinematicUI;
    [BoxGroup("BaseKinematic Settings")]
    [ShowIf("_useKinematicUI")][SerializeField] private GameObject _KinematicUI;
    [BoxGroup("Fade Settings")]
    [SerializeField] protected bool doFade;
    [BoxGroup("Fade Settings")]
    [ShowIf("doFade")][SerializeField] protected bool doStartFade = true;
    [BoxGroup("Fade Settings")]
    [ShowIf("doFade")][SerializeField] protected bool doEndFade = true;
    [BoxGroup("Fade Settings")]
    [ShowIf("doFade")][SerializeField] protected FadeSettings fadeSettings;
    [BoxGroup("Guide Settings")]
    [SerializeField] private bool _showGuide;
    [BoxGroup("Guide Settings")]
    [ShowIf("_showGuide")][SerializeField] private Color _guideColor;
    [BoxGroup("Guide Settings")]
    [ShowIf("_showGuide")][SerializeField] private float _guideSize;
    public Action OnPlay;
    public Action OnStop;
    private Transform _mainCameraTransform;
    private TransformParentSettings _defaultSettings;
    private TransformParentSettings _parentSettings;
    private void Awake()
    {
        if (_supportAutoHand && _setHands == null)
            throw new Exception($"There is not {typeof(SetHands)} attached to {gameObject.name}");
    }
    protected internal virtual void Start()
    {
        _mainCameraTransform = Camera.main.transform;
        if (_useKinematicUI)
            _KinematicUI.SetActive(false);
        _defaultSettings = new TransformParentSettings(_targetTransform, true);
        _parentSettings = new TransformParentSettings(_mainCameraTransform);
        if (!doFade)
        {
            doStartFade = false;
            doEndFade = false;
        }
        enabled = false;
    }
    #region BaseKinematic Methods
    protected virtual void InternalPlay()
    {
        SetCamera();
        OnPlay?.Invoke();
        this.CoroutineExecuteActionAfter(Stop, duration);
    }
    protected virtual void InternalStop()
    {
        ResetCamera();
        OnStop?.Invoke();
    }
    #endregion
    #region Main Methods
    [ContextMenu("Play Base Kinematic")]
    public void Play()
    {
        enabled = true;
        if (doStartFade)
        {
            this.CoroutineExecuteActionAfter(InternalPlay, fadeSettings.duration * 0.5f);
            FadeUI.Fade(fadeSettings);
            return;
        }
        InternalPlay();
    }
    public void Stop()
    {
        enabled = false;
        if (doEndFade)
        {
            this.CoroutineExecuteActionAfter(InternalStop, fadeSettings.duration * 0.5f);
            FadeUI.Fade(fadeSettings);
            return;
        }
        InternalStop();
    }
    protected virtual void SetCamera()
    {
        if (_supportAutoHand)
        {
            if (_disableCameraDriver)
                if (_supportAutoHand)

                    _cameraDriver.enabled = false;
            _cameraDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        }
        if (_moveCamera)
        {
            if (_supportAutoHand)
                _setHands.Set(false);
            _cameraPCController?.SetActive(false);
            _defaultSettings.SetParent(_mainCameraTransform);
        }
        if (_useKinematicUI)
            _KinematicUI.SetActive(true);
    }
    protected virtual void ResetCamera()
    {
        if (_moveCamera)
        {
            if (_supportAutoHand)

                _setHands.Set(true);
            _cameraPCController?.SetActive(true);
            _parentSettings.SetParent(_mainCameraTransform);

        }
        if (_useKinematicUI)
            _KinematicUI.SetActive(false);
        if (_supportAutoHand)
        {
            if (_disableCameraDriver)
                _cameraDriver.enabled = true;
            _cameraDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        }
    }
    #endregion

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (!_showGuide)
            return;
        Gizmos.color = _guideColor;
        if (_targetTransform == null)
            return;
        Gizmos.DrawRay(_targetTransform.position, _targetTransform.forward * _guideSize);
        Gizmos.DrawSphere(_targetTransform.position, _guideSize * 0.75f);
    }
#endif

}
