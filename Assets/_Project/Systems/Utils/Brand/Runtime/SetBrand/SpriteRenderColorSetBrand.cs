using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("SetBrand/SpriteRenderer/BrandSpriteRendereColor")]
public class SpriteRenderColorSetBrand : BaseSetBrand<SpriteRenderer, Color>
{
    #region BaseSetLogo Reusable Methods
    protected override void SetBrand() => target.color = data.GetValue();
    #endregion
}
