#if UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Barre las ventanas en blanco que dejan los AssetImportWorker.
///
/// Contexto: el editor lanza procesos Unity.exe auxiliares con -batchMode para importar en
/// paralelo. Esos procesos ejecutan los [InitializeOnLoad] igual que el editor, asi que si algun
/// script llama GetWindow() sin condicion, ahi se crea una ContainerWindow nativa visible
/// (clase UnityContainerWndClass) que nadie cierra: sale en la barra de tareas, vacia, y la X no
/// responde porque el proceso no bombea mensajes.
///
/// Como viven en OTRO proceso, ni Resources.FindObjectsOfTypeAll ni EditorWindow.Close las
/// alcanzan. Hay que ir por Win32. Por el mismo motivo DestroyWindow no sirve (solo funciona
/// desde el hilo dueno), asi que se intenta WM_CLOSE y a lo que sobreviva se le hace ShowWindow
/// (SW_HIDE), que si funciona entre procesos y quita la ventana de la barra de tareas.
///
/// Filtro de seguridad: solo se tocan procesos cuyo PID padre es ESTE editor. Con dos editores
/// Unity abiertos a la vez, los workers del otro proyecto quedan intactos.
/// </summary>
public static class GhostWindowSweeper
{
    public struct GhostWindow
    {
        public System.IntPtr handle;
        public int processId;
        public string processName;
    }

    public const string k_containerClass = "UnityContainerWndClass";

#if UNITY_EDITOR_WIN
    private const uint TH32CS_SNAPPROCESS = 0x00000002;
    private const uint WM_CLOSE = 0x0010;
    private const int SW_HIDE = 0;

    private delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, System.IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(System.IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
    private static extern int GetWindowText(System.IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassName(System.IntPtr hWnd, StringBuilder name, int count);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(System.IntPtr hWnd, int cmd);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(System.IntPtr hWnd, uint msg, System.IntPtr w, System.IntPtr l);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern System.IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32FirstW")]
    private static extern bool Process32First(System.IntPtr snapshot, ref PROCESSENTRY32 entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "Process32NextW")]
    private static extern bool Process32Next(System.IntPtr snapshot, ref PROCESSENTRY32 entry);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(System.IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public System.IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    /// <summary>PIDs de procesos cuyo padre es este editor, con su nombre de ejecutable.</summary>
    private static Dictionary<int, string> FindChildProcesses()
    {
        Dictionary<int, string> children = new Dictionary<int, string>();
        int ownPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        System.IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == System.IntPtr.Zero || snapshot == new System.IntPtr(-1))
            return children;
        try
        {
            PROCESSENTRY32 entry = new PROCESSENTRY32();
            entry.dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
            if (!Process32First(snapshot, ref entry))
                return children;
            do
            {
                if (entry.th32ParentProcessID == ownPid)
                    children[(int)entry.th32ProcessID] = entry.szExeFile;
            }
            while (Process32Next(snapshot, ref entry));
        }
        finally
        {
            CloseHandle(snapshot);
        }
        return children;
    }
#endif

    /// <summary>
    /// Ventanas ContainerWindow visibles y sin titulo que pertenecen a procesos hijos de este
    /// editor. Una ContainerWindow legitima siempre tiene titulo.
    /// </summary>
    public static List<GhostWindow> Find()
    {
        List<GhostWindow> ghosts = new List<GhostWindow>();
#if UNITY_EDITOR_WIN
        Dictionary<int, string> children = FindChildProcesses();
        if (children.Count == 0)
            return ghosts;

        StringBuilder buffer = new StringBuilder(512);
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;
            buffer.Length = 0;
            GetClassName(hWnd, buffer, buffer.Capacity);
            if (buffer.ToString() != k_containerClass)
                return true;
            buffer.Length = 0;
            GetWindowText(hWnd, buffer, buffer.Capacity);
            if (buffer.Length != 0)
                return true;
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (!children.TryGetValue((int)pid, out string exe))
                return true;
            ghosts.Add(new GhostWindow { handle = hWnd, processId = (int)pid, processName = exe });
            return true;
        }, System.IntPtr.Zero);
#endif
        return ghosts;
    }

    /// <summary>
    /// Intenta WM_CLOSE y esconde lo que sobreviva. Devuelve cuantas dejaron de ser visibles.
    /// </summary>
    public static int Sweep(List<GhostWindow> ghosts)
    {
        int swept = 0;
#if UNITY_EDITOR_WIN
        foreach (GhostWindow ghost in ghosts)
            PostMessage(ghost.handle, WM_CLOSE, System.IntPtr.Zero, System.IntPtr.Zero);
        // El worker corre en -batchMode y puede no bombear mensajes, asi que WM_CLOSE a menudo
        // se queda en la cola. Se le da un respiro y lo que siga visible se esconde.
        System.Threading.Thread.Sleep(200);
        foreach (GhostWindow ghost in ghosts)
        {
            if (IsWindowVisible(ghost.handle))
                ShowWindow(ghost.handle, SW_HIDE);
            if (!IsWindowVisible(ghost.handle))
                swept++;
        }
#endif
        return swept;
    }

    /// <summary>
    /// Mata los import workers hijos de este editor. Unity los relanza solo cuando los necesita.
    /// Es la opcion contundente: se lleva las ventanas y el proceso que las creo.
    /// </summary>
    public static List<string> RestartImportWorkers()
    {
        List<string> killed = new List<string>();
#if UNITY_EDITOR_WIN
        foreach (KeyValuePair<int, string> child in FindChildProcesses())
        {
            // Coincidencia EXACTA con Unity.exe. El editor tiene muchos hijos cuyo nombre
            // empieza por "Unity" y que NO se pueden matar: UnityPackageManager.exe,
            // UnityShaderCompiler.exe, Unity.Licensing.Client.exe, UnityCrashHandler64.exe,
            // Unity.ILPP.Runner.exe, UnityAutoQuitter.exe. Los unicos hijos que son literalmente
            // "Unity.exe" son los AssetImportWorker.
            if (!string.Equals(child.Value, "Unity.exe", System.StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(child.Key);
                process.Kill();
                killed.Add($"{child.Value} (PID {child.Key})");
            }
            catch (System.Exception e)
            {
                killed.Add($"{child.Value} (PID {child.Key}) FALLO: {e.Message}");
            }
        }
#endif
        return killed;
    }
}
#endif
