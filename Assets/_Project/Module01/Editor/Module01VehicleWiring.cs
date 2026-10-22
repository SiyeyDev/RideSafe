using System.Linq;
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
            VehicleStage stage = Module01EditorComponents.Ensure<VehicleStage>(director.gameObject);
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

            // Los puntos de inspeccion los cuelga el escenario del vehiculo encendido, en
            // runtime: hay una sola entidad por punto para los dos vehiculos, asi que no se
            // puede dejar decidido en la escena de cual cuelga.
            SerializedProperty points = so.FindProperty("_inspectionPoints");
            points.ClearArray();
            WorldObjectTaskEntity[] found = Object
                .FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => e.ItemId.StartsWith("point_"))
                .OrderBy(e => e.ItemId)
                .ToArray();
            for (int i = 0; i < found.Length; i++)
            {
                points.InsertArrayElementAtIndex(i);
                points.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            Debug.Log($"[Module01] Escenario de vehiculos con {index} mallas y {found.Length} puntos de inspeccion.");
        }

        /// <summary>
        /// Borra el gemelo del vehículo: el nombre del modelo más " (1)", <b>y solo si está
        /// encima del punto de exhibición</b>. El escenario apaga únicamente la malla que
        /// conoce, así que un gemelo ahí se quedaría a la vista al elegir el otro vehículo.
        /// <para>
        /// El radio no sobra. El garaje v7 trae <b>dos e-bikes colgadas de una estantería</b> a
        /// casi 7 m del pedestal, que son decorado y no gemelos, y la regla sin radio se las
        /// llevaba por delante sin que nadie se enterara: el módulo seguía pasando sus pruebas
        /// y la escena perdía arte recién entregado.
        /// </para>
        /// </summary>
        private static void RemoveStrayCopies(string modelName, GameObject keep)
        {
            // Lo que cabe confundir con el vehículo en exhibición. Mas lejos que esto, es
            // decorado: el aprendiz no va a creer que una bici en una balda es la suya.
            const float confusingRadius = 1.5f;

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Destroyed earlier in this loop with its parent: the list was taken before.
                if (go == null || go == keep || !go.name.StartsWith(modelName + " ("))
                    continue;
                // The module 0 showroom's copy, «ebike (display)», is no twin: it sits in the
                // vehicle showroom and only looks like the vehicle. Staging the module 1 vehicle
                // deleted it on 2026-10-21.
                if (go.name == modelName + " (display)")
                    continue;

                float distance = keep == null
                    ? 0f
                    : Vector3.Distance(go.transform.position, keep.transform.position);

                if (keep != null && distance > confusingRadius)
                {
                    Debug.Log(
                        $"[Module01] '{go.name}' se queda: esta a {distance:F1} m del vehiculo en " +
                        "exhibicion, asi que es decorado y no un gemelo.");
                    continue;
                }

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
