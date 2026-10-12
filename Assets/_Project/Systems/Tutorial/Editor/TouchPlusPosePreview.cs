using System.IO;
using RideSafe.Tutorial.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Tutorial.Editor
{
    /// <summary>
    /// Renders every view pose of both Touch Plus prefabs, with the explained control lit, into
    /// one contact sheet (<see cref="OutputPath"/>), to judge the poses without entering Play.
    /// Uses a temporary scene that is closed afterwards; nothing is saved.
    /// </summary>
    public static class TouchPlusPosePreview
    {
        public const string OutputPath = "Temp/ridesafe-touchplus-poses.png";
        private const int Tile = 320;

        private static readonly ControllerControl[] Controls =
        {
            ControllerControl.Trigger, ControllerControl.Grip, ControllerControl.PrimaryButton,
            ControllerControl.SecondaryButton, ControllerControl.Thumbstick, ControllerControl.Menu, ControllerControl.System
        };

        [MenuItem("RideSafe/Tutorial/Previsualizar poses Touch Plus")]
        public static void Render()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Camera camera = new GameObject("Preview Camera").AddComponent<Camera>();
                SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
                camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0.32f), Quaternion.LookRotation(Vector3.back));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.82f, 0.84f, 0.88f);
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.01f;
                camera.scene = scene;

                Light light = new GameObject("Preview Light").AddComponent<Light>();
                SceneManager.MoveGameObjectToScene(light.gameObject, scene);
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
                light.intensity = 1.3f;

                string[] prefabs = { TouchPlusPrefabBuilder.LeftPrefabPath, TouchPlusPrefabBuilder.RightPrefabPath };
                Texture2D sheet = new Texture2D(Tile * Controls.Length, Tile * prefabs.Length, TextureFormat.RGB24, false);
                RenderTexture target = RenderTexture.GetTemporary(Tile, Tile, 24);
                camera.targetTexture = target;

                for (int row = 0; row < prefabs.Length; row++)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[row]), scene);
                    ControllerModelVisual model = instance.GetComponent<ControllerModelVisual>();
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    for (int column = 0; column < Controls.Length; column++)
                    {
                        Pose pose = model.PoseFor(Controls[column]);
                        instance.transform.SetPositionAndRotation(pose.position, pose.rotation);
                        ControllerButtonVisual button;
                        bool has = model.TryGetButton(Controls[column], out button);
                        if (has)
                        {
                            button.Begin();
                            button.Apply(0f, 1f, new Color(0.45f, 0.82f, 1f), block);
                        }

                        camera.Render(); // first render may miss meshes still uploading
                        camera.Render();
                        RenderTexture.active = target;
                        sheet.ReadPixels(new Rect(0, 0, Tile, Tile), column * Tile, (prefabs.Length - 1 - row) * Tile);
                        if (has)
                            button.End();
                    }
                    Object.DestroyImmediate(instance);
                }

                sheet.Apply();
                RenderTexture.active = null;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                File.WriteAllBytes(OutputPath, sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
