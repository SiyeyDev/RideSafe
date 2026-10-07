using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RideSafe.Module01.Editor
{
    [InitializeOnLoad]
    public static class Module01QuickStart
    {
        private const string Key = "RideSafe.Module01.QuickStart";
        static Module01QuickStart() { EditorApplication.playModeStateChanged += Changed; }
        [MenuItem("RideSafe/Pruebas/Modulo 1 - Espanol, e-bike")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Object.FindAnyObjectByType<Module01Experience>()?.StartWithDefaults(); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/_Project/Scene/RideSafe_Garaje_v5.unity");
            SessionState.SetBool(Key,true);
            EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
            SessionState.SetBool(Key,false);
            EditorApplication.delayCall += () => Object.FindAnyObjectByType<Module01Experience>()?.StartWithDefaults();
        }
    }
}
