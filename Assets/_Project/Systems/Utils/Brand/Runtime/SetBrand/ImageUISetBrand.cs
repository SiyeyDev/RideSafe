using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("SetBrand/Image/BrandImageSprite")]
public class ImageUISetBrand : BaseSetBrand<Image, Sprite>
{
    #region BaseSetLogo Reusable Methods
    protected override void SetBrand() => target.sprite = data.GetValue();
    #endregion
}

