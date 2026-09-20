using Cachacos;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class SelectObjectGesture : SerializedMonoBehaviour
{
    [SerializeField] private SelectObjectType _selectObjectType = SelectObjectType.DistanceObjectPrimary;
    [OdinSerialize] private IGestureProvider _gestureProvider;
    [SerializeField] private Transform _origin;
    [SerializeField] private float _rayRadius = 0.1f;
    [SerializeField] private LayerMask _SelectObjectLayer;
    [SerializeField] private BoolEventChannelSO _activeEvent;
    [SerializeField] private BoolEventChannelSO _selectEvent;
    [SerializeField, ReadOnly] private bool _doingGesture;
    private RaycastHit[] _hits = new RaycastHit[1];
    private RaycastHit _currentHit;
    private IDistanceObject _selectObject;
    [ShowInInspector] private int _selectObjectAttached;
    private void Awake()
    {
        ServiceLocator.Instance.RequestService<DistanceObjectManager>().RegisteSelectObjectGestures(this, _selectObjectType);
        gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        _gestureProvider.OnStartGesture += StartGesture;
        _gestureProvider.OnStopGesture += StopGesture;
    }
    private void OnDisable()
    {
        _gestureProvider.OnStartGesture -= StartGesture;
        _gestureProvider.OnStopGesture -= StopGesture;
    }
    private void Update()
    {
        if (!_doingGesture)
            return;
        int index = Physics.SphereCastNonAlloc(_origin.transform.position, _rayRadius, _origin.transform.forward, _hits, Mathf.Infinity, _SelectObjectLayer);
        if (index == 0)
            return;
        if (_selectObject != null && !_selectObject.IsActive)
        {
            _selectObject = null;
            _currentHit = new RaycastHit();
        }
        if (_currentHit.transform != null && _currentHit.transform.gameObject == _hits[0].transform.gameObject)
            return;
        _currentHit = _hits[0];
        Debug.Log($"Selected object{_hits[0].transform.name}");
        if (!_currentHit.transform.TryGetComponent(out IDistanceObject selectObject))
            return;
        if (_selectObject != null)
            Deselect();
        if (!selectObject.IsActive)
            return;
        _selectObject = selectObject;
        Vector3 hitPoint = _currentHit.point;
        Vector3 objectPivot = _currentHit.transform.position;
        Select(hitPoint - objectPivot);

    }

    private void Deselect()
    {
        Debug.Log($"Deselect object {(_selectObject as MonoBehaviour).name} in {gameObject.name} Time {Time.realtimeSinceStartup}");
        _selectEvent?.RaiseEvent(false);
        _selectObject?.Deselect();
    }
    private void Select(Vector3 offset)
    {
        _selectEvent?.RaiseEvent(true);
        _selectObject.Select(_origin, offset);
    }

    public void Active()
    {
        _selectObjectAttached++;
        gameObject.SetActive(true);

    }
    public void Enactive()
    {
        if (_selectObjectAttached == 0)
            return;
        _selectObjectAttached--;
        if (_selectObjectAttached > 0)
            return;
        _doingGesture = false;
        _selectEvent?.RaiseEvent(false);
        _activeEvent?.RaiseEvent(false);
        gameObject.SetActive(false);
    }

    private void StartGesture()
    {
        _currentHit = new RaycastHit();
        _selectObject = null;
        _doingGesture = true;
        _activeEvent?.RaiseEvent(true);
    }
    private void StopGesture()
    {
        _doingGesture = false;
        _activeEvent?.RaiseEvent(false);
        _selectEvent?.RaiseEvent(false);
        _selectObject?.Deselect();
        _selectObject = null;
    }


    private void OnDrawGizmos()
    {
        if (!_doingGesture)
            return;
        Gizmos.color = _selectObject == null ? Color.red : Color.green;
        Gizmos.DrawRay(_origin.transform.position, _origin.transform.forward * 100);

    }
}
