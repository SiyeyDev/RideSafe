using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
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

        /// <summary>(id, clave de nombre, categoría) de los ocho elementos personales.</summary>
        private static readonly (string Id, string NameKey, SafetyItemCategory Category)[] Items =
        {
            ("helmet_ok",       "Module1/Item_HelmetOk",       SafetyItemCategory.Core),
            ("helmet_damaged",  "Module1/Item_HelmetDamaged",  SafetyItemCategory.Inappropriate),
            ("glasses",         "Module1/Item_Glasses",        SafetyItemCategory.Optional),
            ("shoes_ok",        "Module1/Item_ShoesOk",        SafetyItemCategory.Core),
            ("reflective",      "Module1/Item_Reflective",     SafetyItemCategory.ConditionDependent),
            ("cargo_secured",   "Module1/Item_CargoSecured",   SafetyItemCategory.Core),
            ("cargo_handheld",  "Module1/Item_CargoHandheld",  SafetyItemCategory.Inappropriate)
        };

        private static readonly (string ZoneId, string TitleKey, string[] ItemIds)[] Zones =
        {
            ("head",     "Module1/Zone_Head",     new[] { "helmet_ok", "helmet_damaged", "glasses" }),
            // clothing_loose fuera por decision de Charlie (2026-10-07): no hay modelo de ropa suelta.
            ("clothing", "Module1/Zone_Clothing", new[] { "shoes_ok", "reflective" }),
            ("load",     "Module1/Zone_Load",     new[] { "cargo_secured", "cargo_handheld" })
        };

        /// <summary>Puntos de inspección del e-bike: (id, clave, posición en el cubo).</summary>
        private static readonly (string Id, string NameKey, Vector3 Offset)[] VehiclePoints =
        {
            ("point_steering",  "Module1/Point_Steering",  new Vector3(0f, 0.55f, 0.35f)),
            ("point_brakes",    "Module1/Point_Brakes",    new Vector3(0.25f, 0.5f, 0.3f)),
            ("point_electrics", "Module1/Point_Electrics", new Vector3(0f, 0.1f, 0f)),
            ("point_wheels",    "Module1/Point_Wheels",    new Vector3(0f, -0.3f, 0.5f)),
            ("point_lights",    "Module1/Point_Lights",    new Vector3(0f, 0.3f, 0.6f))
        };

        private static readonly (string ZoneId, string TitleKey, string[] PointIds)[] VehicleZones =
        {
            ("cockpit",    "Module1/Zone_Cockpit",    new[] { "point_steering", "point_brakes", "point_electrics" }),
            ("wheels",     "Module1/Zone_Wheels",     new[] { "point_wheels" }),
            ("visibility", "Module1/Zone_Visibility", new[] { "point_lights" })
        };

        [MenuItem("RideSafe/Módulo 1/Crear datos de la primera pasada")]
        public static void CreateFirstPassData()
        {
            EnsureFolders();

            Dictionary<string, SafetyItemSO> items = CreateItems();
            List<ZoneSO> personalZones = CreateZones(Zones, items, "SO_Zone_");
            CreateCatalog(personalZones);

            GameObject greybox = CreateEbikeGreybox(items);
            Dictionary<string, SafetyItemSO> pointItems = CreateVehiclePointItems();
            List<ZoneSO> vehicleZones = CreateZones(VehicleZones, pointItems, "SO_ZoneVehicle_");
            CreateVehicleProfile(greybox, vehicleZones);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Module01] Datos de la primera pasada listos en " + DataFolder);
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

        private static Dictionary<string, SafetyItemSO> CreateItems()
        {
            Dictionary<string, SafetyItemSO> map = new Dictionary<string, SafetyItemSO>();
            foreach ((string id, string nameKey, SafetyItemCategory category) in Items)
            {
                SafetyItemSO asset = LoadOrCreate<SafetyItemSO>($"{ItemsFolder}/SO_Item_{id}.asset");
                GameObject prop = CreateProp(id);
                asset.ConfigureForTests(id, nameKey, nameKey + "_Risk", category);
                SetDisplayPrefab(asset, prop);
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
        /// Un perfil por vehículo, los dos sobre las mismas tres zonas: se revisa lo
        /// mismo en bici y en patinete —frenos, luces, llantas—, solo que en sitios
        /// distintos. Compartir las <see cref="ZoneSO"/> es lo que evita duplicar los
        /// cinco elementos y sus claves de localización.
        /// </summary>
        private static void CreateVehicleProfile(GameObject greybox, List<ZoneSO> zones)
        {
            Profile("Ebike", "ebike", Model("ebike.fbx") ?? greybox, zones);
            Profile("Escooter", "escooter", Model("scooter.fbx") ?? greybox, zones);
        }

        private static void Profile(string assetSuffix, string contextValue, GameObject model, List<ZoneSO> zones)
        {
            VehicleProfileSO profile =
                LoadOrCreate<VehicleProfileSO>($"{DataFolder}/SO_VehicleProfile_{assetSuffix}.asset");
            profile.Configure(contextValue, model, zones);
            EditorUtility.SetDirty(profile);
        }

        private static GameObject Model(string fileName) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Models/CITY/MODELADOS 3d/" + fileName);

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
