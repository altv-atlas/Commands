using AltV.Net.Elements.Entities;

namespace AltV.Icarus.Commands.Interfaces;

public interface IAsyncCommand : ICommandData
{
    public Task OnCommandAsync( IPlayer player, string[ ] args );
}