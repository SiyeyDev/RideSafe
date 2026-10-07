using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RideSafe.TaskSequence;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Crea las nueve secuencias de zona del módulo 1: 3 personales y 3 por vehículo.
    /// <para>
    /// Las del vehículo llevan el requisito <c>vehicle == ebike</c> o
    /// <c>vehicle == escooter</c>, y cada una su propio <c>SequenceId</c>: el runner
    /// indexa lo completado y el punto de reanudación por ese id, así que dos vehículos
    /// con el mismo id compartirían estado.
    /// </para>
    /// <para>
    /// Los pasos se autoran en el inspector, porque cada uno necesita su clave de
    /// localización, su clave de audio y el id de entidad que existe en la escena.
    /// Aquí se fija lo que decide si la secuencia corre.
    /// </para>
    /// </summary>
    public static class Module01Sequences
    {
        private const string Folder = "Assets/_Project/Module01/Data/Sequences";

        /// <summary>(nombre del asset, sequenceId, valor exigido a 'vehicle' o vacío).</summary>
        private static readonly (string Asset, string SequenceId, string Vehicle)[] Sequences =
        {
            ("SO_Seq_Module01_Head",                 "Module01.Zone.Head",                  ""),
            ("SO_Seq_Module01_Clothing",             "Module01.Zone.Clothing",              ""),
            ("SO_Seq_Module01_Load",                 "Module01.Zone.Load",                  ""),
            ("SO_Seq_Module01_Ebike_Cockpit",        "Module01.Vehicle.Ebike.Cockpit",      "ebike"),
            ("SO_Seq_Module01_Ebike_Wheels",         "Module01.Vehicle.Ebike.Wheels",       "ebike"),
            ("SO_Seq_Module01_Ebike_Visibility",     "Module01.Vehicle.Ebike.Visibility",   "ebike"),
            ("SO_Seq_Module01_Escooter_Cockpit",     "Module01.Vehicle.Escooter.Cockpit",   "escooter"),
            ("SO_Seq_Module01_Escooter_Wheels",      "Module01.Vehicle.Escooter.Wheels",    "escooter"),
            ("SO_Seq_Module01_Escooter_Visibility",  "Module01.Vehicle.Escooter.Visibility","escooter")
        };

        [MenuItem("RideSafe/Módulo 1/Crear secuencias de zona")]
        public static void CreateSequences()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Module01/Data", "Sequences");

            foreach ((string assetName, string sequenceId, string vehicle) in Sequences)
            {
                string path = $"{Folder}/{assetName}.asset";
                TaskSequenceSO sequence = AssetDatabase.LoadAssetAtPath<TaskSequenceSO>(path);
                if (sequence == null)
                {
                    sequence = ScriptableObject.CreateInstance<TaskSequenceSO>();
                    AssetDatabase.CreateAsset(sequence, path);
                }

                Apply(sequence, sequenceId, vehicle);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Module01] Secuencias de zona listas en " + Folder);
        }

        /// <summary>
        /// Escribe id y requisitos por SerializedObject: los campos son privados y
        /// deben quedar serializados en el asset, no solo en memoria.
        /// </summary>
        private static void Apply(TaskSequenceSO sequence, string sequenceId, string vehicle)
        {
            SerializedObject so = new SerializedObject(sequence);
            so.FindProperty("_sequenceId").stringValue = sequenceId;

            SerializedProperty requirements = so.FindProperty("_contextRequirements");
            requirements.ClearArray();

            if (!string.IsNullOrEmpty(vehicle))
            {
                requirements.InsertArrayElementAtIndex(0);
                SerializedProperty element = requirements.GetArrayElementAtIndex(0);
                element.FindPropertyRelative("_key").stringValue = Module01Context.VehicleKey;
                element.FindPropertyRelative("_value").stringValue = vehicle;
                element.FindPropertyRelative("_negate").boolValue = false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sequence);
        }
    }
}
