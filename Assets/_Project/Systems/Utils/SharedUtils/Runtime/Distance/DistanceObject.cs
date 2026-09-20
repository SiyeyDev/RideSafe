using Cachacos;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

public class DistanceObject : MonoBehaviour, IDistanceObject, IProvider
{
    [SerializeField] private SelectObjectType _selectObjectType = SelectObjectType.DistanceObjectPrimary;
    [SerializeField] private BoolEventChannelSO _onLoadChannel;
    [SerializeField] private bool _startActive;
    [SerializeField] private bool _hideOnDeactive;
    [SerializeField, ReadOnly] private bool _isActive;
    [SerializeField, ReadOnly] protected Transform origin;
    private DistanceObjectManager _selectObjectGesture;
    public event Action onSelect;
    [SerializeField] private UnityEvent _onSelectEvent;
    public event Action onDeselect;
    [SerializeField] private UnityEvent _onDeselectEvent;
    public event Action<bool> OnChangeState;
    [ReadOnly, ShowInInspector] protected bool selected = false;
    public bool IsActive => _isActive;
    private bool _setted;
    private bool _wasActive;

    private void Start()
    {
        Set(_startActive);
    }
    private void OnEnable()
    {
        if (_wasActive)
            Set(true, false);
    }
    private void OnDisable()
    {
        if (_wasActive)
            Set(false, false);
    }
    public void Set(bool state, bool changeLastState = true)
    {
        if (_setted && state == _isActive)
            return;
        _setted = true;
        _isActive = state;
        OnChangeState?.Invoke(state);
        if (changeLastState)
            _wasActive = state;
        if (_hideOnDeactive)
            gameObject.SetActive(state);
        if (!state)
        {
            if (_selectObjectGesture == null)
                _selectObjectGesture = ServiceLocator.Instance.RequestService<DistanceObjectManager>();
            _selectObjectGesture.SeletObjectGestures[_selectObjectType]?.Enactive();
            if (selected)
                Deselect();
            return;
        }
        if (_selectObjectGesture == null)
        {
            ServiceLocator.Instance.RequestService<DistanceObjectManager>((selectObjectGesture) =>
                {
                    _selectObjectGesture = selectObjectGesture;
                    _selectObjectGesture.SeletObjectGestures[_selectObjectType]?.Active();
                });
            return;
        }
        _selectObjectGesture.SeletObjectGestures[_selectObjectType]?.Active();
    }
    public virtual void Select(Transform origin, Vector3 offset)
    {
        selected = true;
        this.origin = origin;
        onSelect?.Invoke();
        _onSelectEvent?.Invoke();
        _onLoadChannel?.RaiseEvent(true);
    }
    public void Deselect()
    {
        selected = false;
        onDeselect?.Invoke();
        _onDeselectEvent?.Invoke();
        _onLoadChannel?.RaiseEvent(false);
    }

    [Button("Active"), ShowIf("@UnityEditor.EditorApplication.isPlaying")]
    public void Active() => Set(true);
    [Button("Enactive"), ShowIf("@UnityEditor.EditorApplication.isPlaying")]
    public void Enactive() => Set(false);
#if UNITY_EDITOR
    [Button("Select"), ShowIf("@UnityEditor.EditorApplication.isPlaying")]
    public void DebugSelect() => Select(null, Vector3.zero);
    [Button("Deselect"), ShowIf("@UnityEditor.EditorApplication.isPlaying")]
    public void DebugDeselect() => Deselect();
#endif
}

