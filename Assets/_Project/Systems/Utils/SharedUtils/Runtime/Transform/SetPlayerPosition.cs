using Autohand;
using Sirenix.OdinInspector;
using UnityEngine;

public class SetPlayerPosition : MonoBehaviour
{
    [SerializeField] private AutoHandPlayer _objectToMove;
    [Tooltip("If active, the player can only move horizontally.")]
    [SerializeField] private bool _considerHeight;
    [SerializeField] private bool _macthRotation;
#if UNITY_EDITOR
    [ShowIf("_macthRotation")][SerializeField] private bool _showDirection;
    [ShowIf("_showDirection")][SerializeField] private Color _guideColor;
    [ShowIf("_showDirection")][SerializeField] private float _guideLength = 1.2f;
#endif
    public void Move()
    {
        Vector3 targetPosition = transform.position;
        if (_considerHeight)
            targetPosition = targetPosition.With(y: _objectToMove.transform.position.y);
        _objectToMove.SetPosition(targetPosition);
        if (_macthRotation)
        {
            Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            _objectToMove.SetRotation(targetRotation);
        }
    }

    internal void MoveConfig(Transform _transform = null)
    {
        Vector3 targetPosition;
        if (_transform != null)
        {
            targetPosition = _transform.position;

        }
        else
        {
            targetPosition = transform.position;
        }

        if (_considerHeight)
            targetPosition = targetPosition.With(y: _objectToMove.transform.position.y);
        _objectToMove.SetPosition(targetPosition);
        if (_macthRotation)
        {
            Quaternion targetRotation;
            if (_transform != null)
            {
                targetRotation = Quaternion.Euler(0, _transform.eulerAngles.y, 0);
            }
            else
            {
                targetRotation = Quaternion.Euler(0, targetPosition.y, 0);
            }
            _objectToMove.SetRotation(targetRotation);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showDirection)
            return;
        Gizmos.color = _guideColor;
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Vector3 forwardDirection = targetRotation * Vector3.forward;
        Gizmos.DrawRay(transform.position, forwardDirection * _guideLength);
    }
#endif
}
