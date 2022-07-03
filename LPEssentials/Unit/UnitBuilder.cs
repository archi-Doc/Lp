// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LP.Unit;

public class UnitBuilder
{
    public UnitBuilder()
    {
    }

    public IUnit Build()
    {
        var name = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        var directory = Directory.GetCurrentDirectory();

        return this.Build(new(name, directory));
    }

    public IUnit Build(UnitBuilderContext builderContext)
    {
        if (this.built)
        {
            throw new InvalidOperationException();
        }

        this.built = true;

        // Services
        var services = new ServiceCollection();
        foreach (var x in this.configureUnitBuilders)
        {
            x.Build(builderContext);
        }

        foreach (var x in this.configureServicesActions)
        {
            x(builderContext, services);
        }

        // Commands
        var commands = new CommandCollection();
        foreach (var x in this.configureCommandsActions)
        {
            x(builderContext, commands);
        }

        // Register commands to the service collection.
        foreach (var x in commands)
        {
            services.TryAddSingleton(x.CommandType);
        }

        var serviceProvider = services.BuildServiceProvider();
    }

    public UnitBuilder ConfigureServices(Action<UnitBuilderContext, IServiceCollection> configureDelegate)
    {
        this.configureServicesActions.Add(configureDelegate);
        return this;
    }

    public UnitBuilder ConfigureCommands(Action<UnitBuilderContext, ICommandCollection> configureDelegate)
    {
        this.configureCommandsActions.Add(configureDelegate);
        return this;
    }

    public UnitBuilder ConfigureUnitBuilder(UnitBuilder unitBuilder)
    {
        this.configureUnitBuilders.Add(unitBuilder);
        return this;
    }

    private bool built = false;
    private List<Action<UnitBuilderContext, IServiceCollection>> configureServicesActions = new();
    private List<Action<UnitBuilderContext, ICommandCollection>> configureCommandsActions = new();
    private List<UnitBuilder> configureUnitBuilders = new();
}
