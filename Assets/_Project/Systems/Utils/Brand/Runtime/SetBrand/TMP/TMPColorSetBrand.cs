using TMPro;
using UnityEngine;

[AddComponentMenu("SetBrand/TMP/BrandTMPColor")]
public class TMPColorSetBrand : BaseSetBrand<TMP_Text, Color>
{
    #region BaseSetLogo Reusable Methods
    protected override void SetBrand()
    {
        target.color = data.GetValue();
        target.ForceMeshUpdate();
    }
    #endregion
}
