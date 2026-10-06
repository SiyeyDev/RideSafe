using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RideSafe.TaskSequence;
using EntityId = RideSafe.TaskSequence.EntityId;

namespace RideSafe.Module01.Tests
{
    /// <summary>
    /// Recorre el módulo 1 completo en la escena real, con la escena corriendo.
    /// <para>
    /// Los tests de EditMode prueban la lógica con objetos en memoria. Esto prueba lo
    /// que ninguno de ellos puede: que la escena tenga las entidades donde el director
    /// las busca, que el clic llegue de verdad al objeto 3D, y que el módulo avance de
    /// la primera zona al reporte final sin quedarse esperando.
    /// </para>
    /// </summary>
    public class Module01SliceTests
    {
        private const string SceneName = "RideSafe_Garaje_v5";

        /// <summary>
        /// El resultado lo escribe el propio test. Los callbacks del TestRunnerApi no
        /// sobreviven a la recarga de dominio que ocurre al salir de Play, asi que
        /// preguntarle al runner desde fuera no sirve para PlayMode.
        /// </summary>
        private const string ResultPath = "Temp/ridesafe-playmode.txt";

        private readonly List<string> _problems = new List<string>();

        private Module01Director _director;
        private ItemInspector _inspector;
        private TaskContextService _entities;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Sin gafas, AutoHand y OpenXR registran errores que no son nuestros.
            // Los propios se recogen aparte y se afirman al final.
            LogAssert.ignoreFailingMessages = true;
            _problems.Clear();
            Application.logMessageReceived += Collect;

            SceneManager.LoadScene(SceneName);
            yield return null;
            yield return null;

            _director = Object.FindAnyObjectByType<Module01Director>();
            Assert.IsNotNull(_director, "No hay Module01Director en la escena.");

            _inspector = Object.FindAnyObjectByType<ItemInspector>();
            Assert.IsNotNull(_inspector, "No hay ItemInspector en la escena.");

            _entities = Cachacos.ServiceLocator.Instance.RequestService<TaskContextService>();
            Assert.IsNotNull(_entities, "No hay TaskContextService: nadie registro las entidades.");
        }

        [OneTimeSetUp]
        public void StartReport() =>
            System.IO.File.WriteAllText(ResultPath, "PLAYMODE RUNNING\n");

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= Collect;
            LogAssert.ignoreFailingMessages = false;

            string name = TestContext.CurrentContext.Test.Name;
            string status = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            string message = TestContext.CurrentContext.Result.Message;
            System.IO.File.AppendAllText(ResultPath,
                $"{status}  {name}\n" +
                (string.IsNullOrEmpty(message) ? "" : "    " + message.Replace("\n", "\n    ") + "\n"));

            yield return null;
        }

        [OneTimeTearDown]
        public void EndReport() => System.IO.File.AppendAllText(ResultPath, "PLAYMODE DONE\n");

        [UnityTest]
        public IEnumerator El_modulo_recorre_de_la_primera_zona_al_reporte_final()
        {
            SelectionReport personal = null;
            SelectionReport vehicle = null;
            _director.PersonalSectionCompleted += r => personal = r;
            _director.VehicleSectionCompleted += r => vehicle = r;

            _director.Begin();
            yield return null;

            Assert.IsNotNull(_director.CurrentZone, "El director no entro en ninguna zona.");
            Assert.AreEqual("head", _director.CurrentZone.ZoneId, "Deberia empezar por la zona de cabeza.");

            // Recorre todas las zonas tomando lo correcto y rechazando lo de riesgo.
            int guard = 0;
            while (!_director.IsFinished && guard++ < 50)
            {
                ZoneSO zone = _director.CurrentZone;
                Assert.IsNotNull(zone, "Zona nula antes de terminar.");

                foreach (SafetyItemSO item in new List<SafetyItemSO>(zone.Items))
                {
                    yield return Click(zone.ZoneId, item.ItemId);

                    Assert.IsTrue(_inspector.IsPresenting,
                        $"Al confirmar '{item.ItemId}' el inspector no abrio la presentacion.");

                    bool take = item.Category == SafetyItemCategory.Core
                                || item.Category == SafetyItemCategory.ConditionDependent;
                    if (take) _inspector.Accept(); else _inspector.Decline();
                    yield return null;
                }
            }

            Assert.IsTrue(_director.IsFinished, "El modulo no llego al final: alguna zona no cerro.");
            Assert.IsNotNull(personal, "No se emitio el reporte de la seccion personal.");
            Assert.IsNotNull(vehicle, "No se emitio el reporte de la seccion del vehiculo.");

            CollectionAssert.IsEmpty(personal.InappropriateSelected,
                "Se rechazaron todos los elementos de riesgo, no deberia haber ninguno elegido.");
            CollectionAssert.IsEmpty(personal.CoreOmitted,
                "Se acepto todo el nucleo, no deberia faltar ninguno.");
            Assert.IsFalse(personal.HasCriticalProblem);

            CollectionAssert.IsEmpty(vehicle.CoreOmitted,
                "Se inspeccionaron todos los puntos del vehiculo.");

            CollectionAssert.IsEmpty(_problems, string.Join("\n", _problems));
        }

        [UnityTest]
        public IEnumerator Rechazar_el_casco_correcto_lo_reporta_como_omision_critica()
        {
            SelectionReport personal = null;
            _director.PersonalSectionCompleted += r => personal = r;

            _director.Begin();
            yield return null;

            int guard = 0;
            while (personal == null && guard++ < 50)
            {
                ZoneSO zone = _director.CurrentZone;
                if (zone == null) break;

                foreach (SafetyItemSO item in new List<SafetyItemSO>(zone.Items))
                {
                    yield return Click(zone.ZoneId, item.ItemId);
                    if (!_inspector.IsPresenting) continue;

                    bool take = item.ItemId != "helmet_ok"
                                && (item.Category == SafetyItemCategory.Core
                                    || item.Category == SafetyItemCategory.ConditionDependent);
                    if (take) _inspector.Accept(); else _inspector.Decline();
                    yield return null;
                }
            }

            Assert.IsNotNull(personal, "No se emitio el reporte personal.");
            CollectionAssert.Contains(personal.CoreOmitted, "helmet_ok");
            Assert.IsTrue(personal.HasCriticalProblem,
                "Omitir el casco correcto tiene que marcarse como problema critico.");
        }

        /// <summary>Clic real sobre el objeto 3D, por la misma via que el mouse.</summary>
        private IEnumerator Click(string zoneId, string itemId)
        {
            EntityId id = new EntityId($"module01.{zoneId}.{itemId}");
            Assert.IsTrue(_entities.TryGetEntity(id, out WorldObjectTaskEntity entity),
                $"La escena no tiene la entidad {id}. Corre el menu de montaje.");

            ExecuteEvents.Execute(entity.gameObject,
                new PointerEventData(EventSystem.current),
                ExecuteEvents.pointerClickHandler);
            yield return null;
        }

        private void Collect(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Log || type == LogType.Warning)
                return;
            if (message.Contains("[Module01]") || stackTrace.Contains("RideSafe.Module01"))
                _problems.Add(type + ": " + message);
        }
    }
}
