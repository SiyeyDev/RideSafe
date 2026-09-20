using Cachacos;

public class CommandManager : SingletonMonobehaviour<CommandManager>
{
    private CommandSequence _sequence;
    public void AddCommandSequence(CommandSequence commandSequence) => _sequence = commandSequence;
    public void RemoveCommandSequence() => _sequence = null;
    public void InterrupSequence() => _sequence?.StopSequence();
}
