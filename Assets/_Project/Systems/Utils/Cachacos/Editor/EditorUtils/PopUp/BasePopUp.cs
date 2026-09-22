#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Cachacos
{
    public abstract class BasePopUp<T> : EditorWindow
    {
        private static readonly float _pading = 4;
        private static readonly float _heightMultiplier = 0.85f;
        private static readonly float _minWidth = 120;
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
            _window.ShowAsDropDown(buttonSize, _window.GetWindowSize(options, removePart));
        }
        /// <summary>
        /// Espacio, fuera de la lista, que necesita el popup: el boton CLOSE de
        /// <see cref="SelectablePopUp"/>, por ejemplo. Antes era un static que la hija ocultaba
        /// con 'new'; los statics no son virtuales, asi que siempre se ejecutaba el de la clase
        /// base y ese espacio nunca se reservaba.
        /// </summary>
        protected virtual Vector2 ExtraSize() => Vector2.zero;

        private Vector2 GetWindowSize(string[] options, string removePart)
        {
            float contentWidth = _minWidth;
            float contentHeight = 0;
            foreach (string option in options)
            {
                string nameDisplayed = string.IsNullOrEmpty(removePart) ? option : option.Replace(removePart, "");
                Vector2 size = _selected.CalcSize(new GUIContent(nameDisplayed));
                // El ancho es el del elemento mas largo. Antes se sumaban los margenes en cada
                // vuelta del bucle, asi que el ancho dependia de cuantos elementos habia: con
                // muchos sobraba sitio y con pocos el nombre salia cortado.
                contentWidth = Mathf.Max(contentWidth, size.x + _selected.margin.horizontal);
                contentHeight += size.y + _selected.margin.vertical;
            }
            Vector2 extra = ExtraSize();
            // La barra vertical del ScrollView se come ancho: sin reservarlo, el texto queda
            // cortado y encima aparece una barra horizontal que roba alto a la lista.
            float width = contentWidth + extra.x + _pading + GUI.skin.verticalScrollbar.fixedWidth;
            float height = Mathf.Min(contentHeight + extra.y + _pading, Screen.currentResolution.height * _heightMultiplier);
            return new Vector2(width, height);
        }
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