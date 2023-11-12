using System.Collections;
using System.Reflection;
using AltV.Icarus.Commands.Interfaces;
using AltV.Net;
using AltV.Net.Async;
using AltV.Net.Elements.Entities;
using AltV.Net.FunctionParser;
using Microsoft.Extensions.Logging;

namespace AltV.Icarus.Commands;

public sealed class CommandManager
{
    public delegate void CommandDelegate( IPlayer player, string[ ] arguments );
    public delegate Task AsyncCommandDelegate( IPlayer player, string[ ] arguments );
    
    private readonly IDictionary<string, AsyncCommandDelegate> _asyncCommandDelegates =
        new Dictionary<string, AsyncCommandDelegate>( );

    private readonly IDictionary<string, CommandDelegate> _commandDelegates =
        new Dictionary<string, CommandDelegate>( );
    
    public event CommandDelegate? OnAnyCommand;
    public event AsyncCommandDelegate? OnAnyCommandAsync;

    private readonly ICollection<ICommandData> _commands = new List<ICommandData>();
    
    private readonly ILogger<CommandManager> _logger;
    
    public CommandManager( ILogger<CommandManager> logger )
    {
        _logger = logger;
        _logger.LogInformation( "Command Manager initialized!" );
        AltAsync.OnClient<IPlayer, string, Task>( CommandModule.EventName, OnCommandAsync );
    }
    
    private async Task OnCommandAsync( IPlayer player, string command )
    {
        if( !command.StartsWith( CommandModule.CommandPrefix ) || command.Length < 2 )
            return;

        // Remove prefix from command
        command = command.Trim( ).Remove( 0, 1 );
        
        var args = command.Split( ' ' );

        TriggerAnyKnownCommand( player, args[ 0 ], args[1..] );
        await TriggerAnyKnownAsyncCommandAsync( player, args[ 0 ], args[1..] );
        
        if( OnAnyCommandAsync != null )
        {
            foreach( var d in OnAnyCommandAsync.GetInvocationList( ).Cast<AsyncCommandDelegate>( ) )
            {
                await d.Invoke( player, args );
            }
        }
        
        OnAnyCommand?.Invoke( player, args );
    }

    private void TriggerAnyKnownCommand( IPlayer player, string command, string[] args )
    {
        if( !_commandDelegates.TryGetValue( command, out var commandDelegate ) )
            return;
        
        commandDelegate.Invoke( player, args );
    }
    
    private async Task TriggerAnyKnownAsyncCommandAsync( IPlayer player, string command, string[] args )
    {
        if( !_asyncCommandDelegates.TryGetValue( command, out var asyncCommandDelegate ) )
            return;

        await asyncCommandDelegate.Invoke( player, args );
    }

    public ICollection<ICommandData> GetCommands( )
    {
        return _commands;
    }

    public bool Exists( string command )
    {
        return _commands.Any( c => c.Name == command || c.Aliases.Contains( command ) );
    }
    
    internal void RegisterCommand( Type type, object baseObject )
    {
        MethodInfo? methodInfo;
        
        switch( baseObject )
        {
            case ICommand command:
                methodInfo = type.GetMethod( "OnCommand" );
                
                if( methodInfo is null )
                    return;
                
                var commandDelegate = methodInfo.CreateDelegate<CommandDelegate>( baseObject );

                _commandDelegates[ command.Name ] = commandDelegate;

                foreach( var alias in command.Aliases )
                {
                    _commandDelegates[ alias ] = commandDelegate;
                }
                break;
            
            case IAsyncCommand asyncCommand:
                methodInfo = type.GetMethod( "OnCommandAsync" );
                
                if( methodInfo is null )
                    return;

                var asyncCommandDelegate = methodInfo.CreateDelegate<AsyncCommandDelegate>( baseObject );

                _asyncCommandDelegates[ asyncCommand.Name ] = asyncCommandDelegate;

                foreach( var alias in asyncCommand.Aliases )
                {
                    _asyncCommandDelegates[ alias ] = asyncCommandDelegate;
                }
                break;
        }
        
        _commands.Add( (ICommandData) baseObject );
    }
}