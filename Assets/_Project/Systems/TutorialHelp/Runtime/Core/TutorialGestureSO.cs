using RideSafe.Tutorial;
using UnityEngine;

namespace RideSafe.Help
{
    /// <summary>
    /// One gesture the tutorial can show: which Touch Plus control lights up and is pressed,
    /// on which hand, and the short text for the first time. A new gesture is a new asset.
    /// </summary>
    [CreateAssetMenu(menuName = "RideSafe/Tutorial Help/Gesture", fileName = "SO_Gesture_")]
    public class TutorialGestureSO : ScriptableObject
    {
        public enum HandChoice
        {
            Dominant = 0,
            Left = 1,
            Right = 2
        }

        [SerializeField] private ControllerControl _control = ControllerControl.Trigger;
        [SerializeField] private HandChoice _hand = HandChoice.Dominant;
        [Tooltip("I2 term shown the first time on canvas controls. Empty: no text.")]
        [SerializeField] private string _interfaceTextKey = HelpTextKeys.SelectInterface;
        [Tooltip("I2 term shown the first time on objects in the world. Empty: no text.")]
        [SerializeField] private string _objectTextKey = HelpTextKeys.SelectObject;

        public ControllerControl Control => _control;
        public HandChoice Hand => _hand;

        public string TextKeyFor(InteractionKind kind) =>
            kind == InteractionKind.Interface ? _interfaceTextKey : _objectTextKey;

        internal void Configure(ControllerControl control, HandChoice hand, string interfaceKey, string objectKey)
        {
            _control = control;
            _hand = hand;
            _interfaceTextKey = interfaceKey;
            _objectTextKey = objectKey;
        }
    }
}
