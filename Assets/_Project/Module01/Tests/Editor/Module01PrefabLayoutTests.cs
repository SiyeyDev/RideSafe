using NUnit.Framework;
using RideSafe.UI;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Tests
{
    /// <summary>Lo que PF_Module01_UI necesita para no pisar sus propios textos.</summary>
    public class Module01PrefabLayoutTests
    {
        private const string PrefabPath = "Assets/_Project/UI/Prefabs/PF_Module01_UI.prefab";

        private static Transform Find(Transform root, string path)
        {
            Transform found = root.Find(path);
            if (found == null)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == path)
                        return t;
                }
            }
            Assert.IsNotNull(found, root.name + " has no " + path);
            return found;
        }

        [Test]
        public void Revisa_tus_elecciones_crece_con_la_lista()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform review = Find(prefab.transform, "ReviewChoices");
            GrowWithContent grow = review.GetComponentInChildren<GrowWithContent>(true);
            Assert.IsNotNull(grow, "sin él, siete elementos pisan el aviso y los botones");
            SerializedObject serialized = new SerializedObject(grow);
            // Since 2026-10-26 the window grows, up to a cap, and the rows scroll inside it.
            Assert.AreEqual("Viewport", serialized.FindProperty("_content").objectReferenceValue.name);
            Assert.AreEqual("Rows", serialized.FindProperty("_scrolledRows").objectReferenceValue.name);
            Assert.AreEqual("ListBox", serialized.FindProperty("_box").objectReferenceValue.name);
            Assert.AreEqual("Modal", serialized.FindProperty("_recentre").objectReferenceValue.name);
            SerializedProperty push = serialized.FindProperty("_pushDown");
            string pushed = "";
            for (int i = 0; i < push.arraySize; i++)
                pushed += push.GetArrayElementAtIndex(i).objectReferenceValue.name + ";";
            foreach (string below in new[] { "LockNotice", "LockNoticeText", "KeepChoosingButton", "ConfirmButton" })
                StringAssert.Contains(below + ";", pushed, below + " baja con la lista");
        }

        private static float Left(RectTransform r) => r.anchoredPosition.x - r.pivot.x * r.sizeDelta.x;
        private static float Top(RectTransform r) => -(r.anchoredPosition.y + (1f - r.pivot.y) * r.sizeDelta.y);
        private static float Bottom(RectTransform r) => Top(r) + r.sizeDelta.y;

        [Test]
        public void Mi_Eleccion_es_una_sola_columna_alineada()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Transform panel = Find(prefab.transform, "Comparison");
            RectTransform bar = (RectTransform)Find(panel, "TitleBar");
            RectTransform note = (RectTransform)Find(panel, "Body");
            RectTransform list = (RectTransform)Find(panel, "LeftColumn");
            RectTransform title = (RectTransform)Find(bar, "Title");
            RectTransform button = (RectTransform)Find(panel, "ExplainButton");

            // Cada tarjeta trae su relleno de 9-slice; comparo el borde visible.
            float barPad = 16f / 2.4f, cardPad = 16f / 2.8f;
            Assert.AreEqual(Left(list) + cardPad, Left(bar) + barPad, 1f, "barra y lista empiezan igual");
            Assert.AreEqual(list.sizeDelta.x - 2f * cardPad, bar.sizeDelta.x - 2f * barPad, 1f, "barra y lista miden igual");
            Assert.AreEqual(list.sizeDelta.x, note.sizeDelta.x, 1f, "la nota mide lo mismo que la lista");
            Assert.AreEqual(Left(list), Left(note), 1f, "la nota empieza donde la lista");
            Assert.AreEqual(bar.sizeDelta.x / 2f, title.anchoredPosition.x + title.sizeDelta.x / 2f, 1f, "título centrado en la barra");
            Assert.AreEqual(-bar.sizeDelta.y / 2f, title.anchoredPosition.y - title.sizeDelta.y / 2f, 1f,
                            "título centrado en vertical, no pegado arriba");
            TMPro.TMP_Text titleText = title.GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual(TMPro.VerticalAlignmentOptions.Middle, titleText.verticalAlignment);
            // Orden desde 2026-10-16 (Cindy), como «Revisa tus elecciones»: barra, lista, nota; y el
            // botón libre, debajo de la caja del panel.
            Assert.GreaterOrEqual(Top(list), Bottom(bar), "la lista no se mete en la barra");
            Assert.GreaterOrEqual(Top(note), Bottom(list), "la nota va debajo de la lista");
            RectTransform box = (RectTransform)Find(panel, "Frame");
            Assert.GreaterOrEqual(Top(button), Bottom(box), "el botón va fuera de la caja, debajo");
            Assert.GreaterOrEqual(Bottom(box), Bottom(note), "la caja abarca la nota");
            Assert.IsFalse(Find(list, "Divider").gameObject.activeSelf, "sin encabezado no hay raya que lo separe");

            Assert.IsNull(panel.GetComponent<GrowWithContent>(),
                          "ComparisonView.KeepBelow ya baja el botón con la lista; dos a la vez se pelearían");
        }

        [Test]
        public void En_la_v5_ningun_objeto_que_se_inspecciona_se_funde_con_el_static_batching()
        {
            // Prueba en Quest 2026-10-16: las botas, marcadas Static, salian vacias en el pedestal.
            const string scenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var batched = new System.Collections.Generic.List<string>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (WorldObjectTaskEntity entity in root.GetComponentsInChildren<WorldObjectTaskEntity>(true))
                        foreach (Transform t in entity.GetComponentsInChildren<Transform>(true))
                            if ((GameObjectUtility.GetStaticEditorFlags(t.gameObject) & StaticEditorFlags.BatchingStatic) != 0)
                                batched.Add(t.name);
                CollectionAssert.IsEmpty(batched);
            }
            finally
            {
                if (openedHere)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void En_la_v5_la_nota_de_Mi_Eleccion_no_queda_aplastada()
        {
            const string scenePath = "Assets/_Project/Scene/RideSafe_Garaje_v5.unity";
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                RectTransform note = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true))
                        if (r.name == "Body" && r.parent.name == "Header" && r.parent.parent.name == "Comparison")
                            note = r;
                Assert.IsNotNull(note);
                Assert.Greater(note.sizeDelta.x, 100f, "una sobrescritura de la escena la dejaba en 0×0");
                Assert.Greater(note.sizeDelta.y, 20f);

                FollowBelow follow = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (FollowBelow f in root.GetComponentsInChildren<FollowBelow>(true))
                        if (f.name == "FinalResult")
                            follow = f;
                Assert.IsNotNull(follow, "los botones finales siguen a la caja de Mi Elección");
                Object target = new SerializedObject(follow).FindProperty("_target").objectReferenceValue;
                Assert.AreEqual("Frame", target.name, "la caja entera, no la lista: la nota va debajo de la lista");
                Assert.AreEqual("Comparison", ((Component)target).transform.parent.name);
            }
            finally
            {
                if (openedHere)
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
