using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Cablea los dos vehículos elegibles en la escena: un perfil por vehículo en el
    /// director y la tabla del <see cref="VehicleStage"/>, que enciende la malla del
    /// elegido y apaga la otra.
    /// <para>
    /// Vive aparte de <c>Module01SceneBuilder</c> y trabaja sobre la escena abierta,
    /// porque aquel <b>regenera</b> la escena desde la de lógica y se lleva por delante
    /// todo lo que se le añadió después. Esto se puede repetir sin perder nada.
    /// </para>
    /// </summary>
    public static class Module01VehicleWiring
    {
        private const string DataFolder = "Assets/_Project/Module01/Data";

        [MenuItem("RideSafe/Módulo 1/Cablear los vehículos en la escena abierta")]
        public static void WireOpenScene()
        {
            Module01Director director = Object.FindAnyObjectByType<Module01Director>();
            if (director == null)
            {
                Debug.LogError("[Module01] La escena abierta no tiene Module01Director.");
                return;
            }

            Apply(director);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        }

        /// <summary>Idempotente: reutiliza el <see cref="VehicleStage"/> que ya esté puesto.</summary>
        public static void Apply(Module01Director director)
        {
            VehicleStage stage = director.GetComponent<VehicleStage>()
                                 ?? director.gameObject.AddComponent<VehicleStage>();
            ConfigureStage(stage);

            SetVehicles(director, Profile("Ebike"), Profile("Escooter"));
            SetReference(director, "_stage", stage);
            EditorUtility.SetDirty(director);
        }

        private static VehicleProfileSO Profile(string assetSuffix) =>
            AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                $"{DataFolder}/SO_VehicleProfile_{assetSuffix}.asset");

        /// <summary>
        /// Empareja cada perfil con su malla del garaje por el nombre del modelo. Si una
        /// malla falta, avisa en vez de dejar la tabla a medias: un vehículo que el
        /// escenario no conoce no se puede apagar, y se vería al elegir el otro.
        /// </summary>
        private static void ConfigureStage(VehicleStage stage)
        {
            SerializedObject so = new SerializedObject(stage);
            SerializedProperty list = so.FindProperty("_vehicles");
            list.ClearArray();

            int index = 0;
            foreach (string suffix in new[] { "Ebike", "Escooter" })
            {
                VehicleProfileSO profile = Profile(suffix);
                if (profile == null || profile.Model == null)
                {
                    Debug.LogWarning($"[Module01] Sin perfil o modelo para {suffix}; no entra al escenario.");
                    continue;
                }

                GameObject model = Find(profile.Model.name);
                if (model == null)
                {
                    Debug.LogWarning(
                        $"[Module01] La escena no tiene una malla '{profile.Model.name}' para {suffix}.");
                    continue;
                }

                RemoveStrayCopies(profile.Model.name, model);

                list.InsertArrayElementAtIndex(index);
                SerializedProperty element = list.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("VehicleContextValue").stringValue = profile.VehicleContextValue;
                element.FindPropertyRelative("Model").objectReferenceValue = model;
                index++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            Debug.Log($"[Module01] Escenario de vehiculos con {index} mallas.");
        }

        /// <summary>
        /// Borra las copias sueltas que dejó el paquete de arte: el nombre del modelo
        /// más " (1)". El escenario solo apaga la malla que conoce, así que un gemelo
        /// huérfano se quedaría a la vista al elegir el otro vehículo.
        /// </summary>
        private static void RemoveStrayCopies(string modelName, GameObject keep)
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == keep || !go.name.StartsWith(modelName + " ("))
                    continue;

                Debug.Log($"[Module01] Borrada la copia suelta '{go.name}' en {go.transform.position}.");
                Undo.DestroyObjectImmediate(go);
            }
        }

        private static void SetVehicles(Object target, params VehicleProfileSO[] profiles)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty("_vehicles");
            list.ClearArray();

            int index = 0;
            foreach (VehicleProfileSO profile in profiles)
            {
                if (profile == null)
                    continue;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = profile;
                index++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetReference(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Find(string name)
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name == name)
                    return go;
            return null;
        }
    }
}
