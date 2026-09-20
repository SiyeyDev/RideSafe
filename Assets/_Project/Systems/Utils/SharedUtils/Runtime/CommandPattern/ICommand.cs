using System;
public interface ICommand 
{
    public void Execute(Action onComplete);

    public void Exit();
}