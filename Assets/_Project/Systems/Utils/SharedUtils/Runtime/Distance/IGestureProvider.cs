using System;

public interface IGestureProvider 
{
    public Action OnStartGesture {  get; set; }
    public Action OnStopGesture { get; set; }
}
