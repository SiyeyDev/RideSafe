using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Pone las entidades de tarea sobre los objetos del garaje y crea las que falten.
    /// <para>
    /// Separado de <c>Module01SceneBuilder</c> y aplicable a la escena abierta: aquel
    /// <b>regenera</b> la escena desde la de lógica y se lleva por delante la presentación.
    /// Esto se puede repetir sin perder nada, que es lo que hace falta al cambiar un modelo,
    /// añadir un EPP o recibir una versión nueva del garaje.
    /// </para>
    /// </summary>
    public static class Module01SceneEntities
    {
        /// <summary>(nombre del objeto en la escena, zona, id del elemento del catálogo).</summary>
        private static readonly (string Obj, string Zone, string Item)[] Entities =
        {
            ("df_g_helmet_01",     "head",     "helmet_ok"),
            ("casco 1",            "head",     "helmet_damaged"),
            ("gafas",              "head",     "glasses"),
            ("botas",              "clothing", "shoes_ok"),
            ("chaleco refectivo",  "clothing", "reflective"),
            // La bufanda es "ropa o cordones sueltos": el riesgo que le faltaba a la zona de
            // ropa desde que se apago clothing_loose por no haber modelo. El v7 lo trajo.
            ("bufanda 01",         "clothing", "clothing_loose"),
            // Los pantalones: ajustado bien, ancho se engancha en la cadena, deportivo da igual.
            ("jean ajustado",      "clothing", "trousers_fitted"),
            ("pantalon ancho",     "clothing", "trousers_loose"),
            ("pant low",           "clothing", "trousers_tech"),
            ("guaya candado",      "load",     "cargo_secured"),
            ("MobilePhone_01",     "load",     "cargo_handheld")
        };


        [MenuItem("RideSafe/Módulo 1/Etiquetar entidades en la escena abierta")]
        public static void ApplyToOpenScene()
        {
            Apply();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>Idempotente: reutiliza las entidades que ya estén puestas.</summary>
        public static void Apply()
        {
            TagEntities();
            FillMissingEntities();
            RemoveOrphanSpawned();
        }

        /// <summary>Pone la entidad de tarea sobre cada objeto de EPP ya colocado por arte.</summary>
        private static void TagEntities()
        {
            foreach ((string objName, string zone, string item) in Entities)
            {
                GameObject go = Find(objName);
                if (go == null)
                {
                    Debug.LogWarning($"[Module01] No encuentro '{objName}' en la escena; {zone}.{item} queda sin entidad.");
                    continue;
                }

                // El collider se reajusta SIEMPRE, no solo cuando falta: al recibir un garaje
                // nuevo el objeto cambia de escala y de sitio, y un collider heredado del
                // garaje anterior deja el clic desplazado respecto a lo que se ve.
                BoxCollider box = Module01EditorComponents.Ensure<BoxCollider>(go);
                FitCollider(go, box);

                // Los colliders de los hijos se apagan: con dos volúmenes solapados gana el
                // que el raycast toque primero, que no siempre es el que lleva la entidad.
                foreach (Collider other in go.GetComponentsInChildren<Collider>(true))
                    if (other != box)
                        other.enabled = false;

                bool wasActive = go.activeSelf;
                go.SetActive(false);
                WorldObjectTaskEntity entity = Module01EditorComponents.Ensure<WorldObjectTaskEntity>(go);
                entity.Configure(new EntityId($"module01.{zone}.{item}"), item);
                go.SetActive(wasActive);
            }
        }

        /// <summary>
        /// Toda zona debe tener una entidad por elemento, o nunca se cierra y el módulo
        /// se queda esperando. Lo que el equipo de arte no colocó se crea aquí: el
        /// modelo del catálogo si lo tiene, y si no una primitiva gris.
        /// </summary>
        private static void FillMissingEntities()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            VehicleProfileSO vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");

            HashSet<string> present = new HashSet<string>();
            foreach (WorldObjectTaskEntity e in Object.FindObjectsByType<WorldObjectTaskEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                present.Add(e.Id.Value);

            GameObject spare = Find("Module01_Spawned") ?? new GameObject("Module01_Spawned");
            GameObject bike = Find("ebike");
            int created = 0;

            List<(ZoneSO Zone, bool OnVehicle)> all = new List<(ZoneSO, bool)>();
            if (catalog != null)
                foreach (ZoneSO z in catalog.Zones) if (z != null) all.Add((z, false));
            if (vehicle != null)
                foreach (ZoneSO z in vehicle.InspectionZones) if (z != null) all.Add((z, true));

            foreach ((ZoneSO zone, bool onVehicle) in all)
            {
                for (int i = 0; i < zone.Items.Count; i++)
                {
                    SafetyItemSO item = zone.Items[i];
                    if (item == null)
                        continue;

                    string id = $"module01.{zone.ZoneId}.{item.ItemId}";
                    if (present.Contains(id))
                        continue;

                    GameObject go;
                    if (item.DisplayPrefab != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(item.DisplayPrefab);
                        go.name = item.ItemId;
                    }
                    else
                    {
                        // Esfera SOLO como volumen de clic: el renderer nace apagado. Lo que
                        // se ve es el signo o el contorno de la pieza, nunca una bola blanca.
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = item.ItemId;
                        go.transform.localScale = Vector3.one * 0.15f;
                        foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>(true))
                            mr.enabled = false;
                    }

                    if (onVehicle && bike != null)
                    {
                        // Marcador sobre el vehiculo real, repartido a lo largo de el.
                        go.transform.SetParent(bike.transform, false);
                        go.transform.localPosition = new Vector3(0f, 0.4f + i * 0.25f, -0.4f + i * 0.4f);
                    }
                    else
                    {
                        go.transform.SetParent(spare.transform, false);
                        go.transform.position = new Vector3(i * 0.6f, 1.0f, 0f);
                    }

                    if (go.GetComponent<Collider>() == null)
                    {
                        BoxCollider box = go.AddComponent<BoxCollider>();
                        FitCollider(go, box);
                    }

                    bool wasActive = go.activeSelf;
                    go.SetActive(false);
                    WorldObjectTaskEntity entity =
                        Module01EditorComponents.Ensure<WorldObjectTaskEntity>(go);
                    entity.Configure(new EntityId(id), item.ItemId);
                    go.SetActive(wasActive);

                    present.Add(id);
                    created++;
                }
            }

            Debug.Log($"[Module01] Entidades creadas para completar las zonas: {created}.");
        }

        /// <summary>
        /// Grosor mínimo de un lado del collider, en metros de mundo. La guaya y el teléfono
        /// son láminas de 2 cm: de canto no se ven y el clic falla por un par de milímetros,
        /// que es la mitad del «se hace clic en la nada». Un objeto plano sigue viéndose
        /// plano; lo que se engorda es solo su volumen de acierto.
        /// </summary>
        private const float MinClickThickness = .06f;

        /// <summary>
        /// Ajusta el collider al volumen visible, que los FBX no traen uno.
        /// <para>
        /// Las ocho esquinas de cada renderer se pasan a espacio local <b>antes</b> de
        /// encerrarlas. Tomar <c>bounds.size</c> del mundo y metérselo a <c>box.size</c>,
        /// que es local, deja el collider descolocado en cuanto el objeto tiene escala —y
        /// los objetos que coloca el equipo de arte la tienen casi siempre.
        /// </para>
        /// </summary>
        private static void FitCollider(GameObject go, BoxCollider box)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds local = new Bounds();
            bool first = true;
            foreach (Renderer r in renderers)
            {
                Bounds b = r.bounds;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3(
                        (k & 1) == 0 ? -1 : 1,
                        (k & 2) == 0 ? -1 : 1,
                        (k & 4) == 0 ? -1 : 1));
                    Vector3 p = go.transform.InverseTransformPoint(corner);
                    if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                    else local.Encapsulate(p);
                }
            }
            if (first)
                return;

            box.center = local.center;

            // El mínimo se expresa en mundo, así que se traduce a local por la escala del
            // objeto: si no, en un objeto diminuto el engorde se lo come la escala.
            Vector3 scale = go.transform.lossyScale;
            Vector3 size = local.size;
            size.x = Mathf.Max(size.x, MinClickThickness / Mathf.Max(Mathf.Abs(scale.x), 1e-4f));
            size.y = Mathf.Max(size.y, MinClickThickness / Mathf.Max(Mathf.Abs(scale.y), 1e-4f));
            size.z = Mathf.Max(size.z, MinClickThickness / Mathf.Max(Mathf.Abs(scale.z), 1e-4f));
            box.size = size;
        }

        /// <summary>
        /// Borra las entidades que este script creó para elementos que ya no están en
        /// ninguna zona. Si no, apagar un elemento del catálogo deja su cubo gris en el
        /// garaje: clicable, mudo —no pertenece a ninguna zona— y confuso.
        /// </summary>
        private static void RemoveOrphanSpawned()
        {
            GameObject spawned = Find("Module01_Spawned");
            if (spawned == null)
                return;

            HashSet<string> wanted = new HashSet<string>();
            foreach (ZoneSO zone in AllZones())
                foreach (SafetyItemSO item in zone.Items)
                    if (item != null)
                        wanted.Add($"module01.{zone.ZoneId}.{item.ItemId}");

            int removed = 0;
            foreach (WorldObjectTaskEntity entity in
                     spawned.GetComponentsInChildren<WorldObjectTaskEntity>(true))
            {
                if (wanted.Contains(entity.Id.Value))
                    continue;
                Debug.Log($"[Module01] Borrada la entidad huerfana '{entity.Id.Value}'.");
                Undo.DestroyObjectImmediate(entity.gameObject);
                removed++;
            }

            if (removed > 0)
                Debug.Log($"[Module01] Entidades huerfanas borradas: {removed}.");
        }

        private static IEnumerable<ZoneSO> AllZones()
        {
            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(
                "Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            if (catalog != null)
                foreach (ZoneSO zone in catalog.Zones)
                    if (zone != null)
                        yield return zone;

            VehicleProfileSO vehicle = AssetDatabase.LoadAssetAtPath<VehicleProfileSO>(
                "Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");
            if (vehicle != null)
                foreach (ZoneSO zone in vehicle.InspectionZones)
                    if (zone != null)
                        yield return zone;
        }

        /// <summary>Nombre del primer objeto de arte de la zona, para colocar su ancla.</summary>
        public static string FirstObjectNameOfZone(string zone)
        {
            foreach ((string objName, string z, string _) in Entities)
                if (z == zone)
                    return objName;
            return null;
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
