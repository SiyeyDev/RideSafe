#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using StepCommand.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace StepCommand.Editor
{
    public class StepComandDebugEditor : OdinMenuEditorWindow
    {
        [OdinSerialize] public IStepCommand MyInterface;

        [MenuItem("Tools/Cachacos/StepCommandDebug")]
        public static void OpenWindow()
        {
            GetWindow<StepComandDebugEditor>().Show();
        }
        protected override void OnEnable()
        {
            EditorApplication.playModeStateChanged -= UpdateWindow;
            base.OnEnable();
        }
        protected override void OnDisable()
        {
            EditorApplication.playModeStateChanged -= UpdateWindow;
            base.OnDisable();
        }
        private void UpdateWindow(PlayModeStateChange playmodeStateChange)
        {
            ForceMenuTreeRebuild();
            Repaint();
        }
        private EditorStepCommandNode[] _commands;
        public void SetCommands()
        {
            MonoBehaviour[] allMonoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);
            HashSet<EditorStepCommandNode> nodes = new HashSet<EditorStepCommandNode>();
            foreach (MonoBehaviour monobehaviour in allMonoBehaviours)
            {
                if (monobehaviour == null)
                    continue;
                if (monobehaviour is not IStepInitialize stepInit)
                    continue;
                Type type = monobehaviour.GetType();
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (FieldInfo field in fields)
                {
                    if (field == null)
                        continue;
                    Type fieldType = field.FieldType;
                    bool isDirectMatch = fieldType == typeof(StepCommandWrapper);
                    bool isArrayMatch = fieldType == typeof(StepCommandWrapper[]);
                    bool isListMatch = fieldType.IsGenericType &&
                                       fieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                                       fieldType.GetGenericArguments()[0] == typeof(StepCommandWrapper);
                    if (!isDirectMatch && !isArrayMatch && !isListMatch)
                        return;
                    nodes.Add(new EditorStepInitializedNode(monobehaviour).Initialize());
                    break;
                }
            }
            _commands = nodes.ToArray();
        }
        #region OdinMenuEditorWindow Methods
        protected override OdinMenuTree BuildMenuTree()
        {
            SetCommands();
            if (_commands == null && _commands.Length == 0)
            {
                StepCommandDebugUtilities.DebugLog($"There is not Command in the Scene the window will Close");
                Close();
                return null;
            }

            OdinMenuTree tree = new OdinMenuTree(true);
            tree.Config.DrawSearchToolbar = false;
            AddNodes(ref tree, "", _commands);
            tree.EnumerateTree().Where(item => item.Value is EditorStepCommandNode).ForEach(SubscribeItem);
            return tree;
        }
        protected override void OnBeginDrawEditors()
        {
            return;
            OdinMenuItem selected = this.MenuTree.Selection.FirstOrDefault();
            if (selected == null)
                return;
            float toolbarHeight = 24;
            SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);
            DrawStepCommandToolbar(selected);
            SirenixEditorGUI.EndHorizontalToolbar();
        }
        protected override void OnImGUI()
        {
            if (GUILayout.Button("Refresh", GUILayout.Height(25)))
                ForceMenuTreeRebuild();
            base.OnImGUI();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable Debug for All", GUILayout.Height(25)))
            {
                StepCommandDebugUtilities.DebugLog("Enabled debug for all");
                SetDebugForAll(_commands, true);
            }
            if (GUILayout.Button("Disable Debug for All", GUILayout.Height(25)))
            {
                StepCommandDebugUtilities.DebugLog("Disabled debug for all");
                SetDebugForAll(_commands, false);
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        private void AddNodes(ref OdinMenuTree odinMenuTree, string baseName, IList<EditorStepCommandNode> nodes)
        {
            foreach (EditorStepCommandNode node in nodes)
            {
                if (node.TargetComponent == null)
                    continue;
                string name = $"{baseName}/{node.GetName()}";
                SdfIconType icon = SdfIconType.Circle;
                Color iconColor = Color.gray;
                if (node.HasDebug())
                {
                    iconColor = node.debug.HasDebugAttacched() ? Color.green : Color.red;
                    icon = node.debug.HasDebugAttacched() ? SdfIconType.CheckCircleFill : SdfIconType.XCircleFill;
                }
                UpdateIcon(odinMenuTree.Add(name, node).Last(), icon, iconColor);
                if (node.ChildrenNodes != null)
                    AddNodes(ref odinMenuTree, name, node.ChildrenNodes);
            }
        }
        private void SubscribeItem(OdinMenuItem item) => item.OnDrawItem += UpdateItem;
        private void UpdateItem(OdinMenuItem item)
        {
            if (item.Value == null)
                return;
            EditorStepCommandNode editorStepCommandNode = item.Value as EditorStepCommandNode;
            if (editorStepCommandNode.TargetComponent == null)
                return;
            Color color = editorStepCommandNode.TargetComponent.enabled ? Color.green : Color.red;
            OdinMenuStyle newStyle = new OdinMenuStyle();
            newStyle.DefaultLabelStyle = new GUIStyle(item.Style.DefaultLabelStyle);
            newStyle.DefaultLabelStyle.normal.textColor = color;
            newStyle.SelectedLabelStyle = new GUIStyle(item.Style.SelectedLabelStyle);
            newStyle.SelectedLabelStyle.normal.textColor = color;
            item.Style = newStyle;

            if (editorStepCommandNode.HasDebug())
            {
                SdfIconType icon = editorStepCommandNode.debug.HasDebugAttacched() ? SdfIconType.CheckCircleFill : SdfIconType.XCircleFill;
                Color iconColor = editorStepCommandNode.debug.HasDebugAttacched() ? Color.green : Color.red;
                UpdateIcon(item, icon, iconColor);
            }
        }
        private void UpdateIcon(OdinMenuItem item, SdfIconType sdfIcon, Color color)
        {
            item.SdfIcon = sdfIcon;
            item.SdfIconColor = color;
        }

        private void SetDebug(EditorStepCommandNode node, bool state)
        {
            if (!node.HasDebug())
                return;
            StepCommandDebugUtilities.DebugLog($"Trying set  debug for {node} to {state}");
            if (state)
                node.debug.AddDebug();
            else
                node.debug.RemoveDebug();
        }
        private void SetDebugForAll(IList<EditorStepCommandNode> nodes, bool state)
        {
            foreach (EditorStepCommandNode node in nodes)
            {

                SetDebug(node, state);
                if (node.ChildrenNodes != null)
                    SetDebugForAll(node.ChildrenNodes, state);
            }
        }
        private void DrawStepCommandToolbar(OdinMenuItem selected)
        {
            EditorStepCommandNode currentNode = selected.Value as EditorStepCommandNode;
            if (selected != null)
            {
                GUIStyle centeredBoldLabel = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                GUILayout.Label(currentNode.GetName(), centeredBoldLabel);
            }
            if (currentNode.HasDebug())
            {
                bool debugAttached = currentNode.debug.HasDebugAttacched();
                GUIContent buttonContent = new GUIContent(debugAttached ? "    Disable Current Debug    " : "    Enable Current Debug    ");
                if (SirenixEditorGUI.ToolbarButton(buttonContent))
                    SetDebug(currentNode, !debugAttached);
            }
        }
    }
}
#endif