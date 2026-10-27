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
        private const string ItemsFolder = "Assets/_Project/Module01/Data/Items/";

        /// <summary>
        /// (id del elemento, ruta del modelo). Sale de <see cref="Module01Library"/>, que es la
        /// transcripción del Excel del cliente.
        /// <para>
        /// Esta lista estaba escrita a mano y era uno de los tres sitios donde vivía el
        /// catálogo. Los tres podían discrepar entre sí y con el documento, y de hecho
        /// discrepaban: aquí seguían la bufanda, el jean ancho, el pantalón deportivo y los dos
        /// pares, que el cliente nunca pidió.
        /// </para>
        /// <para>
        /// Los elementos sin modelo <b>no aparecen</b>: se montan con el signo «!» y su
        /// <c>DisplayPrefab</c> tiene que quedarse vacío para que el pedestal caiga al glifo.
        /// </para>
        /// </summary>
        private static IEnumerable<(string ItemId, string Model)> Map()
        {
            foreach (Module01Library.Entry entry in Module01Library.All)
                if (!entry.NeedsMarker)
                    yield return (entry.ItemId, entry.Model);
        }

        [MenuItem("RideSafe/Módulo 1/Usar modelos reales en vez del greybox")]
        public static void Apply()
        {
            int applied = 0;
            List<string> skipped = new List<string>();

            foreach ((string itemId, string modelPath) in Map())
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

            // Los perfiles de los vehículos dejan de apuntar al cubo. El modelo sale de
            // VehicleProfiles, la única lista de vehículos.
            foreach (VehicleProfiles.Definition definition in VehicleProfiles.Definitions)
                PointProfileAtModel(definition);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Module01] Modelos reales aplicados: {applied}.");
            foreach (string s in skipped)
                Debug.LogWarning("[Module01] " + s);
        }

        private static void PointProfileAtModel(VehicleProfiles.Definition definition)
        {
            VehicleProfileSO profile = VehicleProfiles.Load(definition);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(definition.ModelPath);
            if (profile == null || model == null)
            {
                Debug.LogWarning($"[Module01] No pude apuntar {definition.AssetSuffix} a {definition.ModelFile}.");
                return;
            }

            SerializedObject so = new SerializedObject(profile);
            so.FindProperty("_model").objectReferenceValue = model;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            Debug.Log($"[Module01] Perfil {definition.AssetSuffix} apuntando a {definition.ModelFile}.");
        }
    }
}
