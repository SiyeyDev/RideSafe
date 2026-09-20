using Sirenix.OdinInspector;
using StepCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class DialogueStepCommand : StepCommandClass, IParallelStepCommand
{
    [Header("DialogueStep Settings")]
    [SerializeField] private float _delay;
    [SerializeField] private bool _canReplay;
    [FoldoutGroup("$GetFoldoutGroupName")]
    [ValueDropdown("GetAudios")]
    [SerializeField] private string _clipName;
#if UNITY_EDITOR
    [FoldoutGroup("$GetFoldoutGroupName")]
    [FolderPath()]
    [SerializeField] private string _routhFolder;
#endif

    [SerializeField] private AudioSource _source;
    [SerializeField] private bool _isGeneralAudio;
    private Coroutine _coroutine;
    private AudioClip _clip;
    private MonoBehaviour _caller;
    private bool _played;

    #region ICommand Methods
    public override IStepCommand Initialize()
    {
        Debug.Log($"GUIDE Play aduio {_clipName} for AudioSOurce {_source.name}");
        if (_clip == null)
            _clip = AudioTranslateBridge.GetAudioClip(_clipName, _isGeneralAudio);
        _caller = CoroutineCaller.Instance;
        _played = false;
        return base.Initialize();
    }
    public override void Execute(Action<bool, IStepCommand> onComplete)
    {
        if (_played)
        {
            base.Execute(onComplete);
            CoroutineCaller.Instance.WaitForNextFrame(() => Complete(true, this));
            return;
        }
        if (!_canReplay)
            onComplete += (right, stepCOmmand) => _played = true;
        base.Execute(onComplete);
        if (_delay == 0)
        {
            Play(Complete);
            return;
        }
        _coroutine = _caller.CoroutineExecuteActionAfter(() => Play(Complete), _delay);
    }
    public override void Exit()
    {
        if (_coroutine != null)
            _caller.StopCoroutine(_coroutine);

        _source.Stop();
    }
    #endregion
    private void Play(Action<bool, IStepCommand> onComplete)
    {
        Debug.Log($"Player audio clip {_clip.name} with {_clip.length}");
        _source.clip = _clip;
        _source.Play();
        if (onComplete != null)
            _coroutine = _caller.CoroutineUpdateUntil(() => _source.isPlaying, () => onComplete.Invoke(true, this));
    }
    #region IParallelComand Methods
    public bool ActiveParrallel { get; set; }
    public bool ForceQuit() => false;
    public bool IsExecuted() => !_source.isPlaying;
    public void CompleteParallel() { }
    #endregion

    #region Odin Drawer
#if UNITY_EDITOR
    private string GetFoldoutGroupName() => string.IsNullOrEmpty(_clipName) ? "Select Clip" : $"Clip: {_clipName}";
    private IEnumerable<string> GetAudios()
    {
        if (string.IsNullOrEmpty(_routhFolder) || !System.IO.Directory.Exists(_routhFolder))
            return new[] { "No clips found (invalid folder)" };
        List<AudioClip> clips = DialogueClipListBridge.GetClipsInFolder(_routhFolder);
        if (clips == null || clips.Count == 0)
            return new[] { "No audio clips in folder" };
        return clips.Select(clip => clip.name);
    }
#endif
    #endregion
}
