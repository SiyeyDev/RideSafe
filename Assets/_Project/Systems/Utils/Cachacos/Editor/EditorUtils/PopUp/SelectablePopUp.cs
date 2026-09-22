#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace Cachacos
{
    public class SelectablePopUp : BasePopUp<HashSet<string>>
    {
        private static GUIStyle _close;
        private static GUIStyle CloseStyle
        {
            get
            {
                if (_close == null)
                    _close = EditorGUIUtils.GetButtonStyle(Color.red, border: 2);
                return _close;
            }
        }

        protected override Vector2 ExtraSize()
            => Vector2.up * (CloseStyle.CalcSize(new GUIContent("CLOSE")).y + CloseStyle.margin.vertical);

        protected override void Draw()
        {
            base.Draw();
            if (GUILayout.Button("CLOSE", CloseStyle))
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