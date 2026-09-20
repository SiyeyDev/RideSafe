using Autohand;
using UnityEngine;

public class KeepPositionDistanceObject : MonoBehaviour
{
    [SerializeField] private DistanceObject _distanceObject;
    [SerializeField] private float _maxDistance = 0.1f;
    [SerializeField] private float _timerAfterRestart = 2;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _selected;
    private float _timer;
    private Rigidbody _rb;

    private void Awake()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        if (_distanceObject == null)
            _distanceObject = gameObject.GetComponent<DistanceObject>();
        _rb = _distanceObject.GetComponent<Rigidbody>();
        if (_distanceObject == null)
            throw new System.Exception($"There is not {typeof(Grabbable)} attached to {gameObject.name}");
    }
    private void OnEnable()
    {
        _distanceObject.onSelect += OnSelected;
        _distanceObject.onDeselect += OnDeselected;
        if (ShouldReturn())
            Return();

    }

    private void Return()
    {
        _distanceObject.transform.position = _startPosition;
        _distanceObject.transform.rotation = _startRotation;
    }

    private void OnDisable()
    {
        _distanceObject.onSelect -= OnSelected;
        _distanceObject.onDeselect -= OnDeselected;
    }
    private void Update()
    {
        if (!_distanceObject.enabled)
            return;
        if (_selected || !ShouldReturn())
        {
            _timer = 0;
            return;
        }
        _timer += Time.deltaTime;
        if (_timer < _timerAfterRestart)
            return;
        _timer = 0;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        Return();
    }
    private bool ShouldReturn() => Vector3.Distance(_distanceObject.transform.position, _startPosition) > _maxDistance;
    private void OnSelected() => _selected = true;
    private void OnDeselected()
    {
        if (!enabled)
            return;
        _selected = false;

    }
    public void SetNewRestValues(Vector3 restPosition, Quaternion restRotation)
    {
        _startPosition = restPosition;
        _startRotation = restRotation;
        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }
    public Vector3 GetRestPosition() => _startPosition;
    public Quaternion GetRestRotation() => _startRotation;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_distanceObject == null)
            _distanceObject = gameObject.GetComponent<DistanceObject>();
    }
#endif
}
