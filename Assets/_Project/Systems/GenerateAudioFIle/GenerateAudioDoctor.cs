#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidades de reparacion para la tool GenerateAudio.
///
/// Reimportar: el importador de USS/UXML resuelve los url() en tiempo de import y hornea la
/// referencia al asset dentro del asset importado. Si esos archivos se importaron cuando sus
/// dependencias todavia no existian en el proyecto (tipico al copiar la tool desde otro
/// proyecto), la referencia horneada queda rota aunque el GUID del disco sea correcto: la fuente
/// sale como "Missing font asset reference" y la textura se dibuja con otro contenido.
/// Reimportar primero las dependencias y luego el USS/UXML rehace esas referencias.
///
/// Ventanas huerfanas: ver <see cref="TypingTextDisplayWindow"/>.
/// </summary>
public static class GenerateAudioDoctor
{
    // Dependencias primero, USS despues, UXML al final: el UXML depende del USS.
    private static readonly string[] k_assetGuids =
    {
        "a73ff8472bd40b94c8f3e7520a502e0f", // RadialGradien_256x256.png
        "4beb055f07aaff244873dec698d0363e", // Roboto-Bold.ttf
        "1ecff3374ede0df4c88c26e7a0f08ba3", // UnityDefaultRuntimeTheme.tss
        "c8c2573c2f87673489ab2bdf6fee8779", // SlideToggle.uss
        "09c87fe9d088fa243ae779c041ea59ab", // GenerateAudio_StyleSheet.uss
        "23835146230bb3f4d9fa730af7c137d2", // GenerateAudio_VisualTree.uxml
    };

    // OJO: esta entrada NO puede colgar de "Tools/Cachacos/GenerateAudio/...". Unity no admite a la
    // vez un comando hoja y un submenu con la misma ruta: el submenu gana y el comando desaparece,
    // que es justo el que abre la tool (GenerateAudioWIndow.ShowWindow).
    [MenuItem("Tools/Cachacos/Reimportar assets de GenerateAudio", priority = 20)]
    public static void ReimportToolAssets()
    {
        StringBuilder report = new StringBuilder("GenerateAudio: reimportando assets de la tool\n");
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in k_assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    report.AppendLine($"  NO ENCONTRADO guid {guid}");
                    continue;
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                report.AppendLine($"  OK {path}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Cachacos/Ventanas/Diagnostico", priority = 100)]
    public static void DiagnoseWindows()
    {
        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        List<EditorWindow> ghosts = new List<EditorWindow>();
        List<EditorWindow> rest = new List<EditorWindow>();
        foreach (EditorWindow window in windows)
        {
            if (window == null)
                continue;
            if (IsGhost(window))
                ghosts.Add(window);
            else
                rest.Add(window);
        }

        ContainerApi api = ResolveContainerApi();
        List<Object> orphanContainers = api == null ? new List<Object>() : FindOrphanContainers(api);
        List<GhostWindowSweeper.GhostWindow> workerGhosts = GhostWindowSweeper.Find();

        // La consola de Unity solo muestra la primera linea en la lista, asi que el resumen va
        // ahi: el detalle completo queda abajo, en el panel que se abre al hacer click.
        StringBuilder report = new StringBuilder();
        int total = ghosts.Count + orphanContainers.Count + workerGhosts.Count;
        if (total == 0)
            report.AppendLine($"Ventanas: {rest.Count} legitimas, 0 fantasmas. (click para ver el detalle)");
        else
            report.AppendLine($"Ventanas: {rest.Count} legitimas, FANTASMAS {total} ({ghosts.Count} EditorWindow + {orphanContainers.Count} ContainerWindow + {workerGhosts.Count} de import workers)");
        if (api == null)
            report.AppendLine("  AVISO: no se pudo acceder a la API interna de ContainerWindow en esta version de Unity.");

        report.AppendLine("-- EditorWindow huerfanas --");
        foreach (EditorWindow window in ghosts)
            report.AppendLine(Describe(window));
        report.AppendLine("-- ContainerWindow vacias de este proceso --");
        foreach (Object container in orphanContainers)
            report.AppendLine(Describe(api, container));
        report.AppendLine("-- ventanas en blanco de import workers (otros procesos) --");
        foreach (GhostWindowSweeper.GhostWindow ghost in workerGhosts)
            report.AppendLine($"  {ghost.processName} | PID {ghost.processId} | hWnd {ghost.handle}");
        report.AppendLine("-- resto --");
        foreach (EditorWindow window in rest)
            report.AppendLine(Describe(window));
        Debug.Log(report.ToString());
    }

    private static string Describe(EditorWindow window)
    {
        string title = window.titleContent == null ? "<null>" : $"'{window.titleContent.text}'";
        return $"  {window.GetType().FullName} | titulo {title} | docked {window.docked} | rect {window.position}";
    }

    #region ContainerWindow

    // Una ContainerWindow es la ventana nativa que hospeda los paneles. Si se queda sin ningun
    // EditorWindow adentro no se dibuja nada y la X no tiene a quien cerrar: es la ventana vacia
    // que no se deja cerrar. Como no es un EditorWindow, un barrido de EditorWindow no la ve.
    // Todo esto es API interna de UnityEditor, asi que se accede por reflexion y si algun miembro
    // no existe en esta version se aborta sin tocar nada.
    private const int k_showModeMainWindow = 4;

    private class ContainerApi
    {
        public System.Type type;
        public PropertyInfo showMode;
        public PropertyInfo title;
        public PropertyInfo position;
        public PropertyInfo rootView;
        public MethodInfo close;
        public FieldInfo editorWindowParent;
        public PropertyInfo viewWindow;
    }

    private static ContainerApi ResolveContainerApi()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Assembly editorAssembly = typeof(EditorWindow).Assembly;
        ContainerApi api = new ContainerApi { type = editorAssembly.GetType("UnityEditor.ContainerWindow") };
        System.Type viewType = editorAssembly.GetType("UnityEditor.View");
        if (api.type == null || viewType == null)
            return null;
        api.showMode = api.type.GetProperty("showMode", flags);
        api.title = api.type.GetProperty("title", flags);
        api.position = api.type.GetProperty("position", flags);
        api.rootView = api.type.GetProperty("rootView", flags);
        api.close = api.type.GetMethod("Close", flags, null, System.Type.EmptyTypes, null);
        api.editorWindowParent = typeof(EditorWindow).GetField("m_Parent", flags);
        api.viewWindow = viewType.GetProperty("window", flags);
        bool complete = api.showMode != null && api.title != null && api.position != null
                        && api.rootView != null && api.close != null
                        && api.editorWindowParent != null && api.viewWindow != null;
        return complete ? api : null;
    }

    /// <summary>
    /// Contenedores que no hospedan ningun EditorWindow vivo. Se excluye la ventana principal.
    /// </summary>
    private static List<Object> FindOrphanContainers(ContainerApi api)
    {
        HashSet<Object> occupied = new HashSet<Object>();
        foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (window == null)
                continue;
            object hostView = api.editorWindowParent.GetValue(window);
            if (hostView == null)
                continue;
            Object container = api.viewWindow.GetValue(hostView) as Object;
            if (container != null)
                occupied.Add(container);
        }

        List<Object> orphans = new List<Object>();
        foreach (Object container in Resources.FindObjectsOfTypeAll(api.type))
        {
            if (container == null || occupied.Contains(container))
                continue;
            if ((int)api.showMode.GetValue(container) == k_showModeMainWindow)
                continue;
            orphans.Add(container);
        }
        return orphans;
    }

    private static string Describe(ContainerApi api, Object container)
    {
        object rootView = api.rootView.GetValue(container);
        return $"  ContainerWindow | titulo '{api.title.GetValue(container)}' | showMode {api.showMode.GetValue(container)}" +
               $" | rect {api.position.GetValue(container)} | rootView {(rootView == null ? "null" : rootView.GetType().Name)}";
    }

    #endregion

    [MenuItem("Tools/Cachacos/Ventanas/Cerrar huerfanas", priority = 101)]
    public static void CloseGhostWindows()
    {
        List<EditorWindow> toClose = new List<EditorWindow>();
        foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (window != null && IsGhost(window))
                toClose.Add(window);
        }
        ContainerApi api = ResolveContainerApi();
        List<Object> orphanContainers = api == null ? new List<Object>() : FindOrphanContainers(api);
        List<GhostWindowSweeper.GhostWindow> workerGhosts = GhostWindowSweeper.Find();
        int workerSwept = GhostWindowSweeper.Sweep(workerGhosts);

        StringBuilder report = new StringBuilder();
        report.AppendLine($"Fantasmas cerrados: {toClose.Count + orphanContainers.Count + workerSwept} ({toClose.Count} EditorWindow + {orphanContainers.Count} ContainerWindow + {workerSwept}/{workerGhosts.Count} de import workers)");
        if (api == null)
            report.AppendLine("  AVISO: no se pudo acceder a la API interna de ContainerWindow en esta version de Unity.");
        foreach (EditorWindow window in toClose)
        {
            report.AppendLine($"  {window.GetType().FullName} | rect {window.position}");
            window.Close();
        }
        foreach (Object container in orphanContainers)
        {
            report.AppendLine(Describe(api, container));
            api.close.Invoke(container, null);
        }
        foreach (GhostWindowSweeper.GhostWindow ghost in workerGhosts)
            report.AppendLine($"  {ghost.processName} | PID {ghost.processId} | hWnd {ghost.handle}");
        if (workerGhosts.Count > workerSwept)
            report.AppendLine("  Quedaron ventanas de worker sin cerrar: usa 'Reiniciar import workers'.");
        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/Cachacos/Ventanas/Reiniciar import workers", priority = 102)]
    public static void RestartImportWorkers()
    {
        if (!EditorUtility.DisplayDialog("Reiniciar import workers",
                "Mata los procesos AssetImportWorker de ESTE editor. Unity los relanza cuando los necesite.\n\n" +
                "Es seguro si no hay un import en curso; si lo hay, Unity lo reintenta.",
                "Reiniciar", "Cancelar"))
            return;
        List<string> killed = GhostWindowSweeper.RestartImportWorkers();
        StringBuilder report = new StringBuilder($"Import workers reiniciados: {killed.Count}\n");
        foreach (string entry in killed)
            report.AppendLine($"  {entry}");
        Debug.Log(report.ToString());
    }

    /// <summary>
    /// Huerfana = uno de nuestros popups sin chrome, o cualquier ventana flotante sin titulo
    /// (una ventana de editor legitima siempre tiene titulo).
    /// </summary>
    private static bool IsGhost(EditorWindow window)
    {
        if (window is TypingTextDisplayWindow || window is Cachacos.Notification)
            return true;
        for (System.Type type = window.GetType(); type != null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Cachacos.BasePopUp<>))
                return true;
        }
        bool hasTitle = window.titleContent != null && !string.IsNullOrEmpty(window.titleContent.text);
        return !window.docked && !hasTitle;
    }
}
#endif
