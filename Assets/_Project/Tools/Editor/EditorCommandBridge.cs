using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RideSafe.Tools.Editor
{
    /// <summary>
    /// Lets an external tool drive the editor that already has the project open, where a
    /// batchmode run is impossible because the project is locked.
    /// <para>
    /// Polls <see cref="CommandPath"/>. One command per file, consumed on read:
    /// <c>refresh</c>, <c>play</c>, <c>stop</c>, <c>editmode Asm1;Asm2</c>, <c>playmode Asm1</c> (results in
    /// <see cref="HeadlessTestRunner.ResultPath"/>) or <c>exec Namespace.Type.Method</c> for a
    /// static, parameterless editor method (result in <see cref="ResultPath"/>).
    /// </para>
    /// <para>Editor only, inert unless the file exists, and the file lives under Temp/.</para>
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorCommandBridge
    {
        public const string CommandPath = "Temp/ridesafe-cmd.txt";
        public const string ResultPath = "Temp/ridesafe-cmd-result.txt";

        private const double PollSeconds = 0.5;
        private static double _nextPoll;

        static EditorCommandBridge()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

            // While playing only exec/stop run (diagnostics); everything else waits for edit mode.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath))
                return;
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            string pending = File.ReadAllText(CommandPath).Trim();
            if (playing && !(pending.StartsWith("exec ") || pending == "stop"))
                return;

            string command = File.ReadAllText(CommandPath).Trim();
            File.Delete(CommandPath);
            Execute(command);
        }

        private static void Execute(string command)
        {
            int space = command.IndexOf(' ');
            string verb = space < 0 ? command : command.Substring(0, space);
            string argument = space < 0 ? string.Empty : command.Substring(space + 1).Trim();
            string[] assemblies = argument.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                switch (verb)
                {
                    case "play":
                        EditorApplication.EnterPlaymode();
                        File.WriteAllText(ResultPath, "DONE play\n");
                        break;
                    case "stop":
                        EditorApplication.ExitPlaymode();
                        File.WriteAllText(ResultPath, "DONE stop\n");
                        break;
                    case "refresh":
                        AssetDatabase.Refresh();
                        File.WriteAllText(ResultPath, "DONE refresh\n");
                        break;
                    case "editmode":
                        HeadlessTestRunner.Run(assemblies);
                        break;
                    case "playmode":
                        HeadlessTestRunner.RunPlayMode(assemblies);
                        break;
                    case "exec":
                        Invoke(argument);
                        File.WriteAllText(ResultPath, "DONE exec " + argument + "\n");
                        break;
                    default:
                        File.WriteAllText(ResultPath, "ERROR unknown command '" + command + "'\n");
                        break;
                }
            }
            catch (Exception exception)
            {
                Exception root = exception is TargetInvocationException && exception.InnerException != null
                    ? exception.InnerException
                    : exception;
                File.WriteAllText(ResultPath, "ERROR " + root + "\n");
                Debug.LogException(root);
            }
        }

        private static void Invoke(string qualifiedMethod)
        {
            int dot = qualifiedMethod.LastIndexOf('.');
            string typeName = qualifiedMethod.Substring(0, dot);
            string methodName = qualifiedMethod.Substring(dot + 1);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type == null)
                    continue;
                MethodInfo method = type.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method == null)
                    throw new MissingMethodException(typeName, methodName);
                method.Invoke(null, null);
                return;
            }
            throw new TypeLoadException("Type not found: " + typeName);
        }
    }
}
