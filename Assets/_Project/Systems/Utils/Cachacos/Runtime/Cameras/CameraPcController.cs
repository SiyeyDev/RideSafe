using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPcController : MonoBehaviour
{
    [Header("Camera settings")]
    [SerializeField] Transform _followTransform;
    [SerializeField, Range(0.01f, 5f)] float _sensivity;
    [SerializeField, Range(-89f, 0f)] float _maxClamp = -89f;
    [SerializeField, Range(0f, 89f)] float _minClamp = 89f;
    [SerializeField, Range(0f, 0.1f)] float smoothTime = 0;
    private float _targetYaw;
    private float _targetPitch;
    private float _yawVelocity;
    private float _pitchVelocity;
    [SerializeField, ReadOnly] private bool _active;

    private bool lastCursorLockState;
    private void Awake()
    {
        _followTransform.rotation = Quaternion.identity;
        CursorManager.UnlockCursor = false;
        _active = true;
        if (_followTransform == null)
        {
            _followTransform = this.transform;
        }
        Vector3 euler = _followTransform.localEulerAngles;
        _targetYaw = euler.y;
        _targetPitch = euler.x;
    }

    private void Update()
    {
        if (!_active || CursorManager.UnlockCursor)
            return;
        ReadMouseValues();
    }
    private void LateUpdate()
    {
        CameraMove();

    }

    private void CameraMove()
    {
        float yaw = Mathf.SmoothDampAngle(_followTransform.localEulerAngles.y, _targetYaw, ref _yawVelocity, smoothTime);
        float xaw = Mathf.SmoothDampAngle(_followTransform.localEulerAngles.x, _targetPitch, ref _pitchVelocity, smoothTime);
        _followTransform.localEulerAngles = new Vector3(xaw, yaw, 0f);
    }
    private void ReadMouseValues()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        _targetYaw += mouseX * _sensivity;
        _targetPitch -= mouseY * _sensivity;
        _targetPitch = Mathf.Clamp(_targetPitch, _maxClamp, _minClamp);
    }

    public void SetActive(bool state) => _active = state;

    public void SetRotation(Transform targetRotation)
    {
        _followTransform.rotation = targetRotation.rotation;
        Vector3 euler = _followTransform.localEulerAngles;
        _targetYaw = euler.y;
        _targetPitch = euler.x;
        _yawVelocity = 0;
        _pitchVelocity = 0;
    }
}
