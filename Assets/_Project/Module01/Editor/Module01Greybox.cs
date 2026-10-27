using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Crea los datos de la primera pasada del módulo 1: los ocho elementos de
    /// protección personal, sus tres zonas, el catálogo, el e-bike de cubos con sus
    /// puntos de inspección y el perfil que los une.
    /// <para>
    /// Es idempotente: vuelve a correrse sobre los mismos assets sin duplicarlos, de
    /// modo que ajustar una categoría o una clave es editar aquí y repetir.
    /// </para>
    /// <para>
    /// Las primitivas llevan un material distinto por elemento, nunca por categoría:
    /// el color no puede filtrar cuál es la respuesta correcta.
    /// </para>
    /// </summary>
    public static class Module01Greybox
    {
        private const string DataFolder = "Assets/_Project/Module01/Data";
        private const string ItemsFolder = DataFolder + "/Items";
        private const string ZonesFolder = DataFolder + "/Zones";
        private const string GreyboxFolder = DataFolder + "/Greybox";

        /// <summary>
        /// Los elementos y las zonas <b>ya no se escriben aquí</b>: salen de
        /// <see cref="Module01Library"/>, que es la transcripción del Excel del cliente.
        /// <para>
        /// Antes esta clase llevaba su propia lista, y por eso la escena acabó con cinco
        /// elementos que Daniel modeló para adelantar trabajo y la hoja nunca pidió. Con la
        /// tabla fuera, añadir o quitar un EPP es editar el documento transcrito y repetir el
        /// comando, y no hay dos sitios que puedan discrepar.
        /// </para>
        /// </summary>
        private static (string ZoneId, string TitleKey, string[] ItemIds)[] Zones()
        {
            var zones = new (string, string, string[])[Module01Library.Tables.Length];
            for (int i = 0; i < Module01Library.Tables.Length; i++)
            {
                var table = Module01Library.Tables[i];
                List<Module01Library.Entry> entries = Module01Library.Of(table.Id);
                var ids = new string[entries.Count];
                for (int j = 0; j < entries.Count; j++)
                    ids[j] = entries[j].ItemId;
                zones[i] = (table.Id, table.TitleKey, ids);
            }
            return zones;
        }

        /// <summary>Puntos de inspección del e-bike: (id, clave, posición en el cubo).</summary>
        private static readonly (string Id, string NameKey, Vector3 Offset)[] VehiclePoints =
        {
            ("point_steering",  "M1/Point_Steering",  new Vector3(0f, 0.55f, 0.35f)),
            ("point_brakes",    "M1/Point_Brakes",    new Vector3(0.25f, 0.5f, 0.3f)),
            ("point_electrics", "M1/Point_Electrics", new Vector3(0f, 0.1f, 0f)),
            ("point_wheels",    "M1/Point_Wheels",    new Vector3(0f, -0.3f, 0.5f)),
            ("point_lights",    "M1/Point_Lights",    new Vector3(0f, 0.3f, 0.6f)),
            ("point_deck",      "M1/Point_Deck",      new Vector3(0f, -0.2f, 0f))
        };

        private static readonly (string ZoneId, string TitleKey, string[] PointIds)[] VehicleZones =
        {
            ("cockpit",    "M1/Zone_Cockpit",    new[] { "point_steering", "point_brakes", "point_electrics" }),
            ("wheels",     "M1/Zone_Wheels",     new[] { "point_wheels" }),
            ("visibility", "M1/Zone_Visibility", new[] { "point_lights" }),
            // Solo la recorre el scooter: narrativa §4.8.2, «Deck/baseboard and visible
            // load-bearing fasteners». Que zona recorre cada vehiculo lo dice VehicleProfiles.
            ("deck",       "M1/Zone_Deck",       new[] { "point_deck" })
        };

        /// <summary>
        /// Rehace <b>solo</b> los EPP: sus <c>SafetyItemSO</c>, las tres mesas y el catálogo,
        /// desde <see cref="Module01Library"/>. Es el comando que se repite cada vez que cambia
        /// el Excel del cliente.
        /// <para>
        /// No toca el vehículo. <c>Crear datos de la primera pasada</c> sí lo rehace, y aunque
        /// <c>VehicleProfileSO.Configure</c> no borre los <c>_partBindings</c> —comprobado—, no
        /// hay motivo para pasar cerca del cableado de las piezas solo para añadir un EPP.
        /// </para>
        /// </summary>
        [MenuItem("RideSafe/Módulo 1/Rehacer el catálogo de EPP desde la biblioteca")]
        public static void RebuildEppCatalog()
        {
            EnsureFolders();
            RetireItemsTheSheetDoesNotAskFor();

            Dictionary<string, SafetyItemSO> items = CreateItems();
            List<ZoneSO> zones = CreateZones(Zones(), items, "SO_Zone_");
            CreateCatalog(zones);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int conModelo = 0, conMarcador = 0;
            foreach (Module01Library.Entry e in Module01Library.All)
                if (e.NeedsMarker) conMarcador++; else conModelo++;

            Debug.Log($"[Module01] Catálogo rehecho desde la biblioteca: {Module01Library.All.Length} "
                      + $"elementos en {zones.Count} mesas. {conModelo} con modelo, "
                      + $"{conMarcador} con el signo «!» a la espera de 3D.");
        }

        [MenuItem("RideSafe/Módulo 1/Crear datos de la primera pasada")]
        public static void CreateFirstPassData()
        {
            EnsureFolders();

            RetireItemsTheSheetDoesNotAskFor();

            Dictionary<string, SafetyItemSO> items = CreateItems();
            List<ZoneSO> personalZones = CreateZones(Zones(), items, "SO_Zone_");
            CreateCatalog(personalZones);

            RebuildVehicleData();
            Debug.Log("[Module01] Datos de la primera pasada listos en " + DataFolder);
        }

        /// <summary>
        /// Rehace <b>solo</b> los datos del vehículo: los puntos, sus zonas y los dos perfiles.
        /// No toca los EPP ni las piezas ni los giros de cada perfil (<c>Configure</c> no borra
        /// <c>_partBindings</c> ni <c>_zoneViews</c>; eso lo escribe el montador del vehículo).
        /// </summary>
        [MenuItem("RideSafe/Módulo 1/Rehacer los datos del vehículo")]
        public static void RebuildVehicleData()
        {
            EnsureFolders();
            GameObject greybox = CreateEbikeGreybox(null);
            Dictionary<string, SafetyItemSO> pointItems = CreateVehiclePointItems();
            List<ZoneSO> vehicleZones = CreateZones(VehicleZones, pointItems, "SO_ZoneVehicle_");
            CreateVehicleProfile(greybox, vehicleZones);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Module01] Datos del vehículo rehechos: " + vehicleZones.Count + " zonas.");
        }

        private static void EnsureFolders()
        {
            foreach (string folder in new[] { DataFolder, ItemsFolder, ZonesFolder, GreyboxFolder })
            {
                if (AssetDatabase.IsValidFolder(folder))
                    continue;
                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }

        /// <summary>
        /// Borra los <c>SafetyItemSO</c> de lo que Daniel modeló para adelantar trabajo y la
        /// hoja no pide. Sin esto, volver a correr el comando dejaría los viejos en disco y el
        /// montador de escena los seguiría colocando.
        /// <para>
        /// Solo se borra el dato del catálogo: <b>el arte se queda</b>. La bufanda y las
        /// rodilleras siguen en la carpeta de modelos por si algún día el cliente los pide.
        /// </para>
        /// </summary>
        private static void RetireItemsTheSheetDoesNotAskFor()
        {
            foreach (string id in Module01Library.RetiredItemIds)
            {
                string path = $"{ItemsFolder}/SO_Item_{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<SafetyItemSO>(path) == null)
                    continue;
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[Module01] retirado {id}: no está en la biblioteca del cliente.");
            }
        }

        private static Dictionary<string, SafetyItemSO> CreateItems()
        {
            Dictionary<string, SafetyItemSO> map = new Dictionary<string, SafetyItemSO>();
            foreach (Module01Library.Entry entry in Module01Library.All)
            {
                string id = entry.ItemId;
                SafetyItemSO asset = LoadOrCreate<SafetyItemSO>($"{ItemsFolder}/SO_Item_{id}.asset");

                asset.ConfigureForTests(id, entry.NameKey, entry.NameKey + "_Risk", entry.Category);
                asset.ConfigureWeightForTests(entry.Weight, entry.HazardGroup);
                asset.ConfigureBonusForTests(entry.ExclusiveBonusWith);

                // Sin modelo, el prefab de exhibición se queda VACÍO a propósito: así
                // ItemInspector.BuildDisplay cae al glifo y el pedestal enseña el «!», igual
                // que los puntos del vehículo que no tienen pieza propia. Un cubo de greybox
                // aquí taparía ese camino y el aprendiz vería una caja gris sin explicación.
                GameObject display = entry.NeedsMarker
                    ? null
                    : AssetDatabase.LoadAssetAtPath<GameObject>(entry.Model);

                if (!entry.NeedsMarker && display == null)
                    Debug.LogError($"[Module01] {entry.LibraryId}: no carga {entry.Model}");

                SetDisplayPrefab(asset, display);
                EditorUtility.SetDirty(asset);
                map[id] = asset;
            }
            return map;
        }

        private static Dictionary<string, SafetyItemSO> CreateVehiclePointItems()
        {
            Dictionary<string, SafetyItemSO> map = new Dictionary<string, SafetyItemSO>();
            foreach ((string id, string nameKey, Vector3 _) in VehiclePoints)
            {
                SafetyItemSO asset = LoadOrCreate<SafetyItemSO>($"{ItemsFolder}/SO_Item_{id}.asset");
                // Todo punto pre-operativo del e-bike es núcleo: omitir cualquiera es
                // dejar el vehículo sin diagnosticar.
                asset.ConfigureForTests(id, nameKey, nameKey + "_Risk", SafetyItemCategory.Core);
                EditorUtility.SetDirty(asset);
                map[id] = asset;
            }
            return map;
        }

        private static List<ZoneSO> CreateZones((string ZoneId, string TitleKey, string[] Ids)[] source,
                                                Dictionary<string, SafetyItemSO> items, string prefix)
        {
            List<ZoneSO> zones = new List<ZoneSO>();
            foreach ((string zoneId, string titleKey, string[] ids) in source)
            {
                ZoneSO zone = LoadOrCreate<ZoneSO>($"{ZonesFolder}/{prefix}{zoneId}.asset");
                List<SafetyItemSO> members = new List<SafetyItemSO>();
                foreach (string id in ids)
                    members.Add(items[id]);
                zone.ConfigureForTests(zoneId, titleKey, members);
                EditorUtility.SetDirty(zone);
                zones.Add(zone);
            }
            return zones;
        }

        private static void CreateCatalog(List<ZoneSO> zones)
        {
            Module01CatalogSO catalog = LoadOrCreate<Module01CatalogSO>($"{DataFolder}/SO_Module01Catalog.asset");
            catalog.ConfigureForTests(zones);
            EditorUtility.SetDirty(catalog);
        }

        /// <summary>
        /// Un perfil por vehículo, cada uno con <b>sus</b> zonas de <see cref="VehicleProfiles"/>.
        /// Las que comparten —cockpit, wheels, visibility— son el mismo <see cref="ZoneSO"/>, así
        /// que los cinco puntos y sus claves no se duplican; el scooter suma la plataforma.
        /// </summary>
        private static void CreateVehicleProfile(GameObject greybox, List<ZoneSO> zones)
        {
            foreach (VehicleProfiles.Definition definition in VehicleProfiles.Definitions)
            {
                List<ZoneSO> own = new List<ZoneSO>();
                foreach (string zoneId in definition.ZoneIds)
                {
                    ZoneSO zone = zones.Find(z => z.ZoneId == zoneId);
                    // Un hueco en la lista para el recorrido ahí: mejor decirlo que dejarlo.
                    if (zone == null)
                        Debug.LogError($"[Module01] {definition.AssetSuffix} declara la zona '{zoneId}', que no existe.");
                    else
                        own.Add(zone);
                }

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(definition.ModelPath);
                if (model == null)
                    Debug.LogError($"[Module01] {definition.AssetSuffix}: no carga {definition.ModelPath}.");

                VideoClip video = AssetDatabase.LoadAssetAtPath<VideoClip>(definition.VideoPath);
                if (video == null)
                    Debug.LogError($"[Module01] {definition.AssetSuffix}: no carga el video {definition.VideoPath}.");

                VehicleProfileSO profile = LoadOrCreate<VehicleProfileSO>(VehicleProfiles.AssetPath(definition));
                profile.Configure(definition.ContextValue, model != null ? model : greybox, own);
                profile.ConfigureVideo(video);
                EditorUtility.SetDirty(profile);
            }
        }

        /// <summary>Un prop gris por elemento, con collider y entidad ya configurada.</summary>
        private static GameObject CreateProp(string itemId)
        {
            string path = $"{GreyboxFolder}/PF_Prop_{itemId}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Prop_" + itemId;
            go.transform.localScale = Vector3.one * 0.25f;
            ConfigureEntity(go, $"module01.item.{itemId}", itemId);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateEbikeGreybox(Dictionary<string, SafetyItemSO> _)
        {
            string path = $"{GreyboxFolder}/PF_Module01_EbikeGreybox.prefab";

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "EbikeGreybox";
            root.transform.localScale = new Vector3(0.5f, 0.8f, 1.6f);

            foreach ((string id, string _, Vector3 offset) in VehiclePoints)
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = id;
                point.transform.SetParent(root.transform, false);
                point.transform.localPosition = offset;
                point.transform.localScale = Vector3.one * 0.18f;
                ConfigureEntity(point, $"module01.vehicle.{id}", id);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Añade la entidad con el objeto inactivo y la registra, como pide
        /// <c>WorldObjectTaskEntity.Configure</c>.
        /// </summary>
        private static void ConfigureEntity(GameObject go, string entityId, string itemId)
        {
            bool wasActive = go.activeSelf;
            go.SetActive(false);
            WorldObjectTaskEntity entity = go.AddComponent<WorldObjectTaskEntity>();
            entity.Configure(new EntityId(entityId), itemId);
            go.SetActive(wasActive);
        }

        private static void SetDisplayPrefab(SafetyItemSO item, GameObject prefab)
        {
            SerializedObject so = new SerializedObject(item);
            so.FindProperty("_displayPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
