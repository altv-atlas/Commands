using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AltV.Atlas.Commands.Tests;

public class CommandModuleTests
{
    [Fact]
    public void CommandModuleShouldRegister()
    {
        var serviceProvider = Substitute.For<IServiceProvider>( );
        var serviceCollection = Substitute.For<IServiceCollection>( );
        
    }
}