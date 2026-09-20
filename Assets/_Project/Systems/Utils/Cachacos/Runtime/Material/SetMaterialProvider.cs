using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SetMaterialProvider : MonoBehaviour
{
    [ReadOnly] [SerializeField] private Renderer[] renderers;
    private bool isInitialized;

    private void Awake()
    {
        if (!isInitialized)
            Initialize();
    }
    private void Initialize()
    {
        isInitialized = true;
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> renderers = new List<Renderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (!allRenderers[i].enabled)
                continue;
            if (AllowComponentType(allRenderers[i]))
                renderers.Add(allRenderers[i]);
        }
        this.renderers = renderers.ToArray();
    }

    private static bool AllowComponentType(Renderer renderer)
    {
        if (renderer.GetComponent<VisualEffect>())
            return false;
        if (renderer.GetComponent<ParticleSystem>())
            return false;
        if (renderer.GetComponent<TrailRenderer>())
            return false;
        if (renderer.gameObject.name.Contains("ParticleHolder_"))
            return false;
        return true;
    }
    private void AddMaterial(Material materialToAdd, Action<Material> materialSet = null)
    {
        if (!isInitialized)
            Initialize();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            Material[] mats = new Material[renderers[i].materials.Length + 1];
            for (int j = 0; j < mats.Length - 1; j++)
                mats[j] = renderers[i].materials[j];
            mats[mats.Length - 1] = materialToAdd;
            renderers[i].materials = mats;
            materialSet?.Invoke(renderers[i].materials[mats.Length - 1]);
        }
    }
    public void RemoveMaterial(int index)
    {
        if (!isInitialized)
            Initialize();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            List<Material> mats = new List<Material>();
            mats.RemoveAt(i);
            renderers[i].materials = mats.ToArray();
        }
    }
    public void ChangeMaterial(int index, Material materialToChange, Action<Material> materialSet = null)
    {
        if (!isInitialized)
            Initialize();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            Material[] mats = renderers[i].materials;
            mats[index] = materialToChange;
            renderers[i].materials = mats;
        }
    }
    public void SetMaterial(int index = 0, Action<Material> materialSet = null)
    {
        if (!isInitialized)
            Initialize();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            int renderMatIndex = Mathf.Clamp(index, 0, renderers[i].materials.Length - 1);
            materialSet?.Invoke(renderers[i].materials[renderMatIndex]);
        }
    }
}
