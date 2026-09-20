using UnityEngine;

public class SimpleSpriteI2Translate : SimpleI2LocalizeTranslate<SpriteRenderer, Sprite>
{
    #region SimpleI2Localize Methods
    protected override void SetTerm(SpriteRenderer target, Sprite value) => target.sprite = value;
    #endregion
}
