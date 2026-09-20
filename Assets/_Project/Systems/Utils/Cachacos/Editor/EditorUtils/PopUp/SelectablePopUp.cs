#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace Cachacos
{
    public class SelectablePopUp : BasePopUp<HashSet<string>>
    {
        private static GUIStyle _close;

        protected static new Vector2 Defaultsize() =>  Vector2.up * (_close.CalcSize(new GUIContent("CLOSE")).y + _close.margin.top + _close.margin.bottom);
        protected override void Draw()
        {
            if(_close == null)
                _close = EditorGUIUtils.GetButtonStyle(Color.red, border: 2);
            base.Draw();
            if (GUILayout.Button("CLOSE", _close))
                Close();
        }
        protected override bool CompareSelection(string option) => selection.Contains(option);
        protected override void ClickButton(string option)
        {
            if (!selection.Add(option))
                selection.Remove(option);
        }
    }
}
#endif