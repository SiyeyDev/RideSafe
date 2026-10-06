using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using RideSafe.UI;

namespace RideSafe.Module01.Tests
{
    public class Module01BindingTests
    {
        private const string PrefabPath = "Assets/_Project/UI/Prefabs/PF_Module01_UI.prefab";

        [Test]
        public void El_prefab_del_modulo_1_satisface_el_binding()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "No se encontro " + PrefabPath);

            ModuleUI module = prefab.GetComponentInChildren<ModuleUI>(true);
            Assert.IsNotNull(module, "El prefab no tiene ModuleUI.");

            List<string> missing = Module01Binding.FindMissing(module);

            Assert.IsEmpty(missing,
                "PF_Module01_UI ya no coincide con el binding. Falta: " + string.Join(", ", missing) +
                ". Fue regenerado con nombres distintos?");
        }

        [Test]
        public void FindMissing_sin_ModuleUI_lo_reporta_en_vez_de_reventar()
        {
            List<string> missing = Module01Binding.FindMissing(null);

            CollectionAssert.Contains(missing, "ModuleUI");
        }
    }
}
