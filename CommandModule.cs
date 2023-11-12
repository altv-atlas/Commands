using System.Reflection;
using AltV.Icarus.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AltV.Icarus.Commands;

public static class CommandModule
{
    private static readonly ICollection<Type> Commands = new List<Type>( );
    internal static string EventName = "chat:message";
    
    public static IServiceCollection RegisterCommandModule( this IServiceCollection services, string eventName = "chat:message" )
    {
        EventName = eventName;
        
        services.AddSingleton<CommandManager>( );
        
        RegisterCommandType( services, typeof( IAsyncCommand ) );
        RegisterCommandType( services, typeof( ICommand ) );
        
        return services;
    }

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