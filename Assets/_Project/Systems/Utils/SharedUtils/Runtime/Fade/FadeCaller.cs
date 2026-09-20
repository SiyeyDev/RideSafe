using UnityEngine;

public class FadeCaller : MonoBehaviour
{
    public void FadeIn (float transitionDuration) => FadeUI.FadeIn(transitionDuration);
    public void FadeOut (float transitionDuration) => FadeUI.FadeOut(transitionDuration);
    public void Fade() => FadeUI.Fade();
}
