using UnityEngine;

public class SimpleRendererI2Translate : SimpleI2LocalizeTranslate<Renderer, Material>
{
    #region SimpleI2Localize Methods
    protected override void SetTerm(Renderer target, Material value) => target.material = value;
    #endregion
}
