using UnityEngine;

public class InitializeFade : MonoBehaviour
{
    [SerializeField] bool _fadeIn;

    private void Awake()
    {
        if (_fadeIn)
            FadeUI.FadeIn(0.1f);
        else
            FadeUI.FadeOut(0.1f);

    }
}
