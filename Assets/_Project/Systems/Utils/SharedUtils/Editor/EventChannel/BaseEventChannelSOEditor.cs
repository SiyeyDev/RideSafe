using Cachacos;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BaseEventChannelSO<>), true)]
public class BaseEventChannelSOEditor<T> : Editor
{
    private BaseEventChannelSO<T> _eventChannel;

    private void OnEnable()
    {
        if (target is BaseEventChannelSO<T> Test)
            Debug.Log(Test);
        _eventChannel = target as BaseEventChannelSO<T>;
    }
    public override void OnInspectorGUI()
    {
        GUILayout.BeginVertical("box", EditorGUIUtils.GetButtonStyle(Color.black, border: 2));
        GUILayout.Space(4);
        HashSet<MonoBehaviour> listeners = GetListeners();
        if (listeners.Count == 0)
        {
            GUILayout.Label("There is not Listeners", EditorGUIUtils.GetLabelStyle(Color.white));
            GUILayout.EndVertical();
            return;
        }
        GUILayout.Label("Listeners", EditorGUIUtils.GetLabelStyle(Color.white));

        GUILayout.Space(4);
        GUILayout.EndVertical();

        if (GUILayout.Button($"Raise Event", EditorGUIUtils.GetButtonStyle(Color.cyan)))
            _eventChannel.RaiseEvent(default(T));
    }

    private HashSet<MonoBehaviour> GetListeners()
    {

        HashSet<MonoBehaviour> listeners = new HashSet<MonoBehaviour>();
        if (_eventChannel == null || _eventChannel.OnEventRaised == null)
            return listeners;
        Debug.Log("Readinggg");
        Delegate[] delegateSubscribers = _eventChannel.OnEventRaised.GetInvocationList();

        foreach (Delegate subscriber in delegateSubscribers)
        {
            MonoBehaviour componentListener = subscriber.Target as MonoBehaviour;

            if (!listeners.Contains(componentListener))
                listeners.Add(componentListener);
        }
        return listeners;
    }
}
