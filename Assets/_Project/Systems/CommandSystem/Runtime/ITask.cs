using System;

public interface ITask 
{
    public IdTask ID { get;}
    public bool IsActiveTask { get; set; }

    public event Action<bool, IdTask> OnTaskCompleted;
    public void Init() { }
}
