using System;
using System.Collections.Generic;
using UnityEngine;

public class CommandSequence
{
    private Queue<ICommand> _commands = new Queue<ICommand>();
    private Action onComplete;
    private ICommand _currentCommand;

    private CommandManager _commandManager;
    public CommandSequence AddCommand(ICommand command)
    {
        _commands.Enqueue(command);
        _commandManager = CommandManager.Instance;
        return this;
    }

    public void StartSequence(Action onComplete)
    {
        _commandManager.AddCommandSequence(this);   
        if (_commands.Count == 0)
        {
            Debug.LogWarning("There is any comand added");
            return;
        }
        this.onComplete = onComplete;
        ExecuteNextCommand();
    }
    private void ExecuteNextCommand()
    {
        if (_commands.Count == 0)
        {
            _commandManager.RemoveCommandSequence();
            onComplete?.Invoke();
            return;
        }

        _currentCommand = _commands.Dequeue();
        _currentCommand.Execute(ExecuteNextCommand);
    }

    public void StopSequence()
    {
        _commandManager.RemoveCommandSequence();
        _currentCommand.Exit();
    }
}
