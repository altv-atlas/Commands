using System.Reflection;
using AltV.Icarus.Commands.Interfaces;
using AltV.Net;
using AltV.Net.Elements.Args;
using AltV.Net.Elements.Entities;
using Microsoft.Extensions.Logging;

namespace AltV.Icarus.Commands;

public sealed class CommandManager
{
    public delegate void CommandDelegate( IPlayer player, string[ ] parameters );
    public event CommandDelegate? OnAnyCommand;
    public event CommandDelegate? OnCommandNotFound;
    
    private record RegisteredCommand( Type Type, ICommand Instance );
    private readonly ICollection<RegisteredCommand> _registeredCommands =
        new List<RegisteredCommand>( );

    private readonly ILogger<CommandManager> _logger;

    public CommandManager( ILogger<CommandManager> logger )
    {
        _logger = logger;
        _logger.LogInformation( "Command Manager initialized!" );
        Alt.OnClient<IPlayer, string>( CommandModule.EventName, OnCommandAsync, OnCommandParser );
    }

    private void OnCommandParser( IPlayer player, MValueConst[ ] mValueArray, Action<IPlayer, string> action )
    {
        if( mValueArray.Length != 1 )
            return;

        var arg = mValueArray[ 0 ];

        if( arg.type != MValueConst.Type.String )
            return;

        action( player, arg.GetString( ) );
    }

    private void OnCommandAsync( IPlayer player, string command )
    {
        if( string.IsNullOrEmpty( command ) || !command.StartsWith( CommandModule.CommandPrefix ) || command.Length < 2 )
            return;

        // Remove prefix from command
        command = command.Trim( ).Remove( 0, 1 );

        var args = command.Split( ' ' );

        TriggerAnyKnownCommand( player, args );
        OnAnyCommand?.Invoke( player, args );
    }

    private void TriggerAnyKnownCommand( IPlayer player, string[ ] args )
    {
        var command = GetRegisteredCommandByName( args[ 0 ] );

        if( command is null )
        {
            OnCommandNotFound?.Invoke(player, args );
            return;
        }
        
        var methodInfo = command.Type.GetMethod( "OnCommand" );

        if( methodInfo is null )
            return;

        var parsedArgs = ParseCommandArgs( player, args, methodInfo );

        // If any string value is empty, must've parsed incorrectly or player sent malicious data
        if( parsedArgs.Any( c => c is string str && string.IsNullOrEmpty( str ) ) )
            return;
        
        if( methodInfo.ReturnType == typeof( Task ) )
        {
            Task.Run( ( ) => methodInfo.Invoke( command.Instance, parsedArgs ) ).ConfigureAwait( false );
        }
        else
        {
            methodInfo.Invoke( command.Instance, parsedArgs );
        }
    }

    private static object[ ] ParseCommandArgs( IPlayer player, string[ ] args, MethodInfo methodInfo )
    {
        var parameters = methodInfo.GetParameters( );

        // player should be first param at all times
        var parsedArgs = new object[ parameters.Length ];
        parsedArgs[ 0 ] = ( IPlayer ) player;

        // Skip command name
        args = args.Skip( 1 ).ToArray( );
        var argCounter = 0;

        // Loop all "OnCommand" method parameters, skip IPlayer(1st) param
        for( int i = 1; i < parameters.Length; i++ )
        {
            // Failed to parse whatever the player sent, cancel here.
            if( argCounter >= args.Length ) 
                break;

            // If its a string, we have some special parsing to do since it could be a sentence
            if( parameters[ i ].ParameterType == typeof( string ) )
            {
                var leftoverArgs = args.Skip( argCounter );
                
                // If there is no element after this one, concat all remaining values into 1 string param
                if( i + 1 >= parameters.Length )
                {
                    parsedArgs[ i ] = string.Join( " ", leftoverArgs );
                    break;
                }

                parsedArgs[ i ] = ParseStringArg( parameters[ i + 1 ].ParameterType, leftoverArgs, ref argCounter );
            }
            // Easy, just a number or so
            else
            {
                parsedArgs[ i ] = Convert.ChangeType( args[ argCounter ], parameters[ i ].ParameterType );
                argCounter++;
            }
        }

        return parsedArgs;
    }

    private static string ParseStringArg( Type nextParamType, IEnumerable<string> leftoverArgs, ref int argCounter )
    {
        var combinedArgs = new List<string>( );

        foreach( var arg in leftoverArgs )
        {
            if( IsArgumentOfType( arg, nextParamType ) )
                break;

            argCounter++;
            combinedArgs.Add( arg );
        }
        
        return string.Join( " ", combinedArgs );
    }

    private static bool IsArgumentOfType( string arg, Type type )
    {
        try
        {
            _ = Convert.ChangeType( arg, type );
            return true;
        }
        catch( Exception ex )
        {
            return false;
        }
    }
    
    public ICollection<ICommand> GetCommands( )
    {
        return _registeredCommands.Select( c => c.Instance ).ToList( );
    }

    private RegisteredCommand? GetRegisteredCommandByName( string commandName )
    {
        return _registeredCommands.FirstOrDefault( c => 
            c.Instance.Name == commandName || 
            ( c.Instance.Aliases is not null && c.Instance.Aliases.Contains( commandName ) ) 
        );
    }

    public bool Exists( string command )
    {
        return GetCommands().Any( c => c.Name == command || ( c.Aliases is not null && c.Aliases.Contains( command ) ) );
    }

    internal void RegisterCommand( Type type, object baseObject )
    {
        if( baseObject is not ICommand command )
            return;
        
        MethodInfo? methodInfo = type.GetMethod( "OnCommand" );

        if( methodInfo is null )
        {
            throw new Exception( $"Command {type.FullName} does not implement required method \"OnCommand\"." );
        }
                
        _registeredCommands.Add( new RegisteredCommand( type, command ) );
    }
}