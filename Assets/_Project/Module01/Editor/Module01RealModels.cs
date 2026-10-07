using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Cambia el <c>DisplayPrefab</c> de cada elemento del catálogo, y el modelo del
    /// perfil de vehículo, de las primitivas del greybox a los modelos entregados por
    /// el equipo 3D.
    /// <para>
    /// Solo toca datos. La lógica, la puntuación y los tests no se enteran: ese es
    /// justamente el punto de que el modelo se referencie desde el dato y no desde el
    /// código.
    /// </para>
    /// <para>
    /// Un elemento sin modelo asignado aquí conserva su primitiva gris, para que el
    /// recorrido siga siendo jugable mientras llega la pieza que falta.
    /// </para>
    /// </summary>
    public static class Module01RealModels
    {
        private const string Models = "Assets/_Project/Models/CITY/MODELADOS 3d/";
        private const string ItemsFolder = "Assets/_Project/Module01/Data/Items/";

        /// <summary>(id del elemento, ruta del modelo). Vacío = se queda en greybox.</summary>
        private static readonly (string ItemId, string Model)[] Map =
        {
            ("helmet_ok",      Models + "df_g_helmet_01.fbx"),   // Charlie 2026-10-07: este es el bueno
            ("helmet_damaged", Models + "casco 1.fbx"),
            ("glasses",        Models + "gafas.fbx"),
            ("shoes_ok",       Models + "botas.fbx"),
            ("reflective",     Models + "chaleco refectivo.fbx"),
            ("cargo_secured",  Models + "guaya candado.fbx"),
            ("cargo_handheld", Models + "MobilePhone_01.fbx")
        };

        [MenuItem("RideSafe/Módulo 1/Usar modelos reales en vez del greybox")]
        public static void Apply()
        {
            int applied = 0;
            List<string> skipped = new List<string>();

            foreach ((string itemId, string modelPath) in Map)
            {
                string assetPath = ItemsFolder + "SO_Item_" + itemId + ".asset";
                SafetyItemSO item = AssetDatabase.LoadAssetAtPath<SafetyItemSO>(assetPath);
                if (item == null)
                {
                    skipped.Add($"{itemId}: no existe {assetPath}");
                    continue;
                }

                if (string.IsNullOrEmpty(modelPath))
                {
                    skipped.Add($"{itemId}: sin modelo, se queda en greybox");
                    continue;
                }

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    skipped.Add($"{itemId}: no carga {modelPath}");
                    continue;
                }

                SerializedObject so = new SerializedObject(item);
                so.FindProperty("_displayPrefab").objectReferenceValue = model;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
                applied++;
            }

            // Los perfiles de los dos vehículos dejan de apuntar al cubo.
            PointProfileAtModel("Ebike", "ebike.fbx");
            PointProfileAtModel("Escooter", "scooter.fbx");

            AssetDatabase.SaveAssets();
            Debug.Log($"[Module01] Modelos reales aplicados: {applied}.");
            foreach (string s in skipped)
                Debug.LogWarning("[Module01] " + s);
        }

        private static void PointProfileAtModel(string assetSuffix, string fileName)
        {
            VehicleProfileSO profile = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                $"Assets/_Project/Module01/Data/SO_VehicleProfile_{assetSuffix}.asset");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Models + fileName);
            if (profile == null || model == null)
            {
                Debug.LogWarning($"[Module01] No pude apuntar {assetSuffix} a {fileName}.");
                return;
            }

            SerializedObject so = new SerializedObject(profile);
            so.FindProperty("_model").objectReferenceValue = model;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            Debug.Log($"[Module01] Perfil {assetSuffix} apuntando a {fileName}.");
        }
    }
}
