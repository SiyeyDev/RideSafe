using UnityEditor;

[InitializeOnLoad]
internal static class DialogueClipListBridgeInstaller
{
    static DialogueClipListBridgeInstaller()
    {
        DialogueClipListBridge.GetClipsInFolder = folder => FolderUtilities.GetAssetWith<UnityEngine.AudioClip>(folder, "t:AudioClip");
    }
}
