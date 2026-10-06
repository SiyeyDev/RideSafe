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
    public class HeadlessTestRunner : ScriptableObject, ICallbacks
    {
        public const string ResultPath = "Temp/ridesafe-tests.txt";

        private static HeadlessTestRunner _active;

        /// <summary>Lanza los tests de EditMode de las assemblies indicadas.</summary>
        public static void Run(string[] assemblyNames)
        {
            File.WriteAllText(ResultPath, "RUNNING\n");

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _active = ScriptableObject.CreateInstance<HeadlessTestRunner>();
            _active.hideFlags = HideFlags.HideAndDontSave;
            api.RegisterCallbacks(_active);

            Filter filter = new Filter { testMode = TestMode.EditMode };
            if (assemblyNames != null && assemblyNames.Length > 0)
                filter.assemblyNames = assemblyNames;

            api.Execute(new ExecutionSettings(filter));
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
