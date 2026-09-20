using FeedBack;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIHandButton : UIMonoBehaviour, IPointerClickHandler
{
    [BoxGroup("Button Settings")]
    [SerializeField] private BaseFeedBack _clickFeedback;
    [BoxGroup("Button Settings")]
    public UnityEvent onClick;
    public event Action<UIHandButton> Click;
    private bool _clicking;

    protected override void Awake()
    {
        base.Awake();
        Parameters parameters = new Parameters();
        parameters.gameObject1 = gameObject;
        _clickFeedback.WithParam(parameters).Intialize();
    }
    [Button("OnClick")]
    public void ClickButton()
    {
        Debug.Log("Click");
        OnPointerClick();
    }
    #region Interface Methods
    public void OnPointerClick(PointerEventData eventData = null)
    {
        if (!enabled)
            return;
        if (_clicking)
            return;
        _clicking = true;
        _clickFeedback?.Play(this, true, AfterClick);
        onClick?.Invoke();
    }
    #endregion
    [ContextMenu("Click")]
    private void AfterClick()
    {
        _clicking = false;
        Click?.Invoke(this);
        if (isSelected)
        {
            Selected();
            return;
        }
        Deselected();
    }
}
