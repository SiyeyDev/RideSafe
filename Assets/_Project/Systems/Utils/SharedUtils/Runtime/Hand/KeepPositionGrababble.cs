using Autohand;
using Sirenix.OdinInspector;
//using UnityEditor.VersionControl;
using UnityEngine;

[RequireComponent(typeof(Grabbable))]
public class KeepPositionGrababble : MonoBehaviour
{
    [SerializeField] private Grabbable _grabbable;
    [SerializeField] private bool _useRestTransform;
    [ShowIf("_useRestTransform")][SerializeField] private Transform _targetResttransform;
    [SerializeField] private bool _restartPosition;
    [ShowIf("_restartPosition")][SerializeField] private float _maxDistance = 0.1f;
    [SerializeField] private bool _restartRotation;
    [ShowIf("_restartRotation")][SerializeField] private float _maxAngle = 5;
    [ShowIf("@_restartPosition || _restartRotation")]
    [SerializeField] private float _timerAfterRestart;
    [ReadOnly][ShowInInspector] private Vector3 _startPosition;
    private Quaternion _startRotation;
    private int _handsAmountGrabbing;
    private bool _grabbed;
    private float _timer;

    private void Awake()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        if (_grabbable == null)
            _grabbable = gameObject.GetComponent<Grabbable>();
        if (_grabbable == null)
            throw new System.Exception($"There is not {typeof(Grabbable)} attached to {gameObject.name}");
    }
    private void OnEnable()
    {
        _grabbable.OnGrabEvent += OnGrab;
        _grabbable.OnReleaseEvent += OnRelease;
    }
    private void OnDisable()
    {
        _grabbable.OnGrabEvent -= OnGrab;
        _grabbable.OnReleaseEvent -= OnRelease;
    }
    private void Update()
    {
        if (_grabbed || !ShouldReturn())
        {
            _timer = 0;
            return;
        }
        _timer += Time.deltaTime;
        if (_timer < _timerAfterRestart)
            return;
        _timer = 0;
        _grabbable.body.linearVelocity = Vector3.zero;
        _grabbable.body.angularVelocity = Vector3.zero;
        if (_restartPosition)
            transform.position = GetRestPosition();
        if (_restartRotation)
            transform.rotation = _useRestTransform ? _targetResttransform.rotation : _startRotation;
    }
    private bool ShouldReturn()
    {
        if (_restartPosition && Vector3.Distance(transform.position, GetRestPosition()) > _maxDistance)
            return true;
        if (_restartRotation && Quaternion.Angle(transform.rotation, GetRestRotation()) > _maxAngle)
            return true;
        return false;
    }
    private void OnGrab(Hand hand, Grabbable grabbable)
    {
        _handsAmountGrabbing++;
        if (_handsAmountGrabbing > 1)
            return;
        _grabbed = true;

    }
    private void OnRelease(Hand hand, Grabbable grabbable)
    {
        if (!enabled)
            return;
        _handsAmountGrabbing--;
        if (_handsAmountGrabbing > 0)
            return;
        _grabbed = false;

    }
    public void SetNewRestValues(Vector3 restPosition, Quaternion restRotation)
    {
        _startPosition = restPosition;
        _startRotation = restRotation;
        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }
    public Vector3 GetRestPosition() => _useRestTransform ? _targetResttransform.position : _startPosition;
    public Quaternion GetRestRotation() => _useRestTransform ? _targetResttransform.rotation : _startRotation;

    public void ResetParameters()
    {
        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_grabbable == null)
            _grabbable = gameObject.GetComponent<Grabbable>();
    }
#endif

}
