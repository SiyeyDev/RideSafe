using FeedBack;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMonoBehaviour : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [BoxGroup("UIMonoBehaviour Settings")]
    [SerializeField] private BaseFeedBack _uiFeedBack;
    [BoxGroup("UIMonoBehaviour Settings")]
    [SerializeField] private BaseFeedBack _deactiveFeedBack;
    [SerializeField] private bool _hideOnDeactive;
#if UNITY_EDITOR
    [BoxGroup("UIMonoBehaviour Settings")]
    [SerializeField] private bool _showBounds;
    [BoxGroup("UIMonoBehaviour Settings")]
    [ShowIf("_showBounds")][SerializeField] private Color _boudsColors;
#endif
    private RectTransform _rectTransform;
    public RectTransform RectTransform => _rectTransform;
    private Vector4 _imageBounds = Vector4.zero;
    public Vector4 UIBounds => _imageBounds;
    [ShowInInspector] protected bool isSelected;
    [field: ReadOnly][field: SerializeField] public bool IsActive { get; private set; }
    protected virtual void Awake()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;
        Parameters parameters = new Parameters();
        parameters.gameObject1 = gameObject;
        _uiFeedBack.WithParam(parameters).Intialize();
        _deactiveFeedBack.WithParam(parameters).Intialize();
        SetBounds();
        IsActive = true;
    }
#if UNITY_EDITOR
    private void OnEnable() { }
#endif
    #region Interface Methods
    public void OnPointerEnter(PointerEventData eventData) => Selected();
    public void OnPointerExit(PointerEventData eventData) => Deselected();
    #endregion
    #region UIBase Methods
    protected virtual void Selected()
    {
        if (!IsActive)
            return;
        isSelected = true;
        _uiFeedBack?.Play(this, true);
    }
    protected virtual void Deselected()
    {
        if (!IsActive)
            return;
        isSelected = false;
        _uiFeedBack?.Play(this, false);
    }
    public virtual void Active()
    {
        IsActive = true;
        enabled = true;
        _deactiveFeedBack?.Play(this, false);
        if (_hideOnDeactive)
            gameObject.SetActive(true);
    }
    public virtual void Deactive()
    {
        IsActive = false;
        enabled = false;
        _deactiveFeedBack?.Play(this, true);
        if (_hideOnDeactive)
            gameObject.SetActive(false);
    }
    #endregion
    #region Main Methods
    private void SetBounds()
    {
        _rectTransform = transform as RectTransform;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(RectTransform);
        _imageBounds = new Vector4(
           x: bounds.extents.x - bounds.center.x,
           z: bounds.extents.x + bounds.center.x,
           y: bounds.extents.y - bounds.center.y,
           w: bounds.extents.y + bounds.center.y
        );
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showBounds)
            return;
        Gizmos.color = _boudsColors;
        SetBounds();
        Vector3 boundsSize = new Vector3(_imageBounds.x + _imageBounds.z, _imageBounds.y + _imageBounds.w, 1);
        Vector3 position = new Vector3((_imageBounds.z - _imageBounds.x) / 2, (_imageBounds.w - _imageBounds.y) / 2, 0);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.DrawWireCube(position, boundsSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
