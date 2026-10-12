using System.Linq;
using RideSafe.UI;
using RideSafe.UI.EditorTools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Video;
// UnityEngine.UIElements es un namespace y taparia la clase del kit, igual que
// UnityEngine.EntityId tapa la de TaskSequence. Alias explicito para desambiguar.
using UIElements = RideSafe.UI.EditorTools.UIElements;

namespace RideSafe.Module01.Editor
{
    /// <summary>
    /// Adds the presentation to the current v5 scene without rebuilding the art.
    /// <para>
    /// Vive fuera de <c>Module01/Editor</c> a proposito: ese asmdef no puede referenciar
    /// Assembly-CSharp-Editor, que es donde vive el kit de UI (porque <c>UIModules</c> es
    /// una clase parcial y una de sus partes usa I2.Loc, que no tiene asmdef). Desde aqui
    /// el kit si se alcanza, y los asmdef de runtime llegan solos por autoReferenced.
    /// </para>
    /// </summary>
    public static class Module01PresentationBuilder
    {
        private static readonly string[] Zones = { "head", "clothing", "load", "cockpit", "wheels", "visibility" };
        private static Transform _camera;

        /// <summary>Escala de canvas con la que esta dibujado el storyboard del kit.</summary>
        private const float k_KitScale = 0.002f;
        private static UIBuilderKit _kit;
        private static UIElements _ui;
        [MenuItem("RideSafe/Módulo 1/Conectar presentacion en escena actual")]
        public static void Build()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Exit Play before building.");
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.EndsWith("_v5")) throw new System.InvalidOperationException("Open RideSafe_Garaje_v5 first.");
            // Two overlapping copies of a UI root look like a dead button: Show() hides one
            // panel and the twin stays lit on top of it. Find() would pick one in silence.
            RequireUnique("PF_Module00_UI"); RequireUnique("PF_Module01_UI"); RequireUnique("Module01");
            _kit=RideSafeUIPrefabBuilder.CreateKit(); _ui=new UIElements(_kit);
            foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) if (g.name == "XRPlayer" || g.name == "Tutorial") g.SetActive(false);
            _camera = Find("Desktop Camera").transform;
            _camera.position = new Vector3(0,1.45f,9.3f);
            _camera.rotation = Quaternion.Euler(0,180,0);
            var cam = _camera.GetComponent<Camera>(); cam.fieldOfView = 60; cam.nearClipPlane = .05f;
            cam.tag = "MainCamera";
            BuildUiCamera(cam);
            var root = Find("Module01");
            if(root.GetComponent<RideSafe.TaskSequence.TaskSequenceService>()==null) root.AddComponent<RideSafe.TaskSequence.TaskSequenceService>();
            var inspector = root.GetComponent<ItemInspector>(); var tour = root.GetComponent<GuidedTour>();
            var director = root.GetComponent<Module01Director>();
            var binding = root.GetComponent<Module01Binding>();
            var ui = Find("PF_Module01_UI").GetComponent<ModuleUI>();
            var onboarding = Find("PF_Module00_UI").GetComponent<ModuleUI>();
            PlaceCanvas(ui.GetComponent<Canvas>()); PlaceCanvas(onboarding.GetComponent<Canvas>());
            // Los dos raices encendidos, y todos los paneles del modulo 1 presentes: quien decide
            // que se ve es Module01Experience en runtime. Si alguien deja uno apagado en la escena,
            // el modulo arranca mudo y cuesta mucho atribuirlo. El montador lo normaliza siempre.
            ui.gameObject.SetActive(true); onboarding.gameObject.SetActive(true);
            foreach (Transform panel in ui.transform) panel.gameObject.SetActive(true);
            var exp = root.GetComponent<Module01Experience>(); if(exp==null) exp=root.AddComponent<Module01Experience>();
            Set(exp,"_director",director); Set(exp,"_binding",binding); Set(exp,"_tour",tour); Set(exp,"_inspector",inspector);
            Set(exp,"_runner",root.GetComponent<ZoneRunner>()); Set(exp,"_camera",_camera); Set(exp,"_onboarding",onboarding);
            var door = Find("Door_Garage_Brown_5x").transform;
            foreach (var part in door.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(part.gameObject, 0);
            Set(exp,"_door",door);
            Set(exp,"_subtitles",Object.FindAnyObjectByType<SubtitleView>(FindObjectsInactive.Include));
            Set(tour,"_rig",_camera); Set(tour,"_anchorRoot",Find("Module01_Anchors").transform);
            Bool(director,"_manualProgress",true);

            Replace("Module01Controls");
            Replace("Module01_Presentation");
            var presentation = new GameObject("Module01_Presentation").transform;
            presentation.SetParent(_camera,false);
            var fade = Canvas("Fade", presentation, RenderMode.ScreenSpaceOverlay);
            fade.sortingOrder = 32000;
            var group = fade.gameObject.AddComponent<CanvasGroup>(); group.alpha=0; group.blocksRaycasts=false;
            var black = Rect("Black", fade.transform, Vector2.zero, new Vector2(100,100));
            black.anchorMin=Vector2.zero; black.anchorMax=Vector2.one; black.offsetMin=black.offsetMax=Vector2.zero;
            black.gameObject.AddComponent<Image>().color=Color.black;
            Set(tour,"_fade",group);

            // Efecto cine: un panel negro en la capa UI, MAS LEJOS que los paneles de interfaz.
            // La camara de overlay limpia profundidad, asi que lo que pinta tapa el garaje
            // entero sin importar la distancia; y al estar detras de los paneles, el video y
            // sus botones siguen encima. No vale un ScreenSpaceOverlay: taparia el video
            // tambien, y no funciona en VR.
            var dimCanvas = Canvas("TheaterDim",presentation,RenderMode.WorldSpace);
            dimCanvas.transform.localPosition = new Vector3(0,0,6f);
            dimCanvas.transform.localScale = Vector3.one*.01f;
            ((RectTransform)dimCanvas.transform).sizeDelta = new Vector2(4000,4000);
            var dimGroup = dimCanvas.gameObject.AddComponent<CanvasGroup>();
            dimGroup.alpha=0; dimGroup.blocksRaycasts=false; dimGroup.interactable=false;
            var dimRect = Rect("Black", dimCanvas.transform, Vector2.zero, new Vector2(100,100));
            dimRect.anchorMin=Vector2.zero; dimRect.anchorMax=Vector2.one; dimRect.offsetMin=dimRect.offsetMax=Vector2.zero;
            var dimImage = dimRect.gameObject.AddComponent<Image>(); dimImage.color=Color.black; dimImage.raycastTarget=false;
            int uiLayer = LayerMask.NameToLayer("UI");
            foreach(var t in dimCanvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=uiLayer;
            var theater = root.GetComponent<TheaterMode>();
            if(theater==null) theater=root.AddComponent<TheaterMode>();
            Set(theater,"_dim",dimGroup);
            Set(exp,"_theater",theater);
            dimCanvas.gameObject.SetActive(false);

            var inspection = Canvas("ItemInspectorCanvas",presentation,RenderMode.WorldSpace);
            inspection.transform.localPosition = new Vector3(-.16f,-.05f,1.15f);
            inspection.transform.localScale = Vector3.one*.0015f;
            ((RectTransform)inspection.transform).sizeDelta=new Vector2(700,500);
            var stage = new GameObject("ItemStage").transform; stage.SetParent(presentation,false); stage.localPosition=new Vector3(-.16f,-.02f,1.12f);
            var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder); pedestal.name="InspectorPedestal"; pedestal.transform.SetParent(presentation,false); pedestal.transform.localPosition=new Vector3(-.16f,-.23f,1.12f); // 26 cm, no 38: cubre de sobra la huella de un objeto normalizado a 32 cm en su lado mayor,
            // y cada cm de radio adelanta el borde cercano hacia la camara y empuja el popup hacia abajo.
            pedestal.transform.localScale=new Vector3(.26f,.02f,.26f); Object.DestroyImmediate(pedestal.GetComponent<Collider>());
            // El popup vive en un canvas a 0.0015 y el kit esta dibujado para 0.002, asi que
            // sus px se agrandan en vez de escalar el canvas: asi el ItemStage y el pedestal,
            // que estan en coords de mundo relativas a este canvas, no se mueven.
            float px=k_KitScale/inspection.transform.localScale.x;
            // Mismo problema que abajo, pero por arriba: el modelo se normaliza a DisplaySize en
            // su lado mayor y se centra en el stage, asi que su esquina superior cercana se
            // adelanta media talla hacia la camara y en pantalla sube mas de lo que dice su y.
            // El nombre se cuelga por ENCIMA de esa silueta, no de una altura escrita a mano.
            const float nameH=80f, nameMargin=20f;
            float halfItem=ItemInspector.DisplaySize*.5f;
            float itemOnCanvas=(stage.localPosition.y+halfItem)/(stage.localPosition.z-halfItem)*inspection.transform.localPosition.z;
            float nameBottom=(itemOnCanvas-inspection.transform.localPosition.y)/inspection.transform.localScale.x;
            var name=Text("ItemName",inspection.transform,new Vector2(0,nameBottom+nameH*.5f+nameMargin),new Vector2(580,nameH),"",_kit.H1OnDark.With(28f*px));
            // El pedestal es un disco, no un plano: su borde cercano se adelanta media anchura
            // hacia la camara, asi que en pantalla cuelga MUCHO mas abajo que su centro y se
            // comia el techo del popup, dibujandose encima por estar mas cerca. Comparar
            // alturas en el mundo no lo detecta. El popup se cuelga debajo de esa silueta
            // proyectada, no de una altura a ojo, para que siga cuadrando si cambia el disco.
            // 212 y no 185: medido, la pregunta en espanol -"¿Quieres anadir este elemento a tu
            // preparacion?"- parte en dos lineas y pide 87.4 de alto, pero su caja eran 70, asi
            // que la segunda linea se desbordaba hacia los botones. El panel crece hacia ABAJO
            // -su borde superior sale de confirmTop menos el margen, que no depende del alto-
            // asi que el pedestal sigue despejado igual.
            const float confirmH=212f, margin=25f;
            float rimZ=pedestal.transform.localPosition.z-pedestal.transform.localScale.z*.5f;
            float rimY=pedestal.transform.localPosition.y-pedestal.transform.localScale.y;
            float rimOnCanvas=rimY/rimZ*inspection.transform.localPosition.z;
            float confirmTop=(rimOnCanvas-inspection.transform.localPosition.y)/inspection.transform.localScale.x;
            var confirm=Panel("Confirmation",inspection.transform,new Vector2(0,confirmTop-confirmH*.5f-margin),new Vector2(620,confirmH),"Panel_Modal_Dark",2.4f/px);
            var question=Text("Question",confirm,new Vector2(0,54),new Vector2(580,95),"",_kit.H1OnDark.With(24f*px));
            var yes=Button("AcceptButton",confirm,new Vector2(-125,-55),new Vector2(230,50f*px),"Module1/Yes",true,17f*px);
            var no=Button("DeclineButton",confirm,new Vector2(125,-55),new Vector2(230,50f*px),"Module1/No",false,17f*px);
            Set(inspector,"_stage",stage); Set(inspector,"_nameLabel",name); Set(inspector,"_confirmRoot",confirm.gameObject); Set(inspector,"_questionLabel",question); Set(inspector,"_acceptButton",yes); Set(inspector,"_declineButton",no); Set(inspector,"_pedestal",pedestal);

            // Auxiliary controls live outside the generated prefab and are resolved at runtime.
            var hud=Canvas("Module01Controls",ui.transform,RenderMode.WorldSpace);
            hud.transform.localPosition=Vector3.zero; hud.transform.localScale=Vector3.one;
            ((RectTransform)hud.transform).sizeDelta=new Vector2(1002,556);
            // El HUD ya cuelga de PF_Module01_UI con marco 1002x556 y escala heredada 0.002,
            // o sea el storyboard del kit: aqui las medidas entran directas, sin convertir.
            // La navegacion se corre a la izquierda para dejarle la derecha al checklist, que
            // la reunion mando apartar del centro. Calculado en angulos, que es lo que decide
            // si dos cosas se tapan en pantalla cuando estan a distinta profundidad: el
            // checklist flota a 1.6 m y 24 grados a la derecha, asi que ocupa de 7.8 a 33.5
            // grados; la navegacion, a 1.8 m, ocupa de -31 a -1.3. Disjuntos.
            // Fuera de la silueta del vehiculo, no solo mas abajo.
            //
            // Bajarla no servia: el vehiculo ocupa el centro de la pantalla de arriba abajo, asi
            // que cada vez que se bajaba la barra, el siguiente punto de inspeccion mas bajo
            // quedaba debajo de un boton y el clic se lo comia el boton -primero la bateria,
            // luego las llantas-. Hay que salir por el LADO.
            //
            // El vehiculo, de costado y a 2.7 m, abarca de -20 a +20 grados. La barra acaba en
            // -20 (borde derecho en x=-328 unidades) y arranca en -39.7, dentro de los 45.7 que
            // da el campo horizontal. El checklist vive al otro lado, de +4 a +33.
            var nav=Rect("Navigation",hud.transform,new Vector2(-538,-330),new Vector2(420,55));
            Button("PreviousZoneButton",nav,new Vector2(-105,0),new Vector2(200,48),"Module1/Previous",false,16f);
            Button("NextZoneButton",nav,new Vector2(105,0),new Vector2(200,48),"Module1/Next",true,16f);
            Set(exp,"_navigation",nav.gameObject);
            var instructions=Panel("StationInstruction",hud.transform,new Vector2(-110,220),new Vector2(610,85),"Panel_Modal_Dark",2.4f);
            Set(exp,"_instruction",instructions.gameObject);
            Set(exp,"_instructionText",Text("Instruction",instructions,Vector2.zero,new Vector2(585,80),"",_kit.BodyOnDark.With(18f)));
            var decision=Panel("ReadinessDecision",hud.transform,Vector2.zero,new Vector2(760,290),"Panel_Modal_Light",2.4f);
            Text("Title",decision,new Vector2(0,85),new Vector2(720,70),"Module1/Decision_Question",_kit.H1.With(27f));
            // Verde / ambar / rojo, como pidio la reunion.
            //
            // OJO, esto REVIERTE una decision anterior que esta escrita en Docs/memory.md: los
            // tres se dejaron secundarios y grises a proposito, porque en una evaluacion un
            // boton destacado insinua la respuesta antes de que el aprendiz lea la pregunta.
            // La reunion es posterior y manda la reunion, pero el motivo de entonces sigue
            // siendo valido y conviene mirar en el visor si el verde arrastra la eleccion.
            Tint(Button("RoadReadyButton",decision,new Vector2(-245,-35),new Vector2(230,56),"Module1/RoadReady",false,16f),
                 new Color32(0x74,0xC6,0x8A,0xFF));
            Tint(Button("NeedsServiceButton",decision,new Vector2(0,-35),new Vector2(230,56),"Module1/NeedsService",false,16f),
                 new Color32(0xF2,0xC4,0x5A,0xFF));
            Tint(Button("DoNotRideButton",decision,new Vector2(245,-35),new Vector2(230,56),"Module1/DoNotRide",false,16f),
                 new Color32(0xE8,0x7E,0x72,0xFF));
            Set(exp,"_decision",decision.gameObject);

            // Barra de salida del reporte final: solo los dos botones.
            //
            // El rango y su recomendacion NO van aqui: van al titulo y al subtitulo del propio
            // panel "Mi Eleccion", que es lo que pidio la reunion al decir "unificar en un unico
            // panel". Y ademas no caben: medido en unidades de canvas, el marco va de -278 a
            // +278, las columnas bajan hasta -122 y la leyenda ocupa -195..-136, asi que por
            // debajo solo quedan unos 80. Un panel con titulo, cuerpo y dos botones pedia 210 y
            // se comia la leyenda entera.
            //
            // Va por DEBAJO del marco de 1002x556, y eso es correcto: ese marco es la rejilla de
            // diseno, no el limite de lo que se ve. A 1.8 m y con 60 de campo vertical la vista
            // abarca 2.08 m, y el marco solo 1.11: ocupa el 53 %. Queda casi medio metro libre
            // por abajo, que es lo que necesita la columna unica de "Mi Eleccion" para sus doce
            // filas sin apretarlas.
            //
            // Comprobado el angulo: el borde inferior de la barra cae a 23.6 grados por debajo
            // del centro, dentro de los 30 que da el campo visual.
            var result=Panel("FinalResult",hud.transform,new Vector2(0,-358),new Vector2(660,72),"Panel_Modal_Light",2.4f);
            Button("ContinueModule2Button",result,new Vector2(-165,0),new Vector2(300,48),"Module1/ContinueModule2",true,16f);
            Button("RepeatModuleButton",result,new Vector2(170,0),new Vector2(280,48),"Module1/RepeatModule",false,16f);
            Set(exp,"_result",result.gameObject);
            result.gameObject.SetActive(false);
            var vp = ui.GetComponent<VideoPlayer>(); if(vp==null) vp=ui.gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake=false; vp.isLooping=false; vp.renderMode=VideoRenderMode.RenderTexture; vp.waitForFirstFrame=false;
            // El audio sale por un AudioSource, no en modo Direct.
            //
            // Direct manda el sonido a la salida del sistema y el reproductor se NIEGA a
            // arrancar si esa salida no esta disponible: medido, Play() dejaba playing=false en
            // el mismo fotograma, sin error ni excepcion, con prepared=true y el clip puesto.
            // Pasa en el editor automatizado y pasaria en cualquier maquina con el audio
            // ocupado. Ademas es lo que conviene en VR: por AudioSource el sonido se puede
            // espacializar y controlar de volumen, por Direct no.
            var speaker=ui.gameObject.GetComponent<AudioSource>();
            if(speaker==null) speaker=ui.gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake=false;
            vp.audioOutputMode=VideoAudioOutputMode.AudioSource;
            vp.EnableAudioTrack(0,true);
            vp.SetTargetAudioSource(0,speaker);
            var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/_Project/Video/VideoScreen_RT.renderTexture"); vp.targetTexture=rt;
            Set(exp,"_video",vp);
            var clip=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Project/Video/VideoTest.mp4"); Set(exp,"_personalVideo",clip); Set(exp,"_vehicleVideo",clip);
            // Por SerializedObject, no por propiedad: asignar .texture directo sobre un RawImage
            // que vive en el prefab no registra el override y al recargar la escena vuelve a null.
            Set(ui.Get<ExplanationView>("Explanation").VideoSurface,"m_Texture",rt);
            // Combined lookup includes vehicle labels for the final report.
            var all=AssetDatabase.LoadAssetAtPath<Module01CatalogSO>("Assets/_Project/Module01/Data/SO_Module01PresentationCatalog.asset");
            if(all==null) { all=ScriptableObject.CreateInstance<Module01CatalogSO>(); AssetDatabase.CreateAsset(all,"Assets/_Project/Module01/Data/SO_Module01PresentationCatalog.asset"); }
            var catalog=AssetDatabase.LoadAssetAtPath<Module01CatalogSO>("Assets/_Project/Module01/Data/SO_Module01Catalog.asset");
            var vehicle=AssetDatabase.LoadAssetAtPath<VehicleProfileSO>("Assets/_Project/Module01/Data/SO_VehicleProfile_Ebike.asset");
            all.ConfigureForTests(catalog.Zones.Concat(vehicle.InspectionZones)); EditorUtility.SetDirty(all); Set(exp,"_allItems",all);
            // Quien esconde el EPP de la mesa mientras gira en el pedestal, y lo deja escondido
            // cuando el aprendiz se lo lleva puesto. Solo los del catalogo personal: los puntos
            // del vehiculo no se "llevan", se validan.
            var worn=root.GetComponent<WornItemVisibility>();
            if(worn==null) worn=root.AddComponent<WornItemVisibility>();
            Set(worn,"_runner",root.GetComponent<ZoneRunner>());
            Set(worn,"_inspector",inspector);
            Set(worn,"_wearable",catalog);

            ArrangeStations(catalog,vehicle);
            AssignUiLayer();
            // Un sprite mal escrito dibuja nada y se ve como "quedo feo". Que reviente, no que calle.
            if(_kit.MissingSprites.Count>0) throw new System.InvalidOperationException("Sprites que no estan en el atlas: "+string.Join(" | ",_kit.MissingSprites));
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        }
        /// <summary>Altura de los ojos del aprendiz de pie, en metros.</summary>
        private const float k_EyeHeight = 1.45f;

        /// <summary>
        /// Manda a la capa <c>UI</c> todo lo que debe dibujar la cámara de overlay.
        /// <para>
        /// Hace falta una pasada propia al final porque <see cref="BuildUiCamera"/> corre al
        /// principio, cuando el HUD todavía no existe: su asignación de capa no lo alcanzaba y
        /// el HUD se quedaba en <c>Default</c>, donde cualquier malla más cercana lo atraviesa.
        /// </para>
        /// <para>
        /// Se hace así, y no moviendo <c>BuildUiCamera</c> al final, porque reordenar el
        /// montador disparó una regresión en la fuente del <c>BeginButton</c>. Esto toca
        /// capas y nada más, que es justo lo que faltaba.
        /// </para>
        /// <para>
        /// El canvas del inspector se queda fuera <b>a propósito</b>: su popup va detrás del
        /// modelo que gira sobre el pedestal, y en la capa de overlay se dibujaría encima.
        /// </para>
        /// </summary>
        private static void AssignUiLayer()
        {
            int ui=LayerMask.NameToLayer("UI");
            if(ui<0) { Debug.LogError("[Module01] No existe la capa 'UI'."); return; }

            string[] roots={ "Module01Controls","PF_Module01_UI","PF_Module00_UI" };
            int moved=0;
            foreach(string name in roots)
            {
                var found=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .FirstOrDefault(t=>t.name==name);
                if(found==null) { Debug.LogWarning($"[Module01] No encuentro '{name}' para asignarle la capa UI."); continue; }
                foreach(var t in found.GetComponentsInChildren<Transform>(true))
                { t.gameObject.layer=ui; moved++; }
            }
            Debug.Log($"[Module01] Objetos puestos en la capa UI: {moved}.");
        }

        /// <summary>
        /// Coloca el punto de vista de cada zona personal frente a los objetos **donde el
        /// equipo de arte los dejó**, y no toca los objetos.
        /// <para>
        /// Antes esto reparentaba los 7 EPP a <c>Module01_Spawned</c>, los ponía en fila
        /// flotando a 1.5 m del ancla y los normalizaba a 32 cm en su lado mayor. De ahí
        /// salían tres problemas a la vez: los objetos no se apoyaban en nada —y por eso
        /// «se hacía clic en la nada»—, la escala dejaba una linterna enana al lado de un
        /// casco gigante, y el trabajo de colocación del equipo de arte se perdía en cada
        /// montaje. El garaje v6 ya los trae puestos sobre el mostrador, cada uno a su
        /// altura; lo único que falta es mirar desde donde se ven.
        /// </para>
        /// <para>
        /// Las zonas del vehículo conservan su ancla fija: ahí manda el punto único de
        /// exhibición, que vive en el ancla <c>vehicle</c> y lo coloca otro comando.
        /// </para>
        /// </summary>
        private static void ArrangeStations(Module01CatalogSO catalog, VehicleProfileSO vehicle)
        {
            var entities=Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var personal=catalog.Zones.ToArray();

            for(int z=0;z<personal.Length;z++)
                PlaceViewpoint(personal[z],entities);

            // Zonas del vehiculo: ancla fija hasta que exista el punto de exhibicion.
            var vehicleZones=vehicle.InspectionZones;
            for(int z=0;z<vehicleZones.Count;z++)
            {
                var anchor=Find("anchor_"+vehicleZones[z].ZoneId).transform;
                anchor.position=new Vector3(-2+z*1.5f,k_EyeHeight,2.3f);
                anchor.rotation=Quaternion.Euler(0,180,0);
            }
        }

        /// <summary>
        /// Pone el ancla de la zona a la distancia justa para que entren todos sus elementos
        /// en pantalla, mirándolos.
        /// <para>
        /// La distancia se deriva del ancho real que ocupan y del campo de visión, no de un
        /// número a ojo: si el equipo de arte junta o separa los objetos, el punto de vista
        /// se ajusta solo. Se avisa cuando una zona queda demasiado desparramada, porque eso
        /// no se arregla alejando la cámara —los objetos se ven diminutos— sino acercándolos
        /// entre ellos en la escena.
        /// </para>
        /// </summary>
        private static void PlaceViewpoint(ZoneSO zone,WorldObjectTaskEntity[] entities)
        {
            var anchor=Find("anchor_"+zone.ZoneId).transform;

            var mine=zone.Items.Where(i=>i!=null)
                .Select(i=>entities.FirstOrDefault(e=>e.ItemId==i.ItemId))
                .Where(e=>e!=null).ToArray();
            if(mine.Length==0)
            {
                Debug.LogWarning($"[Module01] La zona '{zone.ZoneId}' no tiene ningun objeto en la escena; su ancla se queda donde estaba.");
                return;
            }

            // Caja que encierra lo visible de la zona, en mundo.
            Bounds box=new Bounds(mine[0].transform.position,Vector3.zero);
            foreach(var e in mine)
            {
                var renderers=e.GetComponentsInChildren<Renderer>();
                if(renderers.Length==0) { box.Encapsulate(e.transform.position); continue; }
                foreach(var r in renderers) box.Encapsulate(r.bounds);
            }

            // Medio campo horizontal de la camara de juego: FOV 60 vertical sobre 16:9.
            const float halfFovTan=1.026f;

            // Distancia minima: el popup de confirmacion del inspector flota a 1.15 m del ojo y
            // el frente del mostrador sobresale unos 30 cm por delante de los objetos. A 1.30 m
            // -lo que pedia el encuadre- el aprendiz quedaba a 1.00 m del mueble y el popup
            // caia DENTRO del armario: medido, el panel estaba en pantalla (y de 0.03 a 0.26)
            // pero no se veia, tapado por la madera. Se vio en una captura de PlayMode, porque
            // la comprobacion de "esta en pantalla" no sabe de oclusion.
            const float popupDistance=1.15f, counterOverhang=.30f, clearance=.45f;
            float minDistance=popupDistance+counterOverhang+clearance;

            float width=Mathf.Max(box.size.x,box.size.y);
            float distance=Mathf.Clamp(width*.5f/halfFovTan+.45f,minDistance,3.00f);

            if(width>2.5f)
                Debug.LogWarning(
                    $"[Module01] La zona '{zone.ZoneId}' ocupa {width:F2} m de ancho: desde un solo punto "+
                    "sus elementos se ven diminutos. Hay que juntarlos en la escena, no alejar la camara.");

            // El mostrador esta contra la pared del fondo (z negativo), asi que se mira hacia
            // -z y el aprendiz se coloca por delante. El signo sale de la propia caja para que
            // siga valiendo si el arte mueve la estacion a otra pared.
            Vector3 eye=new Vector3(box.center.x,k_EyeHeight,box.center.z+(box.center.z<0f?distance:-distance));
            anchor.position=eye;

            Vector3 look=box.center-eye; look.y=0f;
            anchor.rotation=look.sqrMagnitude>1e-4f?Quaternion.LookRotation(look,Vector3.up):Quaternion.Euler(0,180,0);
        }
        private static void PlaceCanvas(Canvas canvas)
        { canvas.transform.SetParent(_camera,false); canvas.transform.localPosition=new Vector3(0,0,1.8f); canvas.transform.localRotation=Quaternion.identity; canvas.transform.localScale=Vector3.one*.002f; canvas.worldCamera=_camera.GetComponent<Camera>(); }
        private static Canvas Canvas(string name,Transform parent,RenderMode mode)
        { var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)); go.transform.SetParent(parent,false); var c=go.GetComponent<Canvas>(); c.renderMode=mode;c.worldCamera=_camera.GetComponent<Camera>();return c; }
        private static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        /// <summary>Texto del kit (Manrope/Inter), centrado, con su termino de I2 colgado.</summary>
        private static TMP_Text Text(string name,Transform parent,Vector2 pos,Vector2 size,string key,TextStyle style)
        {
            var r=Rect(name,parent,pos,size);
            var t=_kit.AddText(r.gameObject,key,style,TextAlignmentOptions.Center);
            if(!string.IsNullOrEmpty(key)) t.gameObject.AddComponent<Module01Text>().Configure(key);
            return t;
        }

        /// <summary>
        /// Panel 9-sliced del atlas. El rect se infla por el padding transparente del atlas
        /// para que el arte VISIBLE mida exactamente <paramref name="size"/>; como la inflada
        /// es simetrica, los hijos centrados no se mueven.
        /// </summary>
        private static RectTransform Panel(string name,Transform parent,Vector2 pos,Vector2 size,string sprite,float multiplier)
        {
            float pad=UIBuilderKit.PadFor(multiplier);
            var r=Rect(name,parent,pos,size+Vector2.one*(pad*2f));
            _kit.AddSliced(r.gameObject,sprite,multiplier,Color.white,true);
            return r;
        }

        /// <summary>
        /// Pastilla del kit con sus cuatro estados. El kit habla en coordenadas de storyboard
        /// (origen arriba-izquierda del padre); aqui se traduce desde el centro, que es como
        /// esta escrito el resto del montador.
        /// </summary>
        private static Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string key,bool primary,float font)
        {
            Vector2 half=((RectTransform)parent).rect.size*.5f;
            float x0=half.x+pos.x-size.x*.5f, y0=half.y-pos.y-size.y*.5f;
            var b=_ui.Button(parent,name,key,primary,x0,y0,x0+size.x,y0+size.y,font);
            if(!string.IsNullOrEmpty(key)) b.transform.Find("Label").gameObject.AddComponent<Module01Text>().Configure(key);
            return b;
        }

        /// <summary>
        /// Tiñe la pastilla de un botón sin tocar su sprite ni sus estados.
        /// <para>
        /// El atlas solo trae dos pastillas, la primaria coral y la secundaria clara, así que
        /// el color sale del tinte del <c>Image</c>. La etiqueta se queda en azul marino: los
        /// tres tintes son claros y el texto blanco se perdería encima.
        /// </para>
        /// </summary>
        private static Button Tint(Button button, Color color)
        {
            var image=button.GetComponent<Image>();
            if(image!=null) image.color=color;
            return button;
        }

        /// <summary>
        /// Camara de overlay para la capa UI. Sin esto los canvas de world space se ordenan
        /// por profundidad junto con la geometria, y cualquier malla mas cercana los atraviesa:
        /// el video salia detras de un objeto y las esferas del vehiculo perforaban el reporte.
        /// <para>
        /// Comparte transform con la principal, asi que la UI no se mueve ni cambia de escala;
        /// solo se dibuja despues. El canvas del inspector se queda a proposito en Default: su
        /// popup va <b>detras</b> del modelo que gira sobre el pedestal, y esa relacion importa.
        /// </para>
        /// </summary>
        static void BuildUiCamera(Camera main)
        {
            int uiMask = 1 << LayerMask.NameToLayer("UI");

            var hud = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.name == "Module01Controls");
            if (hud != null)
                foreach (var t in hud.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = LayerMask.NameToLayer("UI");

            var old = main.transform.Find("UI Camera");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var go = new GameObject("UI Camera", typeof(Camera));
            go.transform.SetParent(main.transform, false);
            var uiCam = go.GetComponent<Camera>();
            uiCam.fieldOfView = main.fieldOfView;
            uiCam.nearClipPlane = main.nearClipPlane;
            uiCam.farClipPlane = main.farClipPlane;
            uiCam.cullingMask = uiMask;
            uiCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            main.cullingMask &= ~uiMask;
            var data = main.GetUniversalAdditionalCameraData();
            data.cameraStack.Clear();
            data.cameraStack.Add(uiCam);
        }

        private static void RequireUnique(string name)
        {
            var found=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.name==name).ToArray();
            if(found.Length<=1) return;
            throw new System.InvalidOperationException("La escena tiene "+found.Length+" objetos '"+name+"'. Deja uno solo antes de construir: "+string.Join(" | ",found.Select(x=>x.parent==null?"<root>":x.parent.name+"[hijo "+x.GetSiblingIndex()+"]")));
        }
        private static GameObject Find(string name)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.name==name).OrderByDescending(x=>_camera != null && x.IsChildOf(_camera)).First().gameObject;
        private static void Replace(string name) {var old=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.name==name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
        private static void Set(Object o,string field,Object value){var so=new SerializedObject(o);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
        private static void Bool(Object o,string field,bool value){var so=new SerializedObject(o);so.FindProperty(field).boolValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    }
}






