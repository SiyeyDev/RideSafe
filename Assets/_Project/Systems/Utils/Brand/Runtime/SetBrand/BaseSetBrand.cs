using Sirenix.OdinInspector;
using UnityEngine;
public abstract class BaseSetBrand<T, V> : MonoBehaviour, ISetBrandData where T : Component
{
    [InlineEditor][SerializeField] protected BaseBrandData<V> data;
    [SerializeField] protected T target;
    private BaseBrandData<V> _cachedBrandData = null;
    private BrandID _cachedBrandId = null;

    private void OnEnable() => TrySetBrand(false);
    public void TrySetBrand(bool force)
    {
        if (data.GetValue() == null)
            throw new System.ArgumentNullException($"There is not {typeof(BaseBrandData<V>)} assigned in {data.name}");
        if (target == null)
            throw new System.ArgumentNullException($"There is not {typeof(T)} assigned in {gameObject.name}");
        if (!force && _cachedBrandData == data && _cachedBrandId == data.GetBrandID())
            return;
        _cachedBrandId = data.GetBrandID();
        _cachedBrandData = data;
        SetBrand();
    }
    #region BaseSetLogo Methods
    protected abstract void SetBrand();
    #endregion
#if UNITY_EDITOR
    [Button("Update")]
    private void OnValidate()
    {
        if (target == null)
            target = gameObject.GetComponent<T>();
        TrySetBrand(true);
    }

#endif
}
