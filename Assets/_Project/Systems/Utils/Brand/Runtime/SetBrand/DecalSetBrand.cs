using UnityEngine;
using UnityEngine.Rendering.Universal;
[AddComponentMenu("SetBrand/BrandDecalMat")]

public class DecalSetBrand : BaseSetBrand<DecalProjector,Material>
{
    #region BaseSetLogo Reusable Methods
    protected override void SetBrand() => target.material = data.GetValue();
    #endregion
}
