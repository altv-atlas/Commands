using System.Reflection;
using AltV.Icarus.Commands.Interfaces;
using AltV.Net;
using AltV.Net.Elements.Args;
using AltV.Net.Elements.Entities;
using Microsoft.Extensions.Logging;

namespace AltV.Icarus.Commands;

/// <summary>
/// Handles incoming commands from client-side and emits them through to implemented commands
/// </summary>
public sealed class CommandManager
{
    public delegate void CommandDelegate( IPlayer player, string[ ] parameters );
 
    /// <summary>
    /// Triggered when any command has been received from a client, whether it's a known or unknown command.
    /// </summary>
    public event CommandDelegate? OnAnyCommand;
    
    /// <summary>
    /// Triggers when an unknown command was received from a client.
    /// </summary>
    public event CommandDelegate? OnCommandNotFound;
    
    /// <summary>
    /// Record to keep track of a single registered command
    /// </summary>
    /// <param name="Type">Type, usually ICommand</param>
    /// <param name="Instance">instance of a command</param>
    private record RegisteredCommand( Type Type, ICommand Instance );
    
    /// <summary>
    /// List of registered commands
    /// </summary>
    private readonly ICollection<RegisteredCommand> _registeredCommands =
        new List<RegisteredCommand>( );
    
    private readonly ILogger<CommandManager> _logger;

    public CommandManager( ILogger<CommandManager> logger )
    {
        _logger = logger;
        _logger.LogInformation( "Command Manager initialized!" );
        Alt.OnClient<IPlayer, string>( CommandModule.EventName, OnCommandAsync, OnCommandParser );
    }

    /// <summary>
    /// Parses the command before actually processing it. In case of failure it will not be processed in the next step.
    /// </summary>
    /// <param name="player">the player who sent it</param>
    /// <param name="mValueArray">data that was received</param>
    /// <param name="action">the action to trigger processing of the command</param>
    private void OnCommandParser( IPlayer player, MValueConst[ ] mValueArray, Action<IPlayer, string> action )
    {
        if( mValueArray.Length != 1 )
            return;

        var arg = mValueArray[ 0 ];

        if( arg.type != MValueConst.Type.String )
            return;

        action( player, arg.GetString( ) );
    }

    /// <summary>
    /// Processes an incoming command
    /// </summary>
    /// <param name="player">the player who sent it</param>
    /// <param name="command">the command string to process</param>
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

    /// <summary>
    /// Attempts to trigger any known command
    /// </summary>
    /// <param name="player">player who sent the command</param>
    /// <param name="args">arguments that were sent</param>
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

    /// <summary>
    /// Attempts to parse a command into it's expected parameter types
    /// If certain parameters are strings, it will try to combine sentences into 1 string parameter
    /// </summary>
    /// <param name="player">The player who sent it</param>
    /// <param name="args">The args to parse</param>
    /// <param name="methodInfo">method info of the target OnCommand method</param>
    /// <returns></returns>
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

    /// <summary>
    /// Attempts to parse a string argument
    /// </summary>
    /// <param name="nextParamType">Type of the parameter that comes after this</param>
    /// <param name="leftoverArgs">Leftover arguments from the string</param>
    /// <param name="argCounter">reference to argCounter</param>
    /// <returns>A string containing the parsed value</returns>
    private static string ParseStringArg( Type nextParamType, IEnumerable<string> leftoverArgs, ref int argCounter )
    {
        var combinedArgs = new List<string>( );

        foreach( var arg in leftoverArgs )
        {
            // if the next string value matches the type of the next parameter of OnCommand, then this is probably the end of the "sentence".
            if( IsArgumentOfType( arg, nextParamType ) )
                break;

            argCounter++;
            combinedArgs.Add( arg );
        }
        
        return string.Join( " ", combinedArgs );
    }

    /// <summary>
    /// Checks whether a string argument can be converted to the target type
    /// </summary>
    /// <param name="arg">The argument to check</param>
    /// <param name="type">The type it should match</param>
    /// <returns>True if it matches, false otherwise</returns>
    private static bool IsArgumentOfType( string arg, Type type )
    {
        try
        {
            _ = Convert.ChangeType( arg, type );
            return true;
        }
        catch( Exception )
        {
            return false;
        }
    }
    
    /// <summary>
    /// Get a list of all the currently registered commands
    /// </summary>
    /// <returns>A list of commands</returns>
    public ICollection<ICommand> GetCommands( )
    {
        return _registeredCommands.Select( c => c.Instance ).ToList( );
    }

    /// <summary>
    /// Get a registered command by name
    /// </summary>
    /// <param name="commandName">The name of the command</param>
    /// <returns>A registered command if found, null otherwise.</returns>
    private RegisteredCommand? GetRegisteredCommandByName( string commandName )
    {
        return _registeredCommands.FirstOrDefault( c => 
            c.Instance.Name == commandName || 
            ( c.Instance.Aliases is not null && c.Instance.Aliases.Contains( commandName ) ) 
        );
    }

    /// <summary>
    /// Check if a command exists
    /// </summary>
    /// <param name="command">the name of the command (or alias)</param>
    /// <returns>True if it exists, false otherwise</returns>
    public bool Exists( string command )
    {
        return GetCommands().Any( c => c.Name == command || ( c.Aliases is not null && c.Aliases.Contains( command ) ) );
    }

    /// <summary>
    /// Register a new command into the command module
    /// </summary>
    /// <param name="type">The type of command, by default ICommand</param>
    /// <param name="baseObject">The instance of the command</param>
    internal void RegisterCommand( Type type, object baseObject )
    {
        if( baseObject is not ICommand command )
            return;
        
        MethodInfo? methodInfo = type.GetMethod( "OnCommand" );

        if( methodInfo is null )
        {
            _logger.LogError( "Command \"{FullName}\" does not implement required method \"OnCommand(IPlayer, ...)\"", type.FullName );
        }
                
        _registeredCommands.Add( new RegisteredCommand( type, command ) );
    }
}