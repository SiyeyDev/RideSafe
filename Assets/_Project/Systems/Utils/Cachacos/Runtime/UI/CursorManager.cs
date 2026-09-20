using UnityEngine;

public class CursorManager 
{
    private static int _amountOfLocks = 0;
    public static bool UnlockCursor
    {
        get => _amountOfLocks > 0;
        set
        {
            if (value)
                _amountOfLocks++;
            else
                _amountOfLocks--;
            if(_amountOfLocks < 0)
                _amountOfLocks = 0;
            Debug.Log($"CURSOR mager {_amountOfLocks}");
            Cursor.lockState = UnlockCursor ? CursorLockMode.Confined : CursorLockMode.Locked;
            Cursor.visible = UnlockCursor;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetOnSceneLoad()
    {
        _amountOfLocks = 0;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }
}
