using AltV.Net.Elements.Entities;

namespace AltV.Icarus.Commands.Interfaces;

public interface ICommand : ICommandData
{
    public void OnCommand( IPlayer player, string[ ] args );
}