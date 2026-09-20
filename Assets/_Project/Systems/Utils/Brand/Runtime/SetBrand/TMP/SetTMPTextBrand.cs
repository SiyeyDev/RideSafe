using TMPro;
using UnityEngine;

[AddComponentMenu("SetBrand/TMP/BrandTMPFontAsset")]
public class SetTMPTextBrand : BaseSetBrand<TMP_Text, TMP_FontAsset>
{
    #region BaseSetLogo Reusable Methods
    protected override void SetBrand() => target.font = data.GetValue();
    #endregion
}