using UnityEngine;
using Sirenix.OdinInspector;

namespace StepCommand
{
    [System.Serializable]
    [InlineProperty]
    public class StepCommandWrapper
    {
        [FoldoutGroup("$GetGuideName")]
        [LabelText("")]
        [SerializeField] private string _name;
        [field: FoldoutGroup("$GetGuideName")]
        [field: LabelText("")]
        [field:SerializeField]public IStepCommand StepCommand { get; private set; }

        #region Odin Drawer
        public string GetGuideName()
        {
            if (StepCommand == null)
                return $"{_name} (none)";
            return $"{_name} ({StepCommand.GetType().ToString().Replace("StepCommand.","")})";
        }
        #endregion
    }
}