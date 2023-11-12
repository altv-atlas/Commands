using AltV.Icarus.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AltV.Icarus.Commands;

public static class CommandModule
{
    private static readonly ICollection<Type> Commands = new List<Type>( );
    internal static string EventName = "chat:message";
    internal static string CommandPrefix = "/";
    
    /// <summary>
    /// Registers the command module
    /// </summary>
    /// <param name="services">A service collection</param>
    /// <param name="eventName">Optional: The event that is used to send/receive commands from client-side. By default this is "chat:message".</param>
    /// <param name="commandPrefix">Optional: A command prefix which the player has to type before any command. By default set to "/".</param>
    /// <returns></returns>
    public static IServiceCollection RegisterCommandModule( this IServiceCollection services, string eventName = "chat:message", string commandPrefix = "/" )
    {
        EventName = eventName;
        CommandPrefix = commandPrefix;
        
        services.AddSingleton<CommandManager>( );
        
        RegisterCommandType( services, typeof( ICommand ) );
        
        return services;
    }

    /// <summary>
    /// Register a command of a given type.
    /// </summary>
    /// <param name="services">A service collection</param>
    /// <param name="type">The type to register</param>
    private static void RegisterCommandType( IServiceCollection services, Type type )
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies( );
        
        var commands = assemblies
            .SelectMany( s => s.GetTypes( ) )
            .Where( p => type.IsAssignableFrom( p ) && p.IsClass );

        foreach( var command in commands )
        {
            Commands.Add( command );
            services.AddSingleton( command );
        }
    }
    
    /// <summary>
    /// Initializes the command module, instantiates commands etc.
    /// </summary>
    /// <param name="serviceProvider">A service provider</param>
    /// <returns>The service provider</returns>
    /// <exception cref="NullReferenceException">Thrown when the command module failed to initialize.</exception>
    public static IServiceProvider InitializeCommandModule( this IServiceProvider serviceProvider )
    {
        var count = 0;

        var commandManager = serviceProvider.GetService<CommandManager>( );

        var logger = serviceProvider.GetService<ILogger<Logger>>( );
        
        if( commandManager is null || logger is null )
            throw new NullReferenceException( "Failed to initialize CommandManager" );
        
        foreach( var type in Commands )
        {
            // Console.WriteLine( $"[{ModuleName}] Instantiating command: { type.Name }" );
            var command = serviceProvider.GetService( type );

            if( command is null )
                continue;
            
            commandManager.RegisterCommand( type, command );
            count++;
        }

        logger.LogInformation( "{Count} commands initialized!", count );
        return serviceProvider;
    }
    
    private class Logger {}
}