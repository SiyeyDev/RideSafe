#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Cachacos
{
    public class Notification : EditorWindow
    {
        private static Notification _window;
        private static string _message;
        private static GUIStyle _messageStyle;
        private static GUIStyle _contourStyle;
        private static GUIStyle _buttonStyle;
        private static float _padding = 32;
        private static float _maxWidth = 512;

        public static void Open(string message)
        {
            if (_window != null)
                _window.Close();
            _message = message;
            _window = CreateInstance<Notification>();
            _window.ShowPopup();
            _window.position = new Rect((Screen.width / 2) - 2, (Screen.height / 2) - 2, 4, 4);
            _window.maxSize = new Vector2(1000, 1000);
            _window.minSize = new Vector2(0, 0);
        }

        private void OnGUI()
        {
            if (_window == null)
            {
                Close();
                return;
            }
            if (_window != this)
            {
                Close();
                return;
            }
            IntializeStyles();
            SetWindowSize();
            GUILayout.BeginVertical("", _contourStyle);
            GUILayout.Space(EditorGUIUtils.VerticalSpace);
            GUILayout.Label(_message, _messageStyle);
            GUILayout.Space(EditorGUIUtils.VerticalSpace);
            if (GUILayout.Button("OK", _buttonStyle))
                _window.Close();
            GUILayout.Space(EditorGUIUtils.VerticalSpace);
            GUILayout.EndVertical();
        }

        private void IntializeStyles()
        {
            if (_messageStyle == null)
            {
                _messageStyle = EditorGUIUtils.GetLabelStyle(Color.white);
                _messageStyle.wordWrap = true;
                _messageStyle.fontSize = 16;
            }
            if (_contourStyle == null)
                _contourStyle = EditorGUIUtils.GetButtonStyle(Color.white);
            if (_buttonStyle == null)
            {
                _buttonStyle = EditorGUIUtils.GetButtonStyle(Color.green);
                _messageStyle.fontSize = 16;
            }
        }

        private void SetWindowSize()
        {
            Vector2 textSize = _messageStyle.CalcSize(new GUIContent(_message));
            Vector2 buttonSize = _buttonStyle.CalcSize(new GUIContent(_message));
            float width = Mathf.Clamp(textSize.x + _padding * 2, _padding, _maxWidth);
            float height = textSize.y + buttonSize.y;
            height += GetSyleExtraHeight(_messageStyle);
            height += GetSyleExtraHeight(_buttonStyle);
            height += GetSyleExtraHeight(_contourStyle);
            height += EditorGUIUtils.VerticalSpace * 3;
            Rect mainWindow = new Rect(Screen.currentResolution.width / 2 - width / 2,
                                       Screen.currentResolution.height / 2 - height / 2,
                                       width, height);
            position = mainWindow;
        }

        private float GetSyleExtraHeight(GUIStyle style)
        {
            float bounds = style.margin.top;
            bounds += style.margin.bottom;
            bounds += style.padding.bottom;
            bounds += style.padding.bottom;
            return bounds;
        }
    }
}
#endif