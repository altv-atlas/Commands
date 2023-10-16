namespace AltV.Atlas.Commands.Interfaces;

/// <summary>
/// Base interface for any command, implement this interface to register a new command
/// </summary>
public interface ICommand
{
    /// <summary>
    /// The name of the command (without "/")
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Any extra aliases for this command
    /// </summary>
    public string[ ]? Aliases { get; set; }
    
    /// <summary>
    /// Description for this command
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Required level for this command.
    /// NOTE: There is nothing built-in for this, it's something you can implement in your own game mode for eg admin commands.
    /// </summary>
    public uint RequiredLevel { get; set; }
}