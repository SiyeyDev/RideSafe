using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueClipListBridge
{
    public static Func<string, List<AudioClip>> GetClipsInFolder;
}
