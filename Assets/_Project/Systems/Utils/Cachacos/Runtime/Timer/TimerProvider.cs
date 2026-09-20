using UnityEngine;

public class TimerProvider : MonoBehaviour
{
    [SerializeField] private TimerMonoBehaviour _timer;

    private void Awake()
    {
        if (_timer == null)
            _timer = GetComponent<TimerMonoBehaviour>();
        if (_timer == null)
            throw new System.Exception($"Theres is not {typeof(TimerMonoBehaviour)} assigned");
    }

    public ITimer GetTimer() => _timer;
}
