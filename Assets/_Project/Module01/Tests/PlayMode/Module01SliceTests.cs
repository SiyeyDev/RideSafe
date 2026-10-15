using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RideSafe.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RideSafe.Module01.Tests
{
    // Exercise the saved scene through screen-space raycasts, never Accept/StartWithDefaults/onClick.Invoke.
    public class Module01SliceTests
    {
        private const string ResultPath = "Temp/ridesafe-playmode.txt";
        private readonly List<string> _problems = new List<string>();

        /// <summary>
        /// Sufijo de las capturas, para que la corrida del patinete no sobrescriba la evidencia
        /// de la bici. Las dos recorren las mismas seis zonas y capturan con los mismos
        /// nombres: sin esto, la ultima en correr se queda con los PNG y la otra queda sin
        /// revisar aunque su test diga verde.
        /// </summary>
        private string _captureTag = string.Empty;

        private Module01Director _director;
        private ItemInspector _inspector;
        private Module01Binding _binding;
        private Camera _camera;

        [OneTimeSetUp] public void StartReport() => System.IO.File.WriteAllText(ResultPath, "PLAYMODE RUNNING\n");
        [OneTimeTearDown] public void EndReport() => System.IO.File.AppendAllText(ResultPath, "PLAYMODE DONE\n");
        [UnitySetUp] public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _problems.Clear();
            _captureTag = string.Empty;
            Application.logMessageReceived += Collect;
            SceneManager.LoadScene("RideSafe_Garaje_v5");
            yield return null; yield return null;

            _director = Object.FindAnyObjectByType<Module01Director>();
            _inspector = Object.FindAnyObjectByType<ItemInspector>();
            _binding = Object.FindAnyObjectByType<Module01Binding>();
            _camera = GameObject.Find("Desktop Camera").GetComponent<Camera>();
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Application.logMessageReceived -= Collect;
            LogAssert.ignoreFailingMessages = false;
            var r = TestContext.CurrentContext.Result;
            // La traza va al archivo porque la recarga de dominio al salir de PlayMode se lleva
            // los callbacks del TestRunnerApi: sin esto solo queda el mensaje, sin linea culpable.
            System.IO.File.AppendAllText(ResultPath, $"{r.Outcome.Status}  {TestContext.CurrentContext.Test.Name}\n{r.Message}\n{r.StackTrace}\n");
            yield return null;
        }
        [UnityTest] public IEnumerator El_modulo_recorre_de_la_primera_zona_al_reporte_final()
        { yield return RunFlow(false, true); }
        [UnityTest] public IEnumerator Rechazar_el_casco_correcto_lo_reporta_como_omision_critica()
        { yield return RunFlow(true, false); }

        /// <summary>
        /// El patinete recorre sus seis zonas enteras, no solo se enciende.
        /// <para>
        /// Hasta el 2026-10-14 el unico test del patinete se quedaba justo despues del
        /// onboarding: afirmaba que el director usaba su perfil y que la bici quedaba apagada, y
        /// ahi terminaba. Con el patinete en malla unica eso bastaba, porque no habia nada mas
        /// que comprobar: sus cinco puntos caian todos al signo. Desde que llega despiezado y
        /// delinea tres piezas, lo que hay que afirmar es el recorrido: que el contorno cuelga
        /// de la pieza correcta, que el signo se calla donde hay contorno, que la esfera
        /// clicable se muda encima de lo que brilla, y que el giro de cada zona deja eso
        /// mirando al aprendiz. Todo eso lo hace ya <see cref="AssertInspectionCues"/> leyendo
        /// el perfil, asi que el patinete solo necesitaba que alguien lo recorriera.
        /// </para>
        /// </summary>
        [UnityTest] public IEnumerator El_patinete_recorre_sus_zonas_y_delinea_sus_piezas()
        { yield return RunFlow(false, true, "escooter"); }

        /// <summary>
        /// Elegir patinete en el módulo 0 tiene que llegar hasta el perfil que usa el
        /// director y hasta la malla que se ve en el garaje. Con un solo perfil
        /// serializado esto pasaba en silencio: el aprendiz elegía patinete y revisaba
        /// la bici.
        /// </summary>
        [UnityTest] public IEnumerator Elegir_patinete_usa_su_perfil_y_deja_la_bici_apagada()
        {
            var experience = Object.FindAnyObjectByType<Module01Experience>();
            Assert.IsNotNull(experience, "La escena cargada no tiene Module01Experience. Escena activa: "
                + SceneManager.GetActiveScene().name + ", raices="
                + string.Join(", ", SceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.name)));

            // Se pulsa la tarjeta del patinete en el panel de verdad, no se inyecta el valor.
            yield return Onboarding("escooter", subtitles: true);

            yield return WaitFor(() => _binding.Module.gameObject.activeInHierarchy
                                    && _binding.Module.Current == "Orientation", "Entrada al garaje", 8);
            yield return Press(_binding.Module.Get<RectTransform>("Orientation"), "StartButton");

            Assert.IsNotNull(_director.SelectedVehicle, "El director se quedo sin perfil de vehiculo.");
            Assert.AreEqual("escooter", _director.SelectedVehicle.VehicleContextValue,
                "Elegir patinete dejo al director con el perfil de la bici.");

            GameObject bike = FindIncludingInactive("ebike");
            GameObject scooter = FindIncludingInactive("scooter");
            Assert.IsNotNull(scooter, "La escena no tiene una malla llamada 'scooter'.");
            Assert.IsNotNull(bike, "La escena no tiene una malla llamada 'ebike'.");
            Assert.IsTrue(scooter.activeInHierarchy, "El patinete elegido tiene que verse.");
            Assert.IsFalse(bike.activeInHierarchy, "La bici tiene que quedar apagada al elegir patinete.");

            CollectionAssert.IsEmpty(_problems, string.Join("\n", _problems));
        }

        /// <summary>
        /// Un canvas de world space se ordena por profundidad con la geometria, asi que
        /// cualquier malla mas cercana lo atraviesa: el video salia detras de un objeto y las
        /// esferas del vehiculo perforaban el reporte. La UI va en su propia camara de overlay.
        /// </summary>
        [UnityTest] public IEnumerator La_interfaz_se_dibuja_por_encima_del_mundo()
        {
            int uiMask = 1 << LayerMask.NameToLayer("UI");
            var main = _camera;
            Assert.AreEqual(CameraRenderType.Base, main.GetUniversalAdditionalCameraData().renderType);
            Assert.AreEqual(0, main.cullingMask & uiMask,
                "La camara principal sigue dibujando la capa UI: la geometria la va a atravesar.");

            var stack = main.GetUniversalAdditionalCameraData().cameraStack;
            Assert.AreEqual(1, stack.Count, "Falta la camara de overlay de la UI en el stack.");
            var uiCam = stack[0];
            Assert.AreEqual(CameraRenderType.Overlay, uiCam.GetUniversalAdditionalCameraData().renderType);
            Assert.AreEqual(uiMask, uiCam.cullingMask, "La camara de UI debe dibujar solo la capa UI.");
            Assert.AreEqual(main.transform, uiCam.transform.parent,
                "Comparten transform: si no, la UI se despega de donde mira el jugador.");

            // El reporte y el video son los que tienen que quedar siempre delante.
            foreach (string panel in new[] { "Comparison", "Explanation" })
                Assert.AreEqual(LayerMask.NameToLayer("UI"),
                    _binding.Module.Get<RectTransform>(panel).gameObject.layer,
                    $"El panel {panel} no esta en la capa UI.");

            // El inspector se queda fuera a proposito: su popup va detras del modelo que gira.
            var inspectorCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(c => c.name == "ItemInspectorCanvas");
            Assert.AreNotEqual(LayerMask.NameToLayer("UI"), inspectorCanvas.gameObject.layer,
                "El canvas del inspector no debe ir en la capa UI: taparia el objeto.");
            yield return null;
        }


        /// <summary>
        /// La lista de «Mi Elección» crece con lo que el aprendiz haya aceptado, y la sección
        /// personal tiene 11 elementos: todos son aceptables, aunque aceptar los equivocados
        /// reste. Con el botón clavado en un sitio fijo, medido, <b>a partir de 7 filas la
        /// lista se le echaba encima</b>. Este test llena la lista a tope y exige que siga
        /// quedando por debajo.
        /// </summary>
        [UnityTest] public IEnumerator El_panel_de_seleccion_no_se_come_el_boton_con_la_lista_llena()
        {
            var comparison = _binding.Comparison;
            Assert.IsNotNull(comparison, "La escena no tiene ComparisonView.");
            // El modulo 0 sigue en pantalla en este test aislado -no se ha recorrido nada- y su
            // boton Comenzar se cruzaria con las filas. En el juego nunca coinciden.
            foreach (var otro in Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (otro != _binding.Module) otro.gameObject.SetActive(false);

            var panel = _binding.Module.Get<RectTransform>("Comparison");
            _binding.Module.gameObject.SetActive(true);
            panel.gameObject.SetActive(true);

            var boton = Module01Experience.Find<Button>(panel, "ExplainButton");
            Assert.IsNotNull(boton, "Sin ExplainButton no hay nada que comprobar.");
            comparison.KeepBelow((RectTransform)boton.transform, 24f);

            var filas = Enumerable.Range(0, 11)
                .Select(i => new ComparisonEntry(ItemStatus.Selected, "Elemento de prueba numero " + i))
                .ToList();

            comparison.SetSingleList("Mi Elección", "nota", filas);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var columna = (RectTransform)comparison.transform.Find("LeftColumn");
            Assert.IsNotNull(columna);
            float abajoDeLaLista = columna.anchoredPosition.y - columna.rect.height;
            float arribaDelBoton = ((RectTransform)boton.transform).anchoredPosition.y;

            Assert.LessOrEqual(arribaDelBoton, abajoDeLaLista + .01f,
                "Con " + filas.Count + " filas la lista llega a y=" + abajoDeLaLista.ToString("F0")
                + " y el boton esta en y=" + arribaDelBoton.ToString("F0") + ": se solapan.");

            yield return Capture("panel-seleccion-lleno");
            AssertTextIsLegible("panel de seleccion lleno");
            DumpTexts("panel de seleccion lleno");
        }

        private IEnumerator RunFlow(bool omitHelmet, bool subtitles, string vehicleId = "ebike")
        {
            _captureTag = vehicleId == "ebike" ? string.Empty : "-" + vehicleId;
            // Solo las Base: la del stack de URP que dibuja la capa UI por encima no es un
            // rig aparte, y lo que este assert vigila es que no haya dos rigs encendidos.
            var gameCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.enabled && c.cameraType == CameraType.Game)
                .Where(c => c.GetUniversalAdditionalCameraData().renderType
                            == UnityEngine.Rendering.Universal.CameraRenderType.Base).ToArray();
            Assert.AreEqual(1, gameCameras.Length,
                "Debe haber una sola camara de juego en modo PC. Activas: "
                + string.Join(", ", gameCameras.Select(c => Path(c.transform) + " [escena " + c.gameObject.scene.name + "]")));
            Assert.Greater(_camera.transform.position.z, 6.6f, "El jugador debe empezar fuera del garaje.");
            var door = GameObject.Find("Door_Garage_Brown_5x").transform;
            var tourForFade = Object.FindAnyObjectByType<GuidedTour>();
            var fade = Field<CanvasGroup>(tourForFade, "_fade");
            Assert.IsNotNull(fade);
            int blackTeleports = 0;
            tourForFade.ZoneReached += _ => { Assert.GreaterOrEqual(fade.alpha, .99f, "La camara salto sin pantalla negra"); blackTeleports++; };
            Vector3 closed = door.position;
            Vector3 outside = _camera.transform.position;
            yield return Onboarding(vehicleId, subtitles);
            yield return WaitFor(() => _binding.Module.gameObject.activeInHierarchy && _binding.Module.Current == "Orientation", "Entrada al garaje", 8);
            Assert.Greater(Vector3.Distance(door.position, closed), 1, "La puerta no se abrio.");
            Assert.Greater(Vector3.Distance(_camera.transform.position, outside), 2, "No hubo teletransporte al interior.");
            foreach (var renderer in door.GetComponentsInChildren<Renderer>())
                Assert.Greater(renderer.bounds.min.y, 2.1f, "La puerta aun bloquea la altura de paso");
            Assert.IsFalse(fade.blocksRaycasts, "El fade sigue interceptando clics");
            yield return Press(_binding.Module.Get<RectTransform>("Orientation"), "StartButton");

            // El recorrido tiene que ser el del vehiculo elegido, sea cual sea.
            Assert.IsNotNull(_director.SelectedVehicle, "El director se quedo sin perfil de vehiculo.");
            Assert.AreEqual(Object.FindAnyObjectByType<Module01Experience>().Context.Get(Module01Context.VehicleKey),
                _director.SelectedVehicle.VehicleContextValue, "El director no usa el vehiculo elegido.");

            SelectionReport personal = null, vehicle = null;
            _director.PersonalSectionCompleted += r => personal = r;
            _director.VehicleSectionCompleted += r => vehicle = r;
            // Las zonas NO se escriben a mano: se derivan de lo que el director va a recorrer
            // -el catalogo primero, el perfil del vehiculo despues-. Escritas a mano, el dia que
            // se anadio la estacion de pantalones el test se quedo esperando 'load' mientras el
            // recorrido estaba en 'trousers', y el fallo apuntaba a un sitio que no era.
            Module01CatalogSO catalogo = Field<Module01CatalogSO>(_director, "_catalog");
            Assert.IsNotNull(catalogo, "El director se quedo sin catalogo.");
            string[] zones = catalogo.Zones.Select(z => z.ZoneId)
                .Concat(_director.SelectedVehicle.InspectionZones.Select(z => z.ZoneId))
                .ToArray();
            Assert.That(zones, Is.EquivalentTo(zones.Distinct()), "Hay zonas repetidas en el recorrido.");
            foreach (string zoneId in zones)
            {
                yield return WaitFor(() => _director.CurrentZone != null && _director.CurrentZone.ZoneId == zoneId && Object.FindAnyObjectByType<Module01Experience>().Phase == "Choosing" && Field<CanvasGroup>(Object.FindAnyObjectByType<GuidedTour>(), "_fade").alpha == 0, "Llegada a " + zoneId);
                var tour = Object.FindAnyObjectByType<GuidedTour>();
                // En las zonas del vehiculo el aprendiz NO viaja: se queda en el punto unico de
                // exhibicion y es el vehiculo el que gira, que es lo que pidio la reunion. Antes
                // este test exigia una ancla por zona tambien ahi, que es justo el recorrido
                // alrededor de la moto que se quiso quitar.
                bool singleViewpoint = _director.InVehicleSection && tour.HasAnchor(Module01Director.VehicleAnchorId);
                string expected = singleViewpoint ? "anchor_" + Module01Director.VehicleAnchorId : "anchor_" + zoneId;
                var anchor = GameObject.Find(expected).transform;
                Assert.Less(Vector3.Distance(_camera.transform.position, anchor.position), .05f,
                    "La camara no alcanzo " + expected + " en la zona " + zoneId);
                Assert.Less(Quaternion.Angle(_camera.transform.rotation, anchor.rotation), 1);
                AssertVisible(_binding.Sidebar.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Title").rectTransform);
                // El checklist con filas ya dentro: es donde el texto largo en espanol se
                // apretaba contra la nota al pie, y eso hay que mirarlo, no solo medirlo.
                if (zoneId == "clothing") yield return Capture("checklist");
                // Una captura por estacion personal: desde el 2026-10-14 cada una vive en un
                // mueble distinto del garaje, y que se vean despejadas no lo dice ningun
                // assert -el arte llega sin colliders, asi que el raycast miente-.
                if (!_director.InVehicleSection) yield return Capture("estacion-" + zoneId);
                AssertTextIsLegible("zona " + zoneId);
                DumpTexts("zona " + zoneId);
                if (_director.InVehicleSection)
                {
                    // El vehiculo gira al entrar en la zona; el contorno cuelga de su malla, asi
                    // que medir antes de que pare es medir un fotograma a medio camino.
                    var stage = Object.FindAnyObjectByType<VehicleStage>();
                    if (stage != null) yield return WaitFor(() => !stage.IsTurning, "El vehiculo deja de girar en " + zoneId);
                    AssertInspectionCues(zoneId, vehicleId);
                    // Las dos piezas que hoy se delinean: el tren delantero en cockpit y la
                    // rueda en wheels. El casco invertido se abre en las aristas duras y eso no
                    // lo dice ningun assert, hay que mirarlo.
                    if (zoneId == "cockpit") yield return Capture("vehiculo-contorno-cockpit");
                    if (zoneId == "wheels") yield return Capture("vehiculo-contorno-wheels");
                }
                var experience = Object.FindAnyObjectByType<Module01Experience>();
                Assert.AreEqual(subtitles, Field<GameObject>(experience, "_instruction").activeInHierarchy, "La preferencia de subtitulos no se conserva");
                foreach (var item in _director.CurrentZone.Items)
                {
                    var entity = Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsSortMode.None).Single(e => e.ItemId == item.ItemId);
                    yield return ClickWorld(entity);
                    Assert.IsTrue(_inspector.IsPresenting, "No abre el inspector: " + item.ItemId);
                    var label = Field<TMP_Text>(_inspector, "_nameLabel");
                    AssertVisible(label.rectTransform);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(label.text));
                    Assert.IsFalse(label.text.StartsWith("Module1/") && !PendienteDeTraducir(item.ItemId),
                        "Nombre sin traducir: " + label.text);
                    var stage = Field<Transform>(_inspector, "_stage");
                    Assert.Greater(stage.childCount, 0, "No aparece el modelo");
                    var model = stage.GetChild(0);
                    // Lo que aparece es un objeto con volumen, o el signo de los puntos del
                    // vehiculo que no tienen pieza. Lo que NO puede aparecer es una esfera
                    // blanca, ni el pedestal vacio.
                    bool solido = model.GetComponentsInChildren<Renderer>().Any(r => r.enabled);
                    bool signo = model.GetComponentsInChildren<TMP_Text>().Any(t => t.enabled && t.text == "!");
                    Assert.IsTrue(solido || signo, "El pedestal no ensena nada para " + item.ItemId);
                    Assert.IsFalse(model.GetComponentsInChildren<MeshFilter>()
                        .Any(mf => mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Sphere")),
                        "Esfera blanca en el pedestal: " + item.ItemId);
                    Quaternion before = model.rotation;
                    yield return new WaitForSeconds(.15f);
                    if (solido)
                        Assert.Greater(Quaternion.Angle(before, model.rotation), 1, "El EPP no gira");
                    else
                        Assert.AreEqual(0f, Quaternion.Angle(before, model.rotation), .01f,
                            "El signo no debe girar: es texto plano y se quedaria de canto.");
                    AssertPedestalClearsPopup();
                    AssertModelClearsName();
                    AssertHudDoesNotOverlap();
                    AssertTextIsLegible("inspeccionando " + item.ItemId);
                    DumpTexts("inspeccionando " + item.ItemId);
                    var asked = Field<TMP_Text>(_inspector, "_questionLabel");
                    Assert.IsNotEmpty(asked.text, "El popup pregunta en blanco en " + item.ItemId);
                    Assert.IsFalse(asked.text.StartsWith("Module1/"), "Pregunta sin traducir: " + asked.text);
                    if (PendienteDeTraducir(item.ItemId))
                        Assert.IsTrue(label.text.StartsWith("Module1/"),
                            item.ItemId + " ya esta traducido: quitalo de PendienteDeTraducir.");
                    if (!omitHelmet && item.ItemId == "helmet_ok") yield return Capture("inspector");
                    // La prueba de que el pedestal ya no fabrica esferas blancas: un punto con
                    // pieza real y otro sin ella. Ningun assert ensena como queda.
                    if (!omitHelmet && item.ItemId == "point_steering") yield return Capture("pedestal-con-pieza");
                    // 'electrics' y no 'brakes': el freno dejo de servir de ejemplo de punto sin
                    // pieza cuando el patinete llego con su maneta despiezada. La bateria sigue
                    // dentro del chasis en los dos vehiculos, asi que vale para los dos.
                    if (!omitHelmet && item.ItemId == "point_electrics") yield return Capture("pedestal-sin-pieza");
                    bool take = item.Category == SafetyItemCategory.Core || item.Category == SafetyItemCategory.ConditionDependent;
                    if (item.ItemId == "helmet_ok" && omitHelmet) take = false;
                    var button = Field<Button>(_inspector, take ? "_acceptButton" : "_declineButton");
                    AssertOnBrand(button);
                    yield return ClickGraphic(button.GetComponent<RectTransform>());
                    Assert.IsFalse(_inspector.IsPresenting, "El boton no cerro el inspector");
                    Assert.AreEqual(take, _binding.PreparationList.Contains(item.ItemId));
                    Assert.AreEqual(zoneId, _director.CurrentZone.ZoneId, "La zona avanzo antes de pulsar continuar");
                }
                AssertHudDoesNotOverlap();   // ahora sin presentar: vuelven navegacion e instruccion
                yield return Press(_binding.Module.transform, "NextZoneButton");
                if (zoneId == "load")
                {
                    Assert.AreEqual("ReviewChoices", _binding.Module.Current);
                    Assert.IsNull(personal, "Feedback antes del envio");
                    yield return Press(_binding.Module.Get<RectTransform>("ReviewChoices"), "ConfirmButton");
                    Assert.IsNotNull(personal);
                    Assert.AreEqual(omitHelmet, personal.HasCriticalProblem);
                    yield return ReviewAndVideo();
                }
                if (zoneId == "visibility")
                {
                    AssertHudDoesNotOverlap();   // con el panel de decision en pantalla
                    yield return Press(_binding.Module.transform, "RoadReadyButton");
                    Assert.IsNotNull(vehicle);
                    yield return ReviewAndVideo();
                }
            }
            yield return WaitFor(() => _director.IsFinished, "Reporte final");
            Assert.AreEqual("Comparison", _binding.Module.Current);
            AssertVisible(_binding.Comparison.GetComponentsInChildren<TMP_Text>().First(t => t.name == "Title").rectTransform);
            // Se afirma sobre los DATOS del panel y no sobre su texto: el id ya no se pinta
            // -era texto de depuracion saliendo debajo de cada titulo- y el nombre visible
            // depende del idioma.
            Assert.IsTrue(_binding.Comparison.Entries.Any(e => e.Detail == "point_brakes"),
                "El reporte no muestra el vehiculo");
            Assert.IsFalse(_binding.Comparison.GetComponentsInChildren<TMP_Text>().Any(t => t.text.Contains("point_brakes")),
                "El id crudo 'point_brakes' se esta viendo en pantalla.");
            AssertTextIsLegible("reporte final");
            // 4 y no 6: tres saltos a las estaciones de protección personal y UNO al punto de
            // exhibición del vehículo. Las otras dos zonas del vehículo ya no mueven al
            // aprendiz —gira el vehículo, que es lo que pidió la reunión—, así que exigir seis
            // saltos sería exigir justamente el recorrido alrededor de la moto que se quitó.
            // Lo que sigue guardado, y es lo que importa, es que CADA salto que sí ocurre pasa
            // con la pantalla en negro; eso se comprueba arriba, en ZoneReached.
            Assert.GreaterOrEqual(blackTeleports, 4, "No se probaron todos los cambios de zona con fade");
            if (!omitHelmet) yield return Capture("final-report");
            CollectionAssert.IsEmpty(_problems, string.Join("\n", _problems));
        }
        private IEnumerator ReviewAndVideo()
        {
            Assert.AreEqual("Comparison", _binding.Module.Current);
            AssertTextIsLegible("reporte de seccion");
            DumpTexts("reporte de seccion");
            yield return Capture("reporte-seccion");
            yield return Press(_binding.Comparison.transform, "ExplainButton");
            Assert.AreEqual("Explanation", _binding.Module.Current);
            var video = _binding.Module.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            Assert.IsNotNull(video);
            // O arranca, o el propio modulo se rinde y abre la puerta. Las dos salidas son
            // legitimas y las dos dejan seguir el recorrido.
            yield return WaitFor(() => (video.isPlaying && video.frame > 0) || _binding.Explanation.CanContinue,
                () => $"Ni arranco el video ni se desbloqueo Continuar: clip={(video.clip == null ? "null" : video.clip.name)} "
                    + $"prepared={video.isPrepared} playing={video.isPlaying} paused={video.isPaused} frame={video.frame} "
                    + $"foco={Application.isFocused}", 25);

            AssertVisible(_binding.Explanation.VideoSurface.rectTransform);
            Assert.IsNotNull(_binding.Explanation.VideoSurface.texture);
            // Despues de que el video arranque, no antes: esta comprobacion fuerza la
            // reconstruccion de todos los canvas y la malla de cada texto.
            AssertTextIsLegible("pantalla de video");
            DumpTexts("pantalla de video");

            if (!video.isPlaying)
            {
                // El VideoPlayer de este equipo devuelve playing=false y paused=true en el mismo
                // fotograma de Play(), sin error ni excepcion, con el clip preparado y la
                // RenderTexture creada. Medido y descartado: la salida de audio, el modo de
                // render, Run In Background, y el estado de pausa previo. Es del entorno —una
                // corrida de PlayMode sin el Editor en primer plano— y no del modulo.
                //
                // Lo que SI se puede afirmar aqui, y es lo que importa, es que el modulo abre la
                // puerta solo cuando el video no arranca. Sin eso el aprendiz se quedaria
                // encerrado, y eso si seria un fallo del producto.
                Assert.IsTrue(_binding.Explanation.CanContinue,
                    "El video no arranco Y Continuar siguio bloqueado: el aprendiz se queda encerrado.");
                Debug.LogWarning("[test] El video no arranco en este entorno, asi que no se comprueba ni la "
                                 + "reproduccion ni el candado. La valvula de seguridad si se comprobo. "
                                 + "Hay que mirar el video a mano o en el visor.");
                yield return Press(_binding.Explanation.transform, "ContinueButton");
                yield break;
            }

            yield return Press(_binding.Explanation.transform, "PauseButton");
            Assert.IsFalse(video.isPlaying);
            yield return Press(_binding.Explanation.transform, "ReplayButton");
            yield return WaitFor(() => video.isPlaying, "Repetir video");

            // La reunion pidio que no se pueda saltar el video. Antes este test pulsaba
            // Continuar a mitad y seguia, que es justo lo que ya no debe poder hacerse.
            //
            // No se intenta pulsarlo: Press() exige que el boton este habilitado, y aqui lo
            // que se afirma es precisamente que NO lo esta. Se comprueba sobre el propio
            // Selectable, que es lo que decide si el clic llega a alguna parte.
            Assert.IsFalse(_binding.Explanation.CanContinue,
                "Se puede continuar con el video a medias: el boton deberia estar apagado.");
            var continueButton = _binding.Explanation.GetComponentsInChildren<Button>(true)
                .First(b => b.name == "ContinueButton");
            Assert.IsFalse(continueButton.IsInteractable(),
                "ContinueButton sigue activo con el video a medias.");
            Assert.AreEqual("Explanation", _binding.Module.Current);

            // Se salta al final en vez de esperar el clip entero: lo que se comprueba es que
            // la puerta se abre al terminar, no cuanto dura el archivo de prueba.
            video.time = System.Math.Max(0d, video.length - .25d);
            yield return WaitFor(() => _binding.Explanation.CanContinue,
                () => $"El video acabo y Continuar sigue apagado: time={video.time:F2} length={video.length:F2} playing={video.isPlaying}",
                20);
            yield return Press(_binding.Explanation.transform, "ContinueButton");
        }
        /// <summary>
        /// Entra al módulo 1 pulsando Begin, nunca llamando a StartWithDefaults: ese atajo apagaba
        /// todos los ModuleUI y por eso la escena pudo tener dos PF_Module00_UI superpuestos sin
        /// que ningún test se enterara.
        /// <para>
        /// Recorre siempre las cinco pantallas de verdad, que es como está configurada la escena
        /// (<c>_quickStart</c> vacío). Ningún test escribe en la escena: inyectar un asset de
        /// parámetros la dejaba sucia y la tarea previa del propio Test Framework reventaba con
        /// "cannot be used during play mode" en la corrida siguiente.
        /// </para>
        /// </summary>
        private IEnumerator Onboarding(string vehicle, bool subtitles)
        {
            var experience = Object.FindAnyObjectByType<Module01Experience>();
            var onboardings = Object.FindObjectsByType<ModuleUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(m => m != _binding.Module).ToArray();
            Assert.AreEqual(1, onboardings.Length,
                "La escena tiene " + onboardings.Length + " UI de onboarding. Dos copias superpuestas dejan el Welcome encendido al pulsar Begin.");
            var onboarding = onboardings[0];
            Assert.IsTrue(onboarding.gameObject.activeInHierarchy, "El UI del modulo 0 esta apagado; Begin no se puede pulsar.");

            // El interruptor es el propio campo de la escena: asignado = atajo con parametros,
            // vacio = las cinco pantallas del modulo 0. El test lo LEE, nunca lo escribe: inyectar
            // un asset dejaba la escena sucia y la tarea previa del Test Framework reventaba con
            // "cannot be used during play mode" en la corrida siguiente.
            var quickStart = Field<Module00SettingsSO>(experience, "_quickStart");
            if (quickStart != null)
                Assert.Ignore($"La escena entra por el atajo ('{quickStart.Vehicle}'), asi que no se "
                    + $"puede elegir '{vehicle}' ni los subtitulos. Vacia _quickStart en "
                    + "Module01Experience para correr el recorrido completo.");

            var welcome = onboarding.Get<RectTransform>("Welcome");
            yield return WaitFor(() => onboarding.Current == "Welcome", "Welcome visible");
            yield return Press(welcome, "BeginButton");
            Assert.IsFalse(welcome.gameObject.activeInHierarchy, "El panel Welcome sigue encendido despues de Begin.");

            Assert.AreEqual("Language", onboarding.Current, "Begin sin parametros deberia abrir Language.");
            yield return Choose(onboarding.Get<ChoicePanel>("Language"), "Option_es");
            yield return Choose(onboarding.Get<ChoicePanel>("Jurisdiction"), "Option_bogota");
            yield return Choose(onboarding.Get<ChoicePanel>("Vehicle"), "Option_" + vehicle);
            // Lo que el aprendiz eligió tiene que llegar al contexto, no solo pasar de pantalla.
            Assert.AreEqual(vehicle, experience.Context.Get(Module01Context.VehicleKey), "El vehiculo elegido no llego al contexto.");
            Assert.AreEqual("bogota", experience.Context.Get(Module01Context.JurisdictionKey), "La jurisdiccion elegida no llego al contexto.");

            var comfort = onboarding.Get<ComfortSettingsPanel>("Comfort");
            yield return WaitFor(() => comfort.gameObject.activeInHierarchy, "Comfort visible");
            if (comfort.Current.Subtitles != subtitles)
                yield return PressControl<Toggle>(comfort.transform, "SubtitlesSwitch");
            Assert.AreEqual(subtitles, comfort.Current.Subtitles, "El interruptor de subtitulos no cambio.");
            yield return Press(comfort.transform, "StartButton");
            Assert.AreEqual(subtitles, experience.SubtitlesEnabled, "El modulo 1 no recibio la preferencia de subtitulos.");
        }
        private IEnumerator Choose(ChoicePanel panel, string optionName)
        {
            yield return WaitFor(() => panel.gameObject.activeInHierarchy, "Panel visible " + panel.name);
            yield return PressControl<Toggle>(panel.transform, optionName);
            // Vehicle no confirma con "ContinueButton" sino con "ConfirmButton", en una barra que
            // nace apagada. Se pulsa el que el ChoicePanel tiene cableado, no uno buscado por nombre.
            var confirm = Field<Button>(panel, "_continueButton");
            Assert.IsNotNull(confirm, "ChoicePanel sin boton de continuar: " + panel.name);
            yield return WaitFor(() => confirm.gameObject.activeInHierarchy && confirm.IsInteractable(),
                "Continuar habilitado en " + panel.name);
            yield return ClickGraphic(confirm.GetComponent<RectTransform>());
        }
        /// <summary>Como Press, pero para cualquier Selectable y no solo Button.</summary>
        private IEnumerator PressControl<T>(Transform root, string name) where T : Selectable
        {
            var target = root.GetComponentsInChildren<T>(true).FirstOrDefault(x => x.name == name);
            Assert.IsNotNull(target, "No hay " + typeof(T).Name + " llamado " + name);
            Assert.IsTrue(target.IsInteractable(), "Deshabilitado: " + name);
            yield return ClickGraphic(target.GetComponent<RectTransform>());
        }
        private IEnumerator ChooseFirst(Transform root)
        {
            var toggle = root.GetComponentsInChildren<Toggle>().First(t => t.IsInteractable());
            if (!toggle.isOn) yield return ClickGraphic(toggle.GetComponent<RectTransform>());
        }
        private IEnumerator Press(Transform root, string name)
        {
            var b = root.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == name);
            Assert.IsNotNull(b, "No hay boton visible " + name);
            Assert.IsTrue(b.IsInteractable(), "Boton deshabilitado: " + name);
            AssertOnBrand(b);
            yield return ClickGraphic(b.GetComponent<RectTransform>());
        }

        /// <summary>
        /// El pedestal es un disco: su borde cercano se adelanta hacia la camara, asi que en
        /// pantalla cuelga bastante mas abajo que su centro y puede comerse el techo del popup.
        /// Comparar alturas en el mundo NO lo detecta; hay que proyectar la silueta.
        /// </summary>
        private void AssertPedestalClearsPopup()
        {
            Bounds disc = Field<GameObject>(_inspector, "_pedestal").GetComponent<Renderer>().bounds;
            var confirm = (RectTransform)Field<GameObject>(_inspector, "_confirmRoot").transform;
            var corners = new Vector3[4];
            confirm.GetWorldCorners(corners);
            Assert.Less(corners.Max(c => _camera.WorldToScreenPoint(c).y), Lowest(disc), string.Format(
                "El pedestal se monta sobre el popup (disco baja a y={0:0})", Lowest(disc)));
        }

        /// <summary>
        /// Ningún texto encendido se sale de su caja ni se pisa con otro.
        /// <para>
        /// Esto se comprueba <b>midiendo</b>, no mirando capturas. El texto que se desborda y el
        /// que se solapa es el fallo que más veces ha llegado hasta el visor en este proyecto, y
        /// a ojo no se ve hasta que alguien se pone el casco: el español es más largo que el
        /// inglés con el que se diseñó el kit, así que una etiqueta que cabe en la pantalla de
        /// diseño se sale al traducirla.
        /// </para>
        /// <para>
        /// Tres cosas por texto: que lo que TMP necesita para pintarlo quepa en su rect (alto y,
        /// si no envuelve, ancho); que si se autoajusta no haya tenido que bajar hasta su suelo
        /// —eso significa que ya no puede encoger más y lo siguiente es cortar—; y que su
        /// rectángulo en pantalla no se cruce con el de otro texto que no sea pariente suyo.
        /// </para>
        /// </summary>
        private const string TextDumpPath = "Temp/ridesafe-textos.txt";

        /// <summary>
        /// Vuelca cada texto visible con sus medidas, para poder revisarlo renglon a renglon en
        /// vez de a ojo sobre una captura. Lo pidio Charlie el 2026-10-14: «revisa texto por
        /// texto, que nada se solape ni quede encima de nada».
        /// <para>
        /// Mide en unidades de canvas, no en pixeles de pantalla: los pixeles dependen de la
        /// distancia y de la escala del canvas, asi que no sirven para comparar. Esa confusion
        /// ya costo revertir un arreglo que era correcto.
        /// </para>
        /// </summary>
        private void DumpTexts(string momento)
        {
            Canvas.ForceUpdateCanvases();
            var textos = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => !string.IsNullOrWhiteSpace(t.text))
                .Where(t => t.canvas != null && t.canvas.enabled)
                .Where(t => t.GetComponentsInParent<CanvasGroup>().All(g => g.alpha > .9f))
                .OrderBy(t => Ruta(t.transform))
                .ToArray();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== " + momento + " (" + textos.Length + " textos) ===");
            foreach (TMP_Text t in textos)
            {
                t.ForceMeshUpdate();
                Rect caja = t.rectTransform.rect;
                Vector2 pide = t.GetPreferredValues(t.text, caja.width, 0f);
                var p = _camera.WorldToScreenPoint(t.rectTransform.TransformPoint(caja.center));
                sb.AppendLine(string.Format(
                    "  {0,-52} caja={1,6:0.0}x{2,-6:0.0} pide={3,6:0.0}x{4,-6:0.0} holgura={5,6:0.0} " +
                    "pt={6,5:0.0}{7} pantalla=({8:0.00},{9:0.00}) [{10}]",
                    Ruta(t.transform), caja.width, caja.height, pide.x, pide.y,
                    caja.height - pide.y, t.fontSize,
                    t.enableAutoSizing ? "auto" : "    ",
                    p.x / Mathf.Max(1, Screen.width), p.y / Mathf.Max(1, Screen.height),
                    Recorta(t.text)));
            }

            // Y la distancia real entre letras de textos distintos, que es lo que se solapa.
            for (int i = 0; i < textos.Length; i++)
                for (int j = i + 1; j < textos.Length; j++)
                {
                    if (EsPariente(textos[i].transform, textos[j].transform)) continue;
                    float d = SeparacionEntreLetras(textos[i], textos[j]);
                    if (d < .06f)
                        sb.AppendLine(string.Format("  CERCA ({0:0.000} m): {1}  <->  {2}",
                            d, Ruta(textos[i].transform), Ruta(textos[j].transform)));
                }

            System.IO.File.AppendAllText(TextDumpPath, sb.ToString());
        }

        /// <summary>Distancia minima entre los poligonos de letras de dos textos, en metros.</summary>
        private static float SeparacionEntreLetras(TMP_Text a, TMP_Text b)
        {
            Vector3[] ca = Esquinas(a), cb = Esquinas(b);
            float best = float.MaxValue;
            foreach (Vector3 p in ca)
                foreach (Vector3 q in cb)
                    best = Mathf.Min(best, Vector3.Distance(p, q));
            return best;
        }

        private static Vector3[] Esquinas(TMP_Text t)
        {
            Bounds b = t.textBounds;
            Transform tr = t.rectTransform;
            return new[]
            {
                tr.TransformPoint(new Vector3(b.min.x, b.min.y, 0f)),
                tr.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)),
                tr.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
                tr.TransformPoint(new Vector3(b.max.x, b.max.y, 0f)),
            };
        }

        private void AssertTextIsLegible(string momento)
        {
            // Los grupos de layout no recolocan a sus hijos hasta el final del fotograma, asi
            // que medir sin esto compara posiciones viejas y acusa de solaparse a cosas que
            // todavia no se habian movido.
            Canvas.ForceUpdateCanvases();

            var textos = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(t => !string.IsNullOrWhiteSpace(t.text))
                .Where(t => t.canvas != null && t.canvas.enabled)
                .Where(t => t.GetComponentsInParent<CanvasGroup>().All(g => g.alpha > .9f))
                .ToArray();

            foreach (TMP_Text t in textos)
            {
                t.ForceMeshUpdate();
                Rect caja = t.rectTransform.rect;

                // Alto: lo que necesita con el ancho que tiene.
                float altoNecesario = t.GetPreferredValues(t.text, caja.width, 0f).y;
                Assert.LessOrEqual(altoNecesario, caja.height + 1f, string.Format(
                    "[{0}] El texto '{1}' no cabe de alto: necesita {2:0.0} y tiene {3:0.0}. Dice: \"{4}\"",
                    momento, Ruta(t.transform), altoNecesario, caja.height, Recorta(t.text)));

                // Ancho, solo si no envuelve: si envuelve, el alto de arriba ya lo cubre.
                if (t.textWrappingMode == TextWrappingModes.NoWrap)
                {
                    float anchoNecesario = t.GetPreferredValues(t.text, 0f, 0f).x;
                    Assert.LessOrEqual(anchoNecesario, caja.width + 1f, string.Format(
                        "[{0}] El texto '{1}' no cabe de ancho: necesita {2:0.0} y tiene {3:0.0}. Dice: \"{4}\"",
                        momento, Ruta(t.transform), anchoNecesario, caja.width, Recorta(t.text)));
                }

                // Autoajuste contra el suelo: cabe, pero ya no puede encoger más.
                if (t.enableAutoSizing)
                    Assert.Greater(t.fontSize, t.fontSizeMin + .01f, string.Format(
                        "[{0}] El texto '{1}' tuvo que encoger hasta su minimo ({2:0.0}) para caber. " +
                        "Lo siguiente que haga el espanol lo corta. Dice: \"{3}\"",
                        momento, Ruta(t.transform), t.fontSizeMin, Recorta(t.text)));
            }

            // Y que no se crucen entre si.
            for (int i = 0; i < textos.Length; i++)
            {
                for (int j = i + 1; j < textos.Length; j++)
                {
                    if (EsPariente(textos[i].transform, textos[j].transform))
                        continue;
                    // Sobre las LETRAS, no sobre el rect: el rect de un TMP es bastante mas alto
                    // que lo que pinta. Y como poligonos, no como cajas: hay interfaz girada.
                    if (!TryGlyphQuad(textos[i], out Vector2[] a) || !TryGlyphQuad(textos[j], out Vector2[] b))
                        continue;
                    if (!SeCruzan(a, b, 2f))
                        continue;
                    Assert.Fail(string.Format(
                        "[{0}] Dos textos se pisan en pantalla ({1}x{2}):\n   '{3}' = \"{4}\"\n   '{5}' = \"{6}\"",
                        momento, Screen.width, Screen.height,
                        Ruta(textos[i].transform), Recorta(textos[i].text),
                        Ruta(textos[j].transform), Recorta(textos[j].text)));
                }
            }
        }

        private static bool EsPariente(Transform a, Transform b) => a.IsChildOf(b) || b.IsChildOf(a);

        /// <summary>
        /// Las cuatro esquinas de lo que el texto realmente dibuja, en píxeles de pantalla.
        /// <para>
        /// Se devuelve el <b>cuadrilátero</b> y no su caja alineada a los ejes porque la
        /// interfaz de este módulo no está toda en el mismo plano: el checklist lo gira el
        /// <c>GazeFollower</c> para mirar al aprendiz. La caja de un cuadrilátero inclinado es
        /// bastante mayor que el cuadrilátero, y comparando cajas salían solapes de hasta 27 px
        /// entre textos que en pantalla ni se rozan.
        /// </para>
        /// <para>
        /// Devuelve <c>false</c> si el texto no está delante de la cámara o cae entero fuera de
        /// la pantalla: <c>WorldToScreenPoint</c> de algo que está <b>detrás</b> devuelve
        /// coordenadas espejadas y perfectamente creíbles.
        /// </para>
        /// </summary>
        private bool TryGlyphQuad(TMP_Text t, out Vector2[] quad)
        {
            quad = null;
            t.ForceMeshUpdate();
            Bounds b = t.textBounds;
            Vector3[] esquinas =
            {
                t.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0f)),
                t.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0f)),
                t.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0f)),
                t.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0f)),
            };

            Vector3[] p = esquinas.Select(c => _camera.WorldToScreenPoint(c)).ToArray();
            if (p.Any(v => v.z <= 0f))
                return false;

            quad = p.Select(v => (Vector2)v).ToArray();
            if (quad.All(v => v.x < 0f) || quad.All(v => v.x > Screen.width)
                || quad.All(v => v.y < 0f) || quad.All(v => v.y > Screen.height))
                return false;
            return true;
        }

        /// <summary>
        /// ¿Se cruzan dos cuadriláteros convexos? Por el teorema del eje separador, que es
        /// exacto estén como estén girados.
        /// <para>
        /// Cada uno se encoge <paramref name="holgura"/> píxeles hacia su centro antes de
        /// comparar: dos textos que se tocan por el borde no se están tapando.
        /// </para>
        /// </summary>
        private static bool SeCruzan(Vector2[] a, Vector2[] b, float holgura)
        {
            Vector2[] ea = Encoger(a, holgura), eb = Encoger(b, holgura);
            return !HayEjeSeparador(ea, eb) && !HayEjeSeparador(eb, ea);
        }

        private static Vector2[] Encoger(Vector2[] q, float px)
        {
            Vector2 centro = (q[0] + q[1] + q[2] + q[3]) * .25f;
            return q.Select(v =>
            {
                Vector2 d = v - centro;
                float m = d.magnitude;
                return m <= px ? centro : centro + d * ((m - px) / m);
            }).ToArray();
        }

        private static bool HayEjeSeparador(Vector2[] forma, Vector2[] otra)
        {
            for (int i = 0; i < forma.Length; i++)
            {
                Vector2 borde = forma[(i + 1) % forma.Length] - forma[i];
                Vector2 eje = new Vector2(-borde.y, borde.x);
                if (eje.sqrMagnitude < 1e-6f)
                    continue;
                float minA = forma.Min(v => Vector2.Dot(v, eje)), maxA = forma.Max(v => Vector2.Dot(v, eje));
                float minB = otra.Min(v => Vector2.Dot(v, eje)), maxB = otra.Max(v => Vector2.Dot(v, eje));
                if (maxA < minB || maxB < minA)
                    return true;
            }
            return false;
        }

        private static string Recorta(string s) => s.Length <= 42 ? s : s.Substring(0, 42) + "...";

        private static string Ruta(Transform t)
        {
            string ruta = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
                ruta = p.name + "/" + ruta;
            return ruta;
        }

        /// <summary>
        /// Nada de lo que esté encendido puede taparse entre sí. Quién está encendido lo decide
        /// el código, no el test: simular los estados a mano se equivoca en cuáles coexisten.
        /// </summary>
        private void AssertHudDoesNotOverlap()
        {
            string[] names = { "Confirmation", "ItemName", "Navigation", "StationInstruction",
                               "ReadinessDecision", "ChecklistPanel" };
            var live = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                             .Where(x => names.Contains(x.name)).ToArray();
            for (int i = 0; i < live.Length; i++)
                for (int j = i + 1; j < live.Length; j++)
                {
                    Rect a = ScreenRect(live[i]), b = ScreenRect(live[j]);
                    Assert.IsFalse(a.Overlaps(b),
                        "Se tapan en pantalla: " + live[i].name + " " + a + " y " + live[j].name + " " + b);
                }
        }

        private Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var pts = corners.Select(c => (Vector2)_camera.WorldToScreenPoint(c)).ToArray();
            float x0 = pts.Min(p => p.x), x1 = pts.Max(p => p.x);
            float y0 = pts.Min(p => p.y), y1 = pts.Max(p => p.y);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        /// <summary>El modelo tampoco puede taparle el nombre, y por la misma razón: perspectiva.</summary>
        private void AssertModelClearsName()
        {
            // Lo del pedestal puede ser un objeto o el signo de un punto sin pieza, y el signo
            // es un canvas: no tiene Renderer. Indexar renderers[0] a secas reventaba con un
            // IndexOutOfRange en cuanto el inspector dejo de fabricar la esfera blanca.
            var stage = Field<Transform>(_inspector, "_stage");
            var renderers = stage.GetComponentsInChildren<Renderer>();
            Bounds model;
            if (renderers.Length > 0)
            {
                model = renderers[0].bounds;
                foreach (var r in renderers) model.Encapsulate(r.bounds);
            }
            else
            {
                var glyph = stage.GetComponentsInChildren<TMP_Text>().FirstOrDefault();
                if (glyph == null) return;          // pedestal vacio: ya lo afirma el recorrido
                var esquinas = new Vector3[4];
                glyph.rectTransform.GetWorldCorners(esquinas);
                model = new Bounds(esquinas[0], Vector3.zero);
                foreach (var c in esquinas) model.Encapsulate(c);
            }
            var label = Field<TMP_Text>(_inspector, "_nameLabel").rectTransform;
            var corners = new Vector3[4];
            label.GetWorldCorners(corners);
            Assert.Greater(corners.Min(c => _camera.WorldToScreenPoint(c).y), Highest(model), string.Format(
                "El modelo tapa el nombre (el modelo sube a y={0:0})", Highest(model)));
        }

        /// <summary>Las 8 esquinas, no el centro: una caja con fondo proyecta más alto y más bajo que su centro.</summary>
        private static IEnumerable<float> Projected(Bounds b, Camera camera)
        {
            for (int k = 0; k < 8; k++)
                yield return camera.WorldToScreenPoint(b.center + Vector3.Scale(b.extents,
                    new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1))).y;
        }
        private float Lowest(Bounds b) => Projected(b, _camera).Min();
        private float Highest(Bounds b) => Projected(b, _camera).Max();

        /// <summary>
        /// Todo boton sale del kit del atlas, lo construya el prefab o el montador de escena.
        /// Un Image de color plano con la fuente por defecto de TMP se ve pegado al lado del
        /// modulo 0, y es justo lo que este test esta para no dejar pasar.
        /// </summary>
        private static void AssertOnBrand(Button b)
        {
            Assert.AreEqual(Selectable.Transition.SpriteSwap, b.transition, "Boton sin sprite-swap: " + b.name);
            Assert.IsNotNull((b.targetGraphic as Image)?.sprite, "Boton sin pastilla del atlas: " + b.name);
            Assert.IsNotNull(b.spriteState.pressedSprite, "Boton sin estado Pressed: " + b.name);
            Assert.IsNotNull(b.spriteState.highlightedSprite, "Boton sin estado Hover: " + b.name);
            var label = b.GetComponentInChildren<TMP_Text>(true);
            Assert.IsNotNull(label, "Boton sin etiqueta: " + b.name);
            Assert.AreNotEqual(TMP_Settings.defaultFontAsset, label.font,
                               "Boton con la fuente por defecto de TMP en vez de la del kit: " + b.name);
        }
        private IEnumerator ClickGraphic(RectTransform rect)
        {
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            AssertVisible(rect);
            var canvas = rect.GetComponentInParent<Canvas>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _camera, rect.TransformPoint(rect.rect.center));
            yield return Dispatch(point, rect.gameObject);
        }
        private IEnumerator ClickWorld(WorldObjectTaskEntity entity)
        {
            Physics.SyncTransforms();
            var collider = entity.GetComponent<Collider>();
            Vector3 p = _camera.WorldToScreenPoint(collider.bounds.center);
            // El mensaje lleva los numeros: "fuera de pantalla" a secas obliga a adivinar por
            // que lado se sale y desde donde se estaba mirando, y eso cuesta una vuelta entera
            // de PlayMode por cada hipotesis.
            Assert.IsTrue(p.z > 0 && p.x > 0 && p.x < Screen.width && p.y > 0 && p.y < Screen.height,
                string.Format(
                    "Objeto fuera de pantalla: {0}. pantalla={1}x{2} viewport=({3:0.000}, {4:0.000}, {5:0.00}) "
                    + "mundo={6} camara={7} rot={8}",
                    entity.ItemId, Screen.width, Screen.height,
                    p.x / Mathf.Max(1, Screen.width), p.y / Mathf.Max(1, Screen.height), p.z,
                    collider.bounds.center.ToString("F2"),
                    _camera.transform.position.ToString("F2"),
                    _camera.transform.eulerAngles.ToString("F0")));
            yield return Dispatch(p, entity.gameObject);
        }
        private IEnumerator Dispatch(Vector2 point, GameObject expected)
        {
            var data = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Assert.IsNotEmpty(hits, "Ningun raycast en " + expected.name);
            var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            Assert.AreEqual(expected, target, "Clic bloqueado por " + hits[0].gameObject.name);
            data.pointerCurrentRaycast = hits[0];
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
        }
        /// <summary>
        /// Cada punto de la zona del vehículo señala <b>de una sola manera</b>: con el contorno
        /// sobre la pieza real si su perfil declara una, y si no con el signo de exclamación de
        /// siempre. Nunca con las dos, y nunca con ninguna.
        /// <para>
        /// Qué puntos tienen pieza no se escribe aquí: se le pregunta al perfil del vehículo que
        /// se está recorriendo. Así el test sigue valiendo cuando Daniel entregue la maneta de
        /// freno o el faro, y falla si alguien declara una pieza que la malla no tiene.
        /// </para>
        /// </summary>
        private void AssertInspectionCues(string zoneId, string vehicleId)
        {
            var profile = _director.SelectedVehicle;
            Assert.IsNotNull(profile, "La seccion del vehiculo sin perfil elegido en " + zoneId);
            Assert.AreEqual(vehicleId, profile.VehicleContextValue, "Perfil equivocado en " + zoneId);

            foreach (var item in _director.CurrentZone.Items)
            {
                var entity = Object.FindObjectsByType<WorldObjectTaskEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(e => e.ItemId == item.ItemId);
                var highlight = entity.GetComponent<InspectionHighlight>();
                var marker = entity.GetComponent<InspectionMarker>();
                Assert.IsNotNull(highlight, "Sin InspectionHighlight: " + item.ItemId);
                Assert.IsNotNull(marker, "Sin InspectionMarker: " + item.ItemId);

                string part = profile.PartNameFor(item.ItemId);
                if (string.IsNullOrEmpty(part))
                {
                    Assert.IsNull(highlight.Outline,
                        item.ItemId + " no declara pieza y sin embargo tiene contorno.");
                    Assert.IsTrue(marker.IsVisible,
                        item.ItemId + " se quedo sin ningun aviso: ni contorno ni signo.");
                    continue;
                }

                Assert.IsNotNull(highlight.Outline,
                    item.ItemId + " declara la pieza '" + part + "' y no se delineo: "
                    + "o la malla del vehiculo no la tiene, o el escenario no la encontro.");
                Assert.AreEqual(part, highlight.Outline.transform.parent.name,
                    "El contorno de " + item.ItemId + " cuelga de la pieza equivocada.");
                Assert.IsTrue(highlight.Outline.enabled,
                    item.ItemId + " es de la zona en curso y su contorno esta apagado.");
                Assert.IsFalse(marker.IsVisible,
                    item.ItemId + " muestra el signo encima del contorno: se marca dos veces.");
                Assert.AreSame(highlight.Outline.transform.parent, entity.transform.parent,
                    "La esfera clicable de " + item.ItemId + " no se mudo a su pieza: el aprendiz "
                    + "apunta a lo que brilla y pulsa el aire.");
            }
        }

        private void AssertVisible(RectTransform rect)
        {
            Assert.IsTrue(rect.gameObject.activeInHierarchy, "UI inactiva " + rect.name);
            foreach(var group in rect.GetComponentsInParent<CanvasGroup>()) Assert.Greater(group.alpha, .9f);
            var canvas = rect.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas); Assert.IsTrue(canvas.enabled);
            Vector3 p = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? rect.TransformPoint(rect.rect.center) : _camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
            Assert.IsTrue((canvas.renderMode == RenderMode.ScreenSpaceOverlay || p.z > 0) && p.x > 0 && p.x < Screen.width && p.y > 0 && p.y < Screen.height, "UI fuera de pantalla: " + rect.name + " " + p);
        }
        /// <summary>
        /// Captura el frame tal cual sale por pantalla. Antes renderizaba solo la camara
        /// principal, y desde que la UI vive en una camara de overlay del stack de URP eso
        /// dejaba fuera toda la interfaz: la captura mentia justo sobre lo que hay que revisar.
        /// </summary>
        private IEnumerator Capture(string name)
        {
            const string folder = "Docs/verification/module01";
            System.IO.Directory.CreateDirectory(folder);
            // CaptureScreenshot a archivo, no CaptureScreenshotAsTexture: la variante en memoria
            // devolvia el frame sin la UI desde que esta vive en la camara de overlay del stack.
            string path = folder + "/" + name + _captureTag + ".png";
            // Exigir escritura NUEVA, no solo que el archivo exista: si el borrado falla, un
            // File.Exists sobre la captura vieja da verde y la evidencia queda caducada.
            System.DateTime before = System.IO.File.Exists(path)
                ? System.IO.File.GetLastWriteTimeUtc(path)
                : System.DateTime.MinValue;
            ScreenCapture.CaptureScreenshot(path);
            float until = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < until
                   && (!System.IO.File.Exists(path) || System.IO.File.GetLastWriteTimeUtc(path) <= before))
                yield return null;
            Assert.Greater(System.IO.File.GetLastWriteTimeUtc(path), before,
                "La captura '" + name + _captureTag + "' no se reescribio: la evidencia seria la de la corrida anterior.");
        }
        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
        private static GameObject FindIncludingInactive(string name) =>
            Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(g => g.name == name);
        private static void SetField(object o, string name, object value) =>
            o.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(o, value);
        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(o);
        /// <summary>
        /// Elementos cuyos terminos todavia no estan en I2, con su motivo. La lista esta aqui a
        /// proposito y no se desactiva la comprobacion: lo que no esta traducido sale en pantalla
        /// como la clave cruda, y eso no puede llegar a una demo sin que nadie se entere.
        /// <para>
        /// Charlie, 2026-10-14: «sobre la localizacion, yo me encargo despues». Los tres
        /// pantalones entraron al catalogo ese dia; sus textos van en la hoja del Drive.
        /// <b>Vaciar esta lista al ponerlos.</b> El test avisa si alguno ya esta traducido.
        /// </para>
        /// </summary>
        private static readonly string[] SinTraducirTodavia =
        {
            "trousers_fitted", "trousers_loose", "trousers_tech",
            // Del v9. Las claves son Module1/Item_ElbowPads y Module1/Item_KneePads, con su
            // _Risk, en ingles y espanol. Las pone Charlie en el Drive.
            "elbow_pads", "knee_pads",
        };

        private static bool PendienteDeTraducir(string itemId) =>
            System.Array.IndexOf(SinTraducirTodavia, itemId) >= 0;

        private static IEnumerator WaitFor(System.Func<bool> condition, string label, float seconds = 5)
        { yield return WaitFor(condition, () => label, seconds); }
        /// <summary>Mensaje diferido: se arma al expirar, con el estado real en vez de un "Timeout" pelado.</summary>
        private static IEnumerator WaitFor(System.Func<bool> condition, System.Func<string> label, float seconds = 5)
        { float until = Time.realtimeSinceStartup + seconds; while (!condition() && Time.realtimeSinceStartup < until) yield return null; Assert.IsTrue(condition(), "Timeout: " + label()); }
        private void Collect(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) if (message.Contains("Module01") || stack.Contains("RideSafe.Module01")) _problems.Add(message); }
    }
}






