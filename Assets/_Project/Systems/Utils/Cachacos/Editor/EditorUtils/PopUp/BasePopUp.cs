#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Cachacos
{
    public abstract class BasePopUp<T> : EditorWindow
    {
        private static readonly float _pading = 4;
        private static readonly float _heightMultiplier = 0.85f;
        private static BasePopUp<T> _window;
        protected static T selection;
        private static string[] _options;
        private static string _removePart;
        private static GUIStyle _selected;
        private static GUIStyle _deselected;
        private Vector2 _scrollPosition;

        public static void Open<TPopUp>(Rect buttonSize, string[] options, ref T selectedOption, Color buttonColor = default, string removePart = "" ) where TPopUp : BasePopUp<T>
        {
            _options = options;
            selection = selectedOption;
            _removePart = removePart;
            if(buttonColor == default) 
                buttonColor = Color.white;
            _selected = EditorGUIUtils.GetButtonStyle(buttonColor);
            _deselected = EditorGUIUtils.GetButtonStyle(buttonColor * 0.75f, buttonColor * 0.25f);
            if (_window != null)
                _window.Close();
            _window = CreateInstance<TPopUp>();
            Vector2 windowSize = GetWindowSize(options, removePart);
            _window.ShowAsDropDown(buttonSize, windowSize);
        }
        private static Vector2 GetWindowSize(string[] options, string removePart)
        {
            Vector2 windowSize = Defaultsize();
            float totalContentHeight = 0;
            foreach (string option in options)
            {
                string nameDisplayed = string.IsNullOrEmpty(removePart) ? option : option.Replace(removePart, "");
                Vector2 size = _selected.CalcSize(new GUIContent(nameDisplayed));
                windowSize.x = Mathf.Max(windowSize.x, size.x) + _selected.margin.top + _selected.margin.bottom; 
                totalContentHeight += size.y + _selected.margin.top + _selected.margin.bottom; ;
            }
            windowSize.x += _pading;
            windowSize.y = Mathf.Min(totalContentHeight, Screen.currentResolution.height * _heightMultiplier);
            return windowSize;
        }
        protected static Vector2 Defaultsize() =>Vector2.zero;
        private void OnGUI()
        {
            if (_options == null)
            {
                Close();
                return;
            }
            Draw();
        }
        protected virtual void Draw()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (string option in _options)
            {
                string nameDisplayed = string.IsNullOrEmpty(_removePart) ? option : option.Replace(_removePart, "");
                bool isSelected = CompareSelection(option);
                if (GUILayout.Button(nameDisplayed, isSelected ? _selected : _deselected))
                    ClickButton(option);
            }
            EditorGUILayout.EndScrollView();
        }
        protected abstract bool CompareSelection(string option);
        protected abstract void ClickButton(string option);
    }
}
#endif