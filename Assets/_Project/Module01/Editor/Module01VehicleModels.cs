using RideSafe.UI;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Deja en la escena abierta una instancia del modelo de cada vehículo, a su escala.
    /// Idempotente: si la instancia ya está, solo corrige la escala.
    /// <para>
    /// Dónde se planta cada vehículo no es cosa de este comando: lo apoya en el suelo del garaje
    /// <c>Module01VehicleStaging</c>, y la vitrina del M0 hace su propia copia.
    /// </para>
    /// </summary>
    public static class Module01VehicleModels
    {
        [MenuItem("RideSafe/Módulo 1/Poner los modelos de los vehículos en la escena")]
        public static void PlaceInOpenScene()
        {
            foreach (VehicleProfiles.Definition definition in VehicleProfiles.Definitions)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.ModelPath);
                if (prefab == null)
                {
                    Debug.LogError($"[Module01] No carga {definition.ModelPath}.");
                    continue;
                }

                GameObject instance = FindRoot(prefab.name);
                if (instance == null)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = prefab.name;
                    // Nace apagado: el escenario enciende el vehiculo elegido, y dos vehiculos
                    // encendidos en el mismo sitio se interpenetran.
                    instance.SetActive(false);
                    Undo.RegisterCreatedObjectUndo(instance, "Modelo del vehiculo");
                    Debug.Log($"[Module01] Puesto '{prefab.name}' en la escena.");
                }

                Undo.RecordObject(instance.transform, "Escala del vehiculo");
                instance.transform.localScale = Vector3.one * definition.SceneScale;
            }

            // Se guarda como hace Module00ShowroomSetup: el comando se corre por el puente,
            // sin nadie delante para pulsar Ctrl+S.
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static GameObject FindRoot(string name) =>
            Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(g => g.name == name && SceneFolder.TopLevel(g.transform) == g.transform);
    }
}
