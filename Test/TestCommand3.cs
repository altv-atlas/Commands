using AltV.Icarus.Commands.Interfaces;
using AltV.Net.Elements.Entities;

namespace AltV.Icarus.Commands.Test;

#if DEBUG
public class TestCommand3 : ICommand
{
    public string Name { get; set; } = "test3";
    public string[ ]? Aliases { get; set; }
    public string Description { get; set; } = "Test";
    public uint RequiredLevel { get; set; } = 0;
    
    public void OnCommand( IPlayer player, uint test, string test2 )
    {
        Console.WriteLine( $"{ test }, { test2 }" );
    }
}
#endif