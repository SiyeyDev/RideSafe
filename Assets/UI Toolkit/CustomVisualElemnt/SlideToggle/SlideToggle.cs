using UnityEngine;
using UnityEngine.UIElements;


// UxmlElement en vez de UxmlFactory/UxmlTraits: desde Unity 6 el importador de UXML ignora los
// atributos de los controles que siguen en la API vieja ("Control SlideToggle uses the deprecated
// UxmlTraits API. Its attributes were ignored on import"), y por eso label y value llegaban vacios.
// BaseField<T>.UxmlSerializedData ya declara label y value, asi que no hace falta redeclararlos.
[UxmlElement]
public partial class SlideToggle : BaseField<bool>
{
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