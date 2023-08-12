using AltV.Icarus.Commands.Interfaces;
using AltV.Net.Elements.Entities;

namespace AltV.Icarus.Commands.Test;

#if DEBUG
public class TestCommand : ICommand
{
    public string Name { get; set; } = "test";
    public string[ ]? Aliases { get; set; }
    public string Description { get; set; } = "Test";
    public uint RequiredLevel { get; set; } = 0;
    
    public void OnCommand( IPlayer player, string test, uint test2 )
    {
        Console.WriteLine( $"{ test }, { test2 }" );
    }
}
#endif