using Autohand.Demo;

namespace RideSafe.Tutorial.AutoHand
{
    /// <summary>
    /// Translation between AutoHand's <see cref="CommonButton"/> and the backend-free
    /// <see cref="ControllerControl"/>. The only place the two vocabularies meet.
    /// </summary>
    public static class AutoHandControlMap
    {
        public static ControllerControl ToControl(CommonButton button)
        {
            switch (button)
            {
                case CommonButton.triggerButton: return ControllerControl.Trigger;
                case CommonButton.gripButton: return ControllerControl.Grip;
                case CommonButton.primaryButton:
                case CommonButton.primaryTouch: return ControllerControl.PrimaryButton;
                case CommonButton.secondaryButton:
                case CommonButton.secondaryTouch: return ControllerControl.SecondaryButton;
                case CommonButton.primary2DAxisClick:
                case CommonButton.primary2DAxisTouch: return ControllerControl.Thumbstick;
                case CommonButton.menuButton: return ControllerControl.Menu;
                default: return ControllerControl.None;
            }
        }

        /// <summary>None for controls AutoHand cannot read (the OS-reserved system button).</summary>
        public static CommonButton ToCommonButton(ControllerControl control)
        {
            switch (control)
            {
                case ControllerControl.Trigger: return CommonButton.triggerButton;
                case ControllerControl.Grip: return CommonButton.gripButton;
                case ControllerControl.PrimaryButton: return CommonButton.primaryButton;
                case ControllerControl.SecondaryButton: return CommonButton.secondaryButton;
                case ControllerControl.Thumbstick: return CommonButton.primary2DAxisClick;
                case ControllerControl.Menu: return CommonButton.menuButton;
                default: return CommonButton.none;
            }
        }
    }
}
