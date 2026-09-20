using UnityEditor;
using UnityEngine;

public static class BrandSettingMenu
{
    [MenuItem("Tools/Cachacos/Select Brand Setting", priority = 20)]
    public static void SelectBrandSetting()
    {
        var guids = AssetDatabase.FindAssets("t:BrandSetting");
        if (guids.Length == 0)
        {
            Debug.LogWarning("No BrandSetting asset found.");
            return;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var asset = AssetDatabase.LoadAssetAtPath<BrandSetting>(path);
        Selection.activeObject = asset;
    }
    [MenuItem("Tools/Cachacos/Update Brand Data", priority = 20)]
    public static void UpdateBrandData()
    {
        MonoBehaviour[] allMonoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour monoBehaviour in allMonoBehaviours)
        {
            if (monoBehaviour is not ISetBrandData setBrandData)
                continue;
            setBrandData.TrySetBrand(true);
        }
    }
}
