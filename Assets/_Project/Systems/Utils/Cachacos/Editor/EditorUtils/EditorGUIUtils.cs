#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Cachacos
{
    public static class EditorGUIUtils
    {
        #region Constans
        public static readonly int VerticalSpace = 5;
        #endregion
        #region Button
        private static Dictionary<string, GUIStyle> _buttonStyles = new Dictionary<string, GUIStyle>();
        private static Dictionary<string, GUIStyle> _labelStyles = new Dictionary<string, GUIStyle>();
        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        public static GUIStyle GetButtonStyle(Color textColor, TextAnchor textPosition = TextAnchor.MiddleCenter, int border = 1)
        {
            string key = $"{textColor}_{textPosition}_{border}";
            if (_buttonStyles.ContainsKey(key))
                return _buttonStyles[key];
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = textPosition,
                normal = { textColor = textColor },
                fontStyle = FontStyle.Bold,
                border = new RectOffset(border, border, border, border),
                padding = new RectOffset(border, border, border, border)
            };
            buttonStyle.normal.background = MakeBorderTexture(textColor, border);
            _buttonStyles[key] = buttonStyle;
            return buttonStyle;
        }
        public static GUIStyle GetButtonStyle(Color textColor, Color backGroundColor, TextAnchor textPosition = TextAnchor.MiddleCenter, int border = 1)
        {
            string key = $"{textColor}_{backGroundColor}_{textPosition}_{border}";
            if (_buttonStyles.ContainsKey(key))
                return _buttonStyles[key];
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = textPosition,
                normal = { textColor = textColor },
                fontStyle = FontStyle.Bold,
                border = new RectOffset(border, border, border, border)
            };
            buttonStyle.normal.background = MakeBorderTexture(backGroundColor, border);
            _buttonStyles[key] = buttonStyle;
            return buttonStyle;
        }
        #endregion
        #region Text
        public static GUIStyle GetLabelStyle(Color textColor, TextAnchor textPosition = TextAnchor.MiddleCenter)
        {
            string key = $"{textColor}_{textPosition}";
            if (_labelStyles.ContainsKey(key))
                return _labelStyles[key];
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = textPosition,
                normal = { textColor = textColor },
                fontStyle = FontStyle.Bold,
            };
            _labelStyles[key] = labelStyle;
            return labelStyle;
        }
        #endregion
        #region Texture
        private static Texture2D MakeBorderTexture(Color borderColor, int borderThickness)
        {
            string key = $"{borderColor}_{borderThickness}";
            if (_textureCache.ContainsKey(key))
                return _textureCache[key];
            int size = borderThickness * 2 + 1;
            Texture2D texture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < borderThickness || x >= size - borderThickness || y < borderThickness || y >= size - borderThickness)
                        colors[y * size + x] = borderColor;
                    else
                        colors[y * size + x] = Color.clear;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            _textureCache[key] = texture;
            return texture;
        }
        #endregion
    }
}
#endif