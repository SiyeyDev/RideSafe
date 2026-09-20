using Sirenix.OdinInspector;
using StepCommand;
using System;
using UnityEngine;

[Serializable]
public class ChangeMaterialByIndexStepCommand : StepCommandClass
{
    [Header("ChangeMaterial Settings")]
    [SerializeField, HideReferenceObjectPicker, TableList] private SetMaterialHelper[] _setMaterialHelper;
    #region ICommand Methods
    public override IStepCommand Initialize()
    {
        foreach (SetMaterialHelper setMaterialHelper in _setMaterialHelper)
            setMaterialHelper.Intialize();
        return base.Initialize();
    }
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        base.Execute(onComplete);
        foreach (SetMaterialHelper setMaterialHelper in _setMaterialHelper)
            setMaterialHelper.Change();
        CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
    }
    public override void InternalUndo()
    {
        foreach (SetMaterialHelper setMaterialHelper in _setMaterialHelper)
            setMaterialHelper.Undo();
        base.InternalUndo();
    }
    #endregion



    [Serializable]
    public class SetMaterialHelper
    {
        [HorizontalGroup("/2")]
        [BoxGroup("/2/Renderer"), HideLabel]
        [ValidateInput(nameof(MutbBeAssgined), "Please select a renderer")]
        [SerializeField] private Renderer _renderer;

        [HorizontalGroup("/2")]
        [BoxGroup("/2/Index"), HideLabel]
        [ShowIf(nameof(MutbBeAssgined))]
        [PropertyRange(0, nameof(GetMax))]
        [SerializeField] private int _materialIndex;


        [HorizontalGroup("/2")]
        [BoxGroup("/2/Material"), HideLabel]
        [SerializeField] private Material _newMaterial;
        private Material _oldMaterial;


        private SetMaterialProvider _setMaterialProvider;

        public void Intialize()
        {
            _materialIndex = Mathf.Clamp(_materialIndex, 0, _renderer.sharedMaterials.Length - 1);
            _setMaterialProvider = _renderer.GetOrAddComponent<SetMaterialProvider>();
        }
        public void Change()
        {
            _oldMaterial = _renderer.materials[_materialIndex];
            _setMaterialProvider.ChangeMaterial(_materialIndex, _newMaterial);
        }
        public void Undo() => _setMaterialProvider.ChangeMaterial(_materialIndex, _oldMaterial);


        private string GetName()
        {
            if (!MutbBeAssgined())
                return "Waiting for select renderer";
            return $"Target {_renderer.sharedMaterials[_materialIndex]} in {_renderer.gameObject.name}. ";
        }
        private bool MutbBeAssgined() => this._renderer != null;
        private int GetMax() => _renderer == null ? 0 : _renderer.sharedMaterials.Length - 1;
#if UNITY_EDITOR

        [PropertyOrder(-1)]
        [VerticalGroup(""),HideLabel]
        [ShowInInspector, DisplayAsString] private string Name => GetName();
#endif

    }
}
