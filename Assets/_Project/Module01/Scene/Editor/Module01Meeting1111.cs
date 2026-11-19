using System.Linq;
using RideSafe.Module01;
using RideSafe.Module01.Editor;
using RideSafe.Narration;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Lo de la reunión del 2026-11-11 que toca datos, prefab y escena, en el orden en que se apoya
/// lo uno en lo otro. Todo es idempotente. Para -executeMethod: <c>Module01Meeting1111.RunBatch</c>.
/// </summary>
public static class Module01Meeting1111
{
    /// <summary>Clips (sin extensión) de las dos locuciones nuevas, en Resources/Sounds/M1/&lt;idioma&gt;/.</summary>
    public const string WelcomeClip = "M01_WelcomeController";
    public const string SelectionClip = "M01_SelectionInstruction";
    /// <summary>La entrada a la sección del vehículo (clave M02_Welcome de la hoja del cliente).</summary>
    public const string VehicleIntroClip = "M02_Welcome";
    /// <summary>La mecánica con el control, tras la instrucción de cada sección (Charlie, 2026-11-18).</summary>
    public const string SelectionMechanicClip = "M01_SelectionMechanic";
    public const string InspectMechanicClip = "M02_InspectMechanic";
    /// <summary>La música de fondo (Charlie, 2026-11-19).</summary>
    public const string MusicClip = "Assets/_Project/Audio/Music/background.mp3";

    /// <summary>Las voces de los veredictos: (clip, evento que la lanza), en Resources/Sounds/M1/&lt;idioma&gt;/.</summary>
    private static (string Clip, System.Func<Module01Experience, UnityEngine.Events.UnityEvent> Event)[] Verdicts =>
        new (string, System.Func<Module01Experience, UnityEngine.Events.UnityEvent>)[]
        {
            ("M01_EppPassed", e => e.onEppPassed),
            ("M01_EppRetry", e => e.onEppFailed),
            ("M01_VehicleRetry", e => e.onVehicleFailed),
            ("M01_ModuleComplete", e => e.onModuleCompleted),
        };

    public static void RunBatch()
    {
        Module01MeetingTerms.Apply();
        Module01Greybox.RebuildVehicleData();
        RideSafe.UI.EditorTools.UIAchievementPanel.Apply();

        EditorSceneManager.OpenScene(Module01ClientListCleanup.ScenePath, OpenSceneMode.Single);
        Module01SceneEntities.Apply();
        Module01VehicleStaging.StageOpenScene();
        Module01VehicleWiring.WireOpenScene();
        PoseDanielDisplay();
        WireNarrationAndMusic();
        WireWelcomeTriggerHint();
        // La vitrina del M0 se quedó con el despiece anterior a da5b43c (ShowroomDisplayCopyTests).
        RideSafe.Module00.Editor.Module00ShowroomSetup.RefreshDisplayCopies();
        // Sincroniza el catálogo de presentación (nombres de la transmisión, fuera los P) y guarda.
        RideSafe.Module01.Editor.Module01SceneComposition.ApplyToOpenScene();

        VehiclePartRenderer.RenderAll();
        AssetDatabase.SaveAssets();
    }

    /// <summary>Solo las locuciones y la música, sin rehacer paneles ni poses. -executeMethod Module01Meeting1111.RunMusicBatch</summary>
    public static void RunMusicBatch()
    {
        EditorSceneManager.OpenScene(Module01ClientListCleanup.ScenePath, OpenSceneMode.Single);
        WireNarrationAndMusic();
        EditorSceneManager.SaveOpenScenes();
    }

    /// <summary>
    /// Daniel giró unos grados el display de la e-bike en su escena (ed44f2c, 2026-11-17). Esta
    /// escena no es la suya, así que se copia aquí la pose que dejó.
    /// </summary>
    public static void PoseDanielDisplay()
    {
        GameObject ebikeModel = AssetDatabase.LoadAssetAtPath<GameObject>(Module01Library.Models + "ebike.fbx");
        int posed = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name != "display" || t.parent == null || t.parent.name != "tenedor")
                continue;
            if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject) == null
                || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) != AssetDatabase.GetAssetPath(ebikeModel))
                continue;
            t.localPosition = new Vector3(-0.03197345f, 0.14738685f, -0.10090393f);
            t.localRotation = new Quaternion(-0.051541533f, 0.05143829f, 0.056885067f, 0.9957217f);
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            posed++;
        }
        Debug.Log($"[M1-BATCH] Display de la e-bike con la pose de Daniel: {posed} instancia(s).");
    }

    /// <summary>Raíz del mando de la bienvenida, dentro de la carpeta de la UI.</summary>
    public const string WelcomeHintRoot = "Bienvenida_Mando";

    /// <summary>
    /// El mando derecho con el gatillo pulsándose junto al panel de bienvenida, mientras la voz
    /// explica cómo se elige (cliente, 2026-11-11, 12:23). Reutiliza la vista 3D del tutorial del
    /// módulo 0 (<c>Module00TestSceneBuilder.EnsureHint3D</c>). Idempotente.
    /// </summary>
    public static void WireWelcomeTriggerHint()
    {
        Module01Experience experience = Object.FindAnyObjectByType<Module01Experience>(FindObjectsInactive.Include);
        RideSafe.UI.ModuleUI onboarding = new SerializedObject(experience).FindProperty("_onboarding").objectReferenceValue as RideSafe.UI.ModuleUI;
        GameObject folder = GameObject.Find("--- UI ---");
        if (onboarding == null || folder == null)
            throw new System.InvalidOperationException("Falta el onboarding o la carpeta «--- UI ---».");

        Transform root = folder.transform.Find(WelcomeHintRoot);
        if (root == null)
        {
            root = new GameObject(WelcomeHintRoot).transform;
            root.SetParent(folder.transform, false);
        }
        var report = new System.Text.StringBuilder();
        RideSafe.Tutorial.Presentation.ControllerHint3DView view =
            RideSafe.Module00.Editor.Module00TestSceneBuilder.EnsureHint3D(experience.gameObject.scene, root, report);
        // Más lejos y a un lado que en la escena de pruebas: a 0,6 m tapaba el logo de la bienvenida
        // (captura 2026-11-18).
        SerializedObject place = new SerializedObject(view);
        place.FindProperty("_distance").floatValue = 1.1f;
        // Abajo a la derecha, fuera del panel y de la barra de subtítulos: el panel de bienvenida
        // ocupa casi toda la vista, y a 15° de la mirada el mando le caía encima.
        place.FindProperty("_offset").vector2Value = new Vector2(0.6f, -0.3f);
        place.FindProperty("_maxYawFromGaze").floatValue = 30f;
        place.ApplyModifiedPropertiesWithoutUndo();
        RideSafe.Tutorial.Presentation.WelcomeTriggerHint hint = view.GetComponent<RideSafe.Tutorial.Presentation.WelcomeTriggerHint>();
        if (hint == null)
            hint = view.gameObject.AddComponent<RideSafe.Tutorial.Presentation.WelcomeTriggerHint>();
        hint.Configure(view, onboarding.Get<RectTransform>("Language"));
        EditorUtility.SetDirty(hint);

        // Se ve mientras suena cada instrucción con el control: la del módulo 0 al pulsar Iniciar, y
        // la mecánica al empezar cada sección del módulo 1. Se va al acabar la voz o al actuar.
        Unlisten(experience.onWelcome, hint);
        Unlisten(experience.onLeaveWelcome, hint);
        Listen(experience.onLeaveWelcome, hint.Show);
        Listen(experience.onOrientation, hint.Hide);
        Listen(experience.onSelectionStarted, hint.Show);
        Listen(experience.onVehicleSectionStarted, hint.Show);
        Listen(experience.onLearnerActs, hint.Hide);
        foreach (string voice in new[] { "Narration_WelcomeController", "Narration_SelectionInstruction", "Narration_VehicleIntro" })
        {
            NarrationPlayer player = Object.FindObjectsByType<NarrationPlayer>(FindObjectsInactive.Include).FirstOrDefault(n => n.name == voice);
            if (player != null)
                Listen(player.onFinished, hint.Hide);
        }
        EditorSceneManager.MarkSceneDirty(experience.gameObject.scene);
        Debug.Log("[M1-BATCH] Mando de la bienvenida: " + report.ToString().Replace("\n", " "));
    }

    [MenuItem("RideSafe/Módulo 1/Cablear locuciones nuevas y música (reunión 11-11)")]
    public static void WireNarrationAndMusic()
    {
        Module01Experience experience = Object.FindAnyObjectByType<Module01Experience>(FindObjectsInactive.Include);
        GameObject welcomeVoice = Object.FindObjectsByType<NarrationPlayer>(FindObjectsInactive.Include)
            .Select(n => n.gameObject).FirstOrDefault(g => g.name == "Narration");
        if (experience == null || welcomeVoice == null)
            throw new System.InvalidOperationException("La escena abierta no tiene Module01Experience o «Narration».");

        NarrationPlayer welcome = Copy(welcomeVoice, "Narration_WelcomeController", WelcomeClip);
        NarrationPlayer selection = Copy(welcomeVoice, "Narration_SelectionInstruction", SelectionClip, SelectionMechanicClip);
        NarrationPlayer vehicleIntro = Copy(welcomeVoice, "Narration_VehicleIntro", VehicleIntroClip, InspectMechanicClip);
        // Se enganchan solo con el audio ya generado en los dos idiomas: sin clip, NarrationPlayer
        // registra un error en cada reproducción. Volver a correr este menú tras generarlo.
        // Al pulsar Iniciar, que es cuando empieza la configuración (Charlie, 2026-11-18; reunión
        // 2026-11-11, 13:03: «apenas en el primer botón»). No se corta al entrar al garaje: las copias
        // pueden compartir la fuente de audio, y pararla callaría la bienvenida del módulo 1.
        Unlisten(experience.onWelcome, welcome);
        Unlisten(experience.onLeaveWelcome, welcome);
        Unlisten(experience.onOrientation, welcome);
        if (HasClip(WelcomeClip))
            Listen(experience.onLeaveWelcome, welcome.Play);
        else
            Debug.LogWarning("[M1-BATCH] Falta el audio " + WelcomeClip + ": la bienvenida queda sin voz.");
        if (HasClip(SelectionClip) && HasClip(SelectionMechanicClip))
            Listen(experience.onSelectionStarted, selection.Play);
        else
        {
            Unlisten(experience.onSelectionStarted, selection);
            Debug.LogWarning("[M1-BATCH] Falta el audio " + SelectionClip + ": la sección de EPP empieza sin voz.");
        }
        // Las dos instrucciones se cortan en cuanto el aprendiz actúa: su subtítulo se pisaba con el
        // nombre del podio y los botones (PlayMode, 2026-11-18).
        Listen(experience.onLearnerActs, selection.Stop);
        Listen(experience.onLearnerActs, vehicleIntro.Stop);
        foreach ((string clip, System.Func<Module01Experience, UnityEngine.Events.UnityEvent> evt) in Verdicts)
        {
            NarrationPlayer verdict = Copy(welcomeVoice, "Narration_" + clip, clip);
            // Se corta al pulsar el botón del panel, que es cuando el aprendiz sigue.
            Listen(experience.onLeaveAchievement, verdict.Stop);
            if (HasClip(clip))
                Listen(evt(experience), verdict.Play);
            else
            {
                Unlisten(evt(experience), verdict);
                Debug.LogWarning("[M1-BATCH] Falta el audio " + clip + ": el veredicto sale sin voz.");
            }
        }
        if (HasClip(VehicleIntroClip) && HasClip(InspectMechanicClip))
            Listen(experience.onVehicleSectionStarted, vehicleIntro.Play);
        else
        {
            Unlisten(experience.onVehicleSectionStarted, vehicleIntro);
            Debug.LogWarning("[M1-BATCH] Falta el audio " + VehicleIntroClip + ": la sección del vehículo empieza sin voz.");
        }

        // Música de fondo muy suave (cliente): baja con las voces y se pausa en los videos (BackgroundMusic).
        // Sin «??»: un componente de Unity que falta no es null para C#.
        GameObject music = GameObject.Find("BackgroundMusic");
        if (music == null) music = new GameObject("BackgroundMusic");
        // En la carpeta de las locuciones: en la raíz solo hay carpetas (SceneHierarchyLayoutTests).
        music.transform.SetParent(welcomeVoice.transform.parent, false);
        AudioSource source = music.GetComponent<AudioSource>();
        if (source == null) source = music.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = 0.08f;
        source.priority = 200;
        // Larga: en streaming y comprimida, que no ocupe memoria en el Quest.
        if (AssetImporter.GetAtPath(MusicClip) is AudioImporter importer)
        {
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            if (sample.loadType != AudioClipLoadType.Streaming || sample.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                sample.loadType = AudioClipLoadType.Streaming;
                sample.compressionFormat = AudioCompressionFormat.Vorbis;
                sample.quality = .5f;
                importer.defaultSampleSettings = sample;
                importer.SaveAndReimport();
            }
        }
        source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClip);
        if (source.clip == null) Debug.LogWarning("[M1-BATCH] Falta la música " + MusicClip + ": no suena nada de fondo.");
        if (music.GetComponent<BackgroundMusic>() == null) music.AddComponent<BackgroundMusic>();
        EditorSceneManager.MarkSceneDirty(experience.gameObject.scene);
    }

    /// <summary>Una copia de «Narration» (misma fuente de audio y subtítulos) que dice otras líneas, seguidas.</summary>
    private static NarrationPlayer Copy(GameObject template, string name, params string[] clips)
    {
        GameObject go = Object.FindObjectsByType<NarrationPlayer>(FindObjectsInactive.Include)
            .Select(n => n.gameObject).FirstOrDefault(g => g.name == name);
        if (go == null)
        {
            go = Object.Instantiate(template, template.transform.parent);
            go.name = name;
        }
        NarrationPlayer player = go.GetComponent<NarrationPlayer>();
        SerializedObject so = new SerializedObject(player);
        SerializedProperty lines = so.FindProperty("_lines");
        lines.ClearArray();
        lines.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
            lines.GetArrayElementAtIndex(i).stringValue = clips[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    private static void Unlisten(UnityEngine.Events.UnityEvent evt, Object target)
    {
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            if (evt.GetPersistentTarget(i) == target)
                UnityEventTools.RemovePersistentListener(evt, i);
    }

    private static bool HasClip(string clip) =>
        Resources.Load<AudioClip>("Sounds/M1/Español/" + clip) != null
        && Resources.Load<AudioClip>("Sounds/M1/English/" + clip) != null;

    private static void Listen(UnityEngine.Events.UnityEvent evt, UnityEngine.Events.UnityAction action)
    {
        for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            if (evt.GetPersistentTarget(i) == (Object)action.Target && evt.GetPersistentMethodName(i) == action.Method.Name)
                return;
        UnityEventTools.AddPersistentListener(evt, action);
    }
}
