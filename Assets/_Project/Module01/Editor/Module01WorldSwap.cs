using RideSafe.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Cambia el mundo de la escena abierta por el del paquete de arte, conservando todo
    /// lo que no es mundo: la lógica, la presentación, las anclas y el jugador.
    /// <para>
    /// Existe porque <c>Module01SceneBuilder</c> <b>regenera</b> la escena desde la de
    /// lógica y se lleva por delante la presentación, y además busca el mundo por una
    /// lista de nombres escrita a mano que el garaje v6 dejó obsoleta: allí los 7 EPP
    /// pasaron a colgar de una raíz nueva <c>epps</c>, así que la lista vieja los dejaba
    /// atrás y la escena se quedaba sin elementos que inspeccionar.
    /// </para>
    /// <para>
    /// <b>Qué es mundo no se decide aquí por nombre.</b> Se decide preguntándole a la
    /// escena de arte: lo que es raíz allí es mundo aquí. Así un garaje v7 que vuelva a
    /// reagrupar sus objetos sigue funcionando sin tocar este archivo. Lo contrario
    /// —borrar «todo lo que no reconozco»— haría desaparecer en silencio cualquier objeto
    /// de lógica nuevo, que es el error que más caro sale en una escena.
    /// </para>
    /// </summary>
    public static class Module01WorldSwap
    {
        private const string ArtScene = "Assets/_Project/Models/CITY/scenas/Garage.unity";

        /// <summary>
        /// Donde vive el arte importado. Lo que está hecho solo de mallas de aquí es mundo,
        /// aunque el paquete nuevo ya no sepa cómo se llamaba.
        /// </summary>
        private const string ArtFolder = "Assets/_Project/Models/";
        private const string CatalogAsset = "Assets/_Project/Module01/Data/SO_Module01Catalog.asset";
        private const string SpawnedRoot = "Module01_Spawned";

        /// <summary>
        /// Las cámaras de la escena de arte no pasan: las pone la presentación, y la escena
        /// ya arrastró una vez el bug de dos cámaras de juego a la vez.
        /// </summary>
        private static readonly string[] CamerasToSkip = { "Camera", "Main Camera" };

        /// <summary>
        /// Lo que nunca es mundo, pase lo que pase. Es una red de seguridad explícita: si un
        /// objeto de lógica llegara a llamarse igual que uno del paquete de arte, sin esta
        /// lista desaparecería y costaría muchísimo atribuirlo.
        /// </summary>
        private static readonly string[] ProtectedRoots =
        {
            "Camera", "Main Camera", "DesktopMode", "Garage Floor Collider",
            "Module01", "Module01_Anchors", "Module01_Spawned", "Module01_Presentation",
            "Module01Controls", "Narration", "Tutorial", "XRPlayer",
            "PF_Subtitles", "PF_VideoScreen", "PF_Module00_UI", "PF_Module01_UI",
        };

        [MenuItem("RideSafe/Módulo 1/Cambiar el mundo por el del paquete de arte")]
        public static void SwapOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Module01] Sal de Play antes de cambiar el mundo.");
                return;
            }

            Scene work = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(work.path))
            {
                Debug.LogError("[Module01] La escena abierta no está guardada; guárdala antes.");
                return;
            }

            if (!System.IO.File.Exists(ArtScene))
            {
                Debug.LogError($"[Module01] No existe la escena de arte en {ArtScene}.");
                return;
            }

            // Sobre la propia escena de arte esto se comeria el mundo entero y lo volveria a
            // pegar: no es destructivo por suerte, es destructivo por accidente. Mejor no.
            if (work.path == ArtScene)
            {
                Debug.LogError(
                    "[Module01] La escena abierta ES la de arte. Abre la escena de trabajo antes.");
                return;
            }

            Scene art = EditorSceneManager.OpenScene(ArtScene, OpenSceneMode.Additive);

            // Se captura antes de mover nada: mover raíces muta la lista de raíces.
            List<GameObject> incoming = art.GetRootGameObjects()
                .Where(go => !CamerasToSkip.Contains(go.name))
                .ToList();

            // Los nombres se recogen de TODA la jerarquía de arte, no solo de sus raíces.
            // En v5 'luz grontal', 'luz roja 1' y los dos 'plataform' eran raíz; en v6 son
            // hijos de 'epps' y de 'FX'. Comparando solo contra raíces se quedaban en la
            // escena y se veían dos de cada uno.
            HashSet<string> worldNames = new HashSet<string>();
            foreach (GameObject go in art.GetRootGameObjects())
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    worldNames.Add(t.name);

            int removedRoots = RemoveOldWorld(work, worldNames);
            int removedStale = RemoveStaleCatalogInstances(work);

            foreach (GameObject go in incoming)
                EditorSceneManager.MoveGameObjectToScene(go, work);

            EditorSceneManager.CloseScene(art, true);

            ApplyGarageFinishes();

            // El mundo nuevo llega a la raíz: se archiva en sus carpetas.
            SceneHierarchyLayout.Apply(work);

            SceneManager.SetActiveScene(work);
            EditorSceneManager.MarkSceneDirty(work);

            Debug.Log(
                $"[Module01] Mundo cambiado: {removedRoots} raíces viejas fuera, " +
                $"{removedStale} copias de catálogo obsoletas fuera, {incoming.Count} raíces nuevas dentro. " +
                $"Saltadas por ser cámaras: {string.Join(", ", CamerasToSkip)}.");
        }

        /// <summary>
        /// Saca de la escena de trabajo las raíces cuyo nombre aparece en la jerarquía de la
        /// escena de arte. Lo que el arte no conoce se queda, porque es lógica.
        /// <para>
        /// Las raíces que no son ninguna de las dos cosas —ni protegidas, ni reconocidas como
        /// arte— se avisan en vez de decidirse en silencio. Un objeto huérfano a la vista es
        /// molesto; un objeto de lógica borrado sin avisar cuesta una tarde.
        /// </para>
        /// </summary>
        private static int RemoveOldWorld(Scene work, HashSet<string> worldNames)
        {
            int removed = 0;
            // Por objeto de primer nivel, no por raíz: el mundo vive en las carpetas del Hierarchy.
            foreach (GameObject go in SceneFolder.TopLevelObjects(work).ToList())
            {
                if (ProtectedRoots.Contains(go.name))
                    continue;

                bool porNombre = worldNames.Contains(go.name);
                bool porArte = MadeOfArt(go, out int pieces);

                if (!porNombre && !porArte)
                {
                    Debug.LogWarning(
                        $"[Module01] La raíz '{go.name}' no está protegida, el paquete de arte no la " +
                        "conoce y no está hecha solo de arte importado: se queda tal cual. Míralo, " +
                        "puede ser un resto del garaje viejo.");
                    continue;
                }

                Debug.Log($"[Module01] Fuera la raíz '{go.name}' " + (porNombre
                    ? "(el paquete nuevo la trae)."
                    : $"(no viene en el paquete, pero son {pieces} mallas de {ArtFolder} y nada más)."));

                Object.DestroyImmediate(go);
                removed++;
            }
            return removed;
        }

        /// <summary>Acabado de hormigón del suelo. El arte lo entrega en baldosa blanca.</summary>
        private const string FloorMaterial = "Assets/_Project/Models/CITY/Materials/SUELO.mat";
        private const string FloorObject = "piso";

        /// <summary>
        /// Devuelve al suelo del garaje su acabado de hormigón.
        /// <para>
        /// <b>La escena de arte trae el suelo con <c>suelo 1</c>, que es baldosa cerámica blanca
        /// brillante</b> —<c>Poliigon_TilesCeramicWhite</c>, tiling 2×2, smoothness 0.90—, y eso
        /// pinta el garaje de blanco con juntas de metro y medio. El acabado que usa el proyecto
        /// es <c>SUELO.mat</c>: <c>plaster_grey_04</c> a 6×6 y smoothness 0.20, hormigón
        /// continuo sin juntas.
        /// </para>
        /// <para>
        /// Se hace <b>aquí</b> y no a mano en la escena porque el cambio de mundo sustituye el
        /// objeto <c>piso</c> entero por el del arte, así que cualquier ajuste manual se pierde
        /// en la siguiente entrega. Pasó, y costó caro atribuirlo: ni la malla ni los materiales
        /// habían cambiado en git —lo que había cambiado era <b>cuál de los dos</b> llevaba
        /// puesto el suelo—, así que parecía «han traído otro garaje».
        /// </para>
        /// <para>
        /// Si algún día el arte entrega el suelo ya con el acabado bueno, esto se vuelve
        /// inofensivo: asigna lo que ya estaba.
        /// </para>
        /// </summary>
        private static void ApplyGarageFinishes()
        {
            Material floor = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterial);
            if (floor == null)
            {
                Debug.LogWarning(
                    $"[Module01] No está {FloorMaterial}; el suelo se queda con el acabado que " +
                    "traiga el arte, que es baldosa blanca brillante.");
                return;
            }

            int painted = 0;
            foreach (MeshRenderer renderer in Object
                         .FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(r => r.name == FloorObject))
            {
                Undo.RecordObject(renderer, "Acabado del suelo");
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => floor).ToArray();
                EditorUtility.SetDirty(renderer);
                painted++;
            }

            if (painted == 0)
                Debug.LogWarning($"[Module01] El mundo nuevo no trae ningún '{FloorObject}'.");
            else
                Debug.Log($"[Module01] Suelo con acabado de hormigón ({floor.name}) en {painted} objeto(s).");
        }

        /// <summary>
        /// ¿Es esta raíz puro arte importado?
        /// <para>
        /// Hace falta porque el nombre no basta. El v7 <b>reemplazó</b> la raíz
        /// <c>enviroment</c> por tres prefabs nuevos y se llevó los tres maniquíes en pose T,
        /// así que con la regla del nombre esas raíces no se reconocían, se quedaban puestas y
        /// la escena acababa con dos garajes encima. Y poner una lista de «lo que el v7 retiró»
        /// solo aplaza el problema hasta el v8.
        /// </para>
        /// <para>
        /// Se miran las mallas y no los componentes: la lógica del módulo se <b>engancha</b> a
        /// los objetos de arte —<c>botas</c> y <c>df_g_helmet_01</c> llevan su entidad dentro de
        /// <c>enviroment</c>—, así que buscar scripts nuestros protegería justo lo que hay que
        /// cambiar. Las entidades se vuelven a poner después, con <c>Module01SceneEntities</c>.
        /// </para>
        /// <para>
        /// Una raíz sin mallas no se considera arte: ni se afirma ni se niega, se avisa. Es el
        /// caso de <c>Box Volume</c> y de un <c>GameObject</c> vacío que arrastra la escena.
        /// </para>
        /// </summary>
        private static bool MadeOfArt(GameObject root, out int pieces)
        {
            pieces = 0;
            int foreign = 0;

            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                Classify(mf.sharedMesh, ref pieces, ref foreign);
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Classify(smr.sharedMesh, ref pieces, ref foreign);

            return pieces > 0 && foreign == 0;
        }

        private static void Classify(Mesh mesh, ref int pieces, ref int foreign)
        {
            if (mesh == null)
                return;
            if (AssetDatabase.GetAssetPath(mesh).StartsWith(ArtFolder))
                pieces++;
            else
                foreign++;
        }

        /// <summary>
        /// Borra los objetos del catálogo que <c>ArrangeStations</c> había reparentado a
        /// <c>Module01_Spawned</c>.
        /// <para>
        /// Son los mismos EPP que el paquete de arte vuelve a traer, así que si se quedan
        /// se ven dos de cada uno, los dos con collider: el aprendiz haría clic en el que
        /// no lleva entidad y no pasaría nada. Los puntos del vehículo
        /// (<c>point_*</c>) <b>no</b> se tocan: no vienen del arte, los crea el greybox.
        /// </para>
        /// </summary>
        private static int RemoveStaleCatalogInstances(Scene work)
        {
            GameObject spawned = SceneFolder.TopLevelObjects(work).FirstOrDefault(go => go.name == SpawnedRoot);
            if (spawned == null)
                return 0;

            Module01CatalogSO catalog = AssetDatabase.LoadAssetAtPath<Module01CatalogSO>(CatalogAsset);
            if (catalog == null)
            {
                Debug.LogWarning(
                    $"[Module01] Sin catálogo en {CatalogAsset}: no se limpian las copias viejas de EPP. " +
                    "Revisa a mano que no haya dos de cada objeto.");
                return 0;
            }

            HashSet<string> catalogIds = new HashSet<string>(
                catalog.AllItems.Where(i => i != null).Select(i => i.ItemId));

            List<GameObject> doomed = new List<GameObject>();
            foreach (Transform child in spawned.transform)
            {
                WorldObjectTaskEntity entity = child.GetComponent<WorldObjectTaskEntity>();
                if (entity != null && catalogIds.Contains(entity.ItemId))
                    doomed.Add(child.gameObject);
            }

            foreach (GameObject go in doomed)
            {
                Debug.Log($"[Module01] Fuera la copia vieja '{go.name}' de {SpawnedRoot}.");
                Object.DestroyImmediate(go);
            }

            return doomed.Count;
        }
    }
}
