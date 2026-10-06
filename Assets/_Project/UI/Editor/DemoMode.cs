using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.UI.EditorTools
{
    /// <summary>
    /// Flips the scene between the headset rig and a flat camera driven by the mouse, so the demo
    /// can be shown on a monitor when no headset is on.
    /// <para>
    /// The two cannot run at once: AutoHand's HandCanvasPointer hands its own UI camera to every
    /// world-space canvas in the scene, which would steal the mouse's aim.
    /// </para>
    /// </summary>
    public static class DemoMode
    {
        [MenuItem("RideSafe/Demo/Modo VR (gafas)", priority = 0)]
        public static void Vr() => Apply(true);

        [MenuItem("RideSafe/Demo/Modo PC (mouse)", priority = 1)]
        public static void Pc() => Apply(false);

        private static void Apply(bool vr)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject xr = Find(scene, "XRPlayer");
            GameObject desktop = Find(scene, "DesktopMode");
            if (xr == null || desktop == null)
            {
                Debug.LogError("[RideSafe] La escena necesita 'XRPlayer' y 'DesktopMode' para cambiar de modo.");
                return;
            }

            Undo.RecordObjects(new Object[] { xr, desktop }, "Cambiar modo de demo");
            xr.SetActive(vr);
            desktop.SetActive(!vr);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(vr
                ? "[RideSafe] Modo VR: XRPlayer activo. Dale Play con el Link abierto."
                : "[RideSafe] Modo PC: camara fija y mouse. El rayo de las manos queda fuera.");
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }
    }
}
