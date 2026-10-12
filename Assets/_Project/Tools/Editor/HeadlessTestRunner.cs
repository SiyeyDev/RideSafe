using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace RideSafe.Tools.Editor
{
    /// <summary>
    /// Corre tests de EditMode desde un script y deja el resultado en un archivo,
    /// para poder verificar sin abrir la ventana del Test Runner.
    /// <para>
    /// El callback vive en un ScriptableObject con <c>hideFlags</c> de no guardar,
    /// porque una corrida de EditMode recarga el dominio y un objeto normal no
    /// sobreviviria para escribir el resultado.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    public class HeadlessTestRunner : ScriptableObject, ICallbacks
    {
        public const string ResultPath = "Temp/ridesafe-tests.txt";

        /// <summary>
        /// A PlayMode run reloads the domain on the way in and out, which drops callbacks
        /// registered by the code that started it. While a run is pending (result file says
        /// RUNNING) the callback is registered again on every load, so the final domain still
        /// writes the result.
        /// </summary>
        static HeadlessTestRunner()
        {
            if (!File.Exists(ResultPath) || !File.ReadAllText(ResultPath).StartsWith("RUNNING"))
                return;
            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _active = ScriptableObject.CreateInstance<HeadlessTestRunner>();
            _active.hideFlags = HideFlags.HideAndDontSave;
            api.RegisterCallbacks(_active);
        }

        private static HeadlessTestRunner _active;

        /// <summary>Lanza los tests de EditMode de las assemblies indicadas.</summary>
        public static void Run(string[] assemblyNames) => Run(assemblyNames, TestMode.EditMode);

        /// <summary>Lanza los tests de PlayMode de las assemblies indicadas.</summary>
        public static void RunPlayMode(string[] assemblyNames) => Run(assemblyNames, TestMode.PlayMode);

        private static void Run(string[] assemblyNames, TestMode mode)
        {
            if (!PrepareEditor())
                return;

            File.WriteAllText(ResultPath, "RUNNING\n");

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _active = ScriptableObject.CreateInstance<HeadlessTestRunner>();
            _active.hideFlags = HideFlags.HideAndDontSave;
            api.RegisterCallbacks(_active);

            Filter filter = new Filter { testMode = mode };
            if (assemblyNames != null && assemblyNames.Length > 0)
                filter.assemblyNames = assemblyNames;

            api.Execute(new ExecutionSettings(filter));
        }

        /// <summary>
        /// Deja el editor en condiciones de correr tests sin abrir un diálogo.
        /// <para>
        /// <b>Con una escena sin guardar, el Test Framework abre un modal
        /// «Scene(s) Have Been Modified» antes de empezar.</b> Delante de una persona es una
        /// pregunta; desde un script o desde el relay de MCP es un bloqueo indefinido: la
        /// ventana principal queda deshabilitada, el hilo principal no avanza, y ni los tests
        /// corren ni responde ninguna otra orden hasta que alguien la cierre a mano. Pasó, y
        /// costó media hora de reloj averiguar que era eso y no un cuelgue.
        /// </para>
        /// <para>
        /// Se guarda sin preguntar porque un test no debe poder perder trabajo, y guardar es
        /// lo que habría contestado cualquiera en ese diálogo.
        /// </para>
        /// </summary>
        private static bool PrepareEditor()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError(
                    "[Tests] El editor esta en Play. Sal de Play antes de correr tests: la tarea " +
                    "previa del framework revienta y tiñe de rojo un test que no tiene nada que ver.");
                return false;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogError(
                    "[Tests] Sigue compilando. Compilar y correr en la misma llamada no funciona: " +
                    "los tests correrian contra las assemblies viejas.");
                return false;
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            return true;
        }

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DONE");
            sb.AppendLine($"passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount}");
            AppendFailures(sb, result);
            File.WriteAllText(ResultPath, sb.ToString());
            _active = null;
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) { }

        private static void AppendFailures(StringBuilder sb, ITestResultAdaptor result)
        {
            if (result.TestStatus == TestStatus.Failed && !result.HasChildren)
            {
                sb.AppendLine($"FAIL  {result.FullName}");
                if (!string.IsNullOrEmpty(result.Message))
                    sb.AppendLine("      " + result.Message.Replace("\n", "\n      "));
            }

            if (result.Children == null)
                return;

            foreach (ITestResultAdaptor child in result.Children)
                AppendFailures(sb, child);
        }
    }
}
