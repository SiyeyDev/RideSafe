using RideSafe.UI;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Arreglos de composición sobre PF_Module01_UI, editando el prefab en sitio en vez de
    /// regenerarlo: regenerar le da ids internos nuevos y rompería las referencias de la escena.
    /// UIModules.Module01 construye lo mismo, así que una regeneración futura no lo pierde.
    /// Idempotente.
    /// </summary>
    public static class Module01LayoutFixes
    {
        public const string PrefabPath = "Assets/_Project/UI/Prefabs/PF_Module01_UI.prefab";

        [MenuItem("RideSafe/Módulo 1/Arreglar composición de paneles")]
        public static void Apply()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                WireReviewChoices(Find(root.transform, "ReviewChoices"));
                ArrangeMyChoice((RectTransform)Find(root.transform, "Comparison"));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[Module01] Panel layout fixes applied to " + PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public const string ScenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";

        /// <summary>
        /// La escena v5 guardaba una sobrescritura de la instancia que dejaba la nota de «Mi
        /// Elección» en 0×0 (resto de cuando la medía un ContentSizeFitter). Con la nota de ancho
        /// fijo, ese 0 ganaba al prefab y el texto salía en una columna de letras. Se revierte
        /// solo esa sobrescritura y se guarda la escena.
        /// </summary>
        [MenuItem("RideSafe/Módulo 1/Quitar sobrescritura de la nota de Mi Elección (Garaje_v5)")]
        public static void RevertSceneNoteOverride()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                int reverted = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true))
                    {
                        if (r.name != "Body" || r.parent == null || r.parent.name != "Header" ||
                            r.parent.parent == null || r.parent.parent.name != "Comparison" ||
                            !PrefabUtility.IsPartOfPrefabInstance(r))
                            continue;
                        PrefabUtility.RevertObjectOverride(r, InteractionMode.AutomatedAction);
                        reverted++;
                    }
                }
                WireFinalResult(scene);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log("[Module01] Reverted " + reverted + " override(s) on Comparison/Header/Body in " + ScenePath);
            }
            finally
            {
                if (openedHere)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// Los botones finales («Continuar al Módulo 2», «Repetir Módulo») estaban clavados a
        /// y = −358 del HUD y la lista larga de «Mi Elección» les pasaba por debajo: los sigue a ella.
        /// </summary>
        private static void WireFinalResult(UnityEngine.SceneManagement.Scene scene)
        {
            RectTransform result = null, column = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (r.name == "FinalResult")
                        result = r;
                    if (r.name == "LeftColumn" && r.parent != null && r.parent.name == "Comparison")
                        column = r;
                }
            }
            if (result == null || column == null)
                throw new System.InvalidOperationException("FinalResult or Comparison/LeftColumn missing in " + scene.name);
            FollowBelow follow = result.GetComponent<FollowBelow>();
            if (follow == null)
                follow = result.gameObject.AddComponent<FollowBelow>();
            follow.Configure(column, 14f);
            EditorUtility.SetDirty(follow);
        }

        /// <summary>«Revisa tus elecciones»: la lista crece y el aviso y los botones quedan debajo.</summary>
        private static void WireReviewChoices(Transform panel)
        {
            RectTransform modal = (RectTransform)Find(panel, "Modal");
            GrowWithContent grow = modal.GetComponent<GrowWithContent>();
            if (grow == null)
                grow = modal.gameObject.AddComponent<GrowWithContent>();
            grow.Configure((RectTransform)Find(modal, "Rows"), (RectTransform)Find(modal, "ListBox"),
                           new[] { modal },
                           new[]
                           {
                               (RectTransform)Find(modal, "LockNotice"), (RectTransform)Find(modal, "LockNoticeText"),
                               (RectTransform)Find(modal, "KeepChoosingButton"), (RectTransform)Find(modal, "ConfirmButton")
                           },
                           modal);
            EditorUtility.SetDirty(grow);
        }

        /// <summary>«Mi Elección»: barra, nota y lista en una sola columna; el botón sigue a la lista.</summary>
        private static void ArrangeMyChoice(RectTransform panel)
        {
            RectTransform bar = (RectTransform)Find(panel, "TitleBar");
            ColumnComposition.Arrange(panel, bar, 16f / 2.4f, (RectTransform)Find(bar, "Title"),
                                      (RectTransform)Find(panel, "Body"), (RectTransform)Find(panel, "LeftColumn"), 16f / 2.8f,
                                      (RectTransform)Find(panel, "ExplainButton"));
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            throw new System.InvalidOperationException(root.name + " has no " + name);
        }
    }
}
