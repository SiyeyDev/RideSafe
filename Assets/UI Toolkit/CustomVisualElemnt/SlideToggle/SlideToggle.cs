using UnityEngine;
using UnityEngine.UIElements;


public class SlideToggle : BaseField<bool>
{
    public new class UxmlFactory : UxmlFactory<SlideToggle, UxmlTraits> { }
    public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription> { }
    public static readonly new string ussClassName = "slide-toggle";
    public static readonly new string inputUssClassName = "slide-toggle__input";
    public static readonly string inputKnobUssClassName = "slide-toggle__input-knob";
    public static readonly string inputCheckedUssClassName = "slide-toggle__input--checked";
    VisualElement m_Input;
    VisualElement m_Knob;

    public SlideToggle() : this(null) { }
    public SlideToggle(string label) : base(label, null)
    {
        AddToClassList(ussClassName);
        m_Input = this.Q(className: BaseField<bool>.inputUssClassName);
        m_Input.AddToClassList(inputUssClassName);
        Add(m_Input);
        m_Knob = new();
        m_Knob.AddToClassList(inputKnobUssClassName);
        m_Input.Add(m_Knob);
        RegisterCallback<ClickEvent>(evt => OnClick(evt));
        RegisterCallback<KeyDownEvent>(evt => OnKeydownEvent(evt));
        RegisterCallback<NavigationSubmitEvent>(evt => OnSubmit(evt));
    }

    private static void OnClick(ClickEvent evt)
    {
        SlideToggle slideToggle = evt.currentTarget as SlideToggle;
        slideToggle.ToggleValue();
        evt.StopPropagation();
    }
    private static void OnSubmit(NavigationSubmitEvent evt)
    {
        SlideToggle slideToggle = evt.currentTarget as SlideToggle;
        slideToggle.ToggleValue();
        evt.StopPropagation();
    }
    private static void OnKeydownEvent(KeyDownEvent evt)
    {
        SlideToggle slideToggle = evt.currentTarget as SlideToggle;
        if (slideToggle.panel?.contextType == ContextType.Player)
            return;
        if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space)
        {
            slideToggle.ToggleValue();
            evt.StopPropagation();
        }
    }
    private void ToggleValue() => value = !value;
    public override void SetValueWithoutNotify(bool newValue)
    {
        base.SetValueWithoutNotify(newValue);
        m_Input.EnableInClassList(inputCheckedUssClassName, newValue);
    }
}