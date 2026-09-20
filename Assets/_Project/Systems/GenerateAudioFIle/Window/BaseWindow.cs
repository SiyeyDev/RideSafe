#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class BaseWindow
{
    protected EditorWindow window;
    protected VisualElement root;
    protected VisualElement container;
    public abstract void Show();
    public virtual void CreateGUI(EditorWindow window,VisualElement root, string windowNew)
    {
        this.window = window;
        this.root = root;
        container = root.Q<VisualElement>($"{windowNew}Config");
        AddButton($"{windowNew}Btn", SwitchVisibility);
    }
    public void SetVisibility(bool visibility) => container.style.display = visibility ? DisplayStyle.Flex : DisplayStyle.None;
    private void SwitchVisibility()
    {
        container.style.display = container.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
    }
    protected Rect GetRect(VisualElement element)
    {
        return new Rect(
            element.worldBound.x + window.position.x,
            element.worldBound.y + window.position.y,
            element.worldBound.width,
            element.worldBound.height
        );
    }
    protected Slider AddSlider(string name, EventCallback<ChangeEvent<float>> onChange)
    {
        Slider slider = root.Q<Slider>(name);
        slider.RegisterValueChangedCallback(onChange);
        return slider;
    }
    protected Button AddButton(string name, System.Action clicked)
    {
        Button button = root.Q<Button>(name);
        button.clicked += clicked;
        return button;

    }
    protected ObjectField AddObjectField(string name, EventCallback<ChangeEvent<Object>> onChange = null)
    {
        ObjectField objectField = root.Q<ObjectField>(name);
        if (onChange != null)
            objectField.RegisterValueChangedCallback(onChange);
        return objectField;
    }
    protected SlideToggle AddSlideToogle(string name, EventCallback<ChangeEvent<bool>> onChange = null)
    {

        SlideToggle slideToggle = root.Q<SlideToggle>(name);
        if (onChange != null)
            slideToggle.RegisterValueChangedCallback(onChange);
        return slideToggle;
    }

}
#endif