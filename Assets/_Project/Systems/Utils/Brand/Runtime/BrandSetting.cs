using UnityEngine;

[CreateAssetMenu(fileName = "SO_BrandSetting", menuName = "System/Brand/Setting", order = 51)]
public class BrandSetting : ScriptableObject
{
   [field:SerializeField] public BrandID BrandID { get; private set; }
}
