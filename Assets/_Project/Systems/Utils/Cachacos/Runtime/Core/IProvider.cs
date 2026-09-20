using System;

public interface IProvider 
{
    public event Action<bool> OnChangeState;
}
