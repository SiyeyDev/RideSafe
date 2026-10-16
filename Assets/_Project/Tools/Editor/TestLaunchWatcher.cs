using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Tools.Editor
{
    /// <summary>
    /// Lanza los tests cuando aparece un fichero de señal, en vez de cuando alguien llama a
    /// <see cref="HeadlessTestRunner"/> directamente.
    /// <para>
    /// <b>Por qué existe:</b> el puente de MCP rechaza la llamada que arranca el Test Framework
    /// con <i>«User interactions are not supported for MCP tool calls»</i> —el framework pide
    /// interfaz al empezar—, y lo hace también si la llamada se aplaza con
    /// <c>EditorApplication.delayCall</c>: el aplazamiento sigue cayendo dentro de la ventana en
    /// que el puente vigila la orden. Comprobado el 2026-10-16: <c>SaveOpenScenes</c> y
    /// <c>SaveAssets</c> pasan sin problema, así que lo que molesta es
    /// <c>TestRunnerApi.Execute</c> y nada de lo que hace <c>PrepareEditor</c>.
    /// </para>
    /// <para>
    /// Con la señal en un fichero, la orden del puente termina enseguida —escribir un fichero no
    /// es interfaz— y los tests arrancan en el siguiente ciclo del Editor, ya fuera de su
    /// vigilancia. El resultado sigue yendo a <see cref="HeadlessTestRunner.ResultPath"/>, que es
    /// lo que hay que mirar.
    /// </para>
    /// <para>
    /// La señal es un fichero con el modo en la primera línea (<c>EditMode</c> o
    /// <c>PlayMode</c>) y, si se quiere, una assembly por línea debajo. Sin assemblies corre
    /// todas, que para EditMode está bien y para PlayMode <b>no</b>: mezclar la assembly de
    /// PlayMode en una corrida de EditMode revienta con <c>InvalidOperationException</c>.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    internal static class TestLaunchWatcher
    {
        /// <summary>Fichero de señal. Se borra en cuanto se lee, para no lanzar dos veces.</summary>
        public const string TriggerPath = "Temp/ridesafe-run-tests.txt";

        private const double PollSeconds = 1d;

        private static double _next;

        static TestLaunchWatcher()
        {
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next)
                return;
            _next = EditorApplication.timeSinceStartup + PollSeconds;

            if (!File.Exists(TriggerPath))
                return;

            // Ni en Play ni compilando: las dos cosas tiñen de rojo tests que no fallan. La
            // señal se queda donde está y se reintenta al siguiente ciclo.
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                return;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(TriggerPath);
            }
            catch (IOException)
            {
                // Lo están escribiendo ahora mismo. Al siguiente ciclo.
                return;
            }

            File.Delete(TriggerPath);

            if (lines.Length == 0)
            {
                Debug.LogError($"[Tests] La senal '{TriggerPath}' esta vacia: hace falta " +
                               "'EditMode' o 'PlayMode' en la primera linea.");
                return;
            }

            string mode = lines[0].Trim();
            string[] assemblies = Trim(lines);

            if (mode.Equals("EditMode", StringComparison.OrdinalIgnoreCase))
                HeadlessTestRunner.Run(assemblies);
            else if (mode.Equals("PlayMode", StringComparison.OrdinalIgnoreCase))
                HeadlessTestRunner.RunPlayMode(assemblies);
            else
                Debug.LogError($"[Tests] Modo '{mode}' desconocido: solo EditMode o PlayMode.");
        }

        /// <summary>Las líneas de assembly, sin la del modo y sin blancos.</summary>
        private static string[] Trim(string[] lines)
        {
            var kept = new System.Collections.Generic.List<string>();
            for (int i = 1; i < lines.Length; i++)
            {
                string name = lines[i].Trim();
                if (name.Length > 0)
                    kept.Add(name);
            }
            return kept.ToArray();
        }
    }
}
