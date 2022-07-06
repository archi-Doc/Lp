// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LP.Unit;

public class UnitBuilder<TUnit> : UnitBuilder
    where TUnit : BuiltUnit
{
    public UnitBuilder()
    {
    }

    public override TUnit Build() => this.Build<TUnit>();

    public override UnitBuilder<TUnit> Configure(Action<UnitBuilderContext> configureDelegate)
        => (UnitBuilder<TUnit>)((UnitBuilder)this).Configure(configureDelegate);
}

/// <summary>
/// Builder class of unit, a unit of function and dependency.<br/>
/// Unit: HostBuilder +Nested unit +Unit operation, -AppConfiguration -Container.
/// </summary>
public class UnitBuilder
{
    public UnitBuilder()
    {
    }

    public virtual BuiltUnit Build() => this.Build<BuiltUnit>();

    public virtual TUnit Build<TUnit>()
        where TUnit : BuiltUnit
    {
        var context = new UnitBuilderContext();
        context.UnitName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        context.RootDirectory = Directory.GetCurrentDirectory();

        this.Configure(context);

        context.TryAddSingleton<BuiltUnitParameter>();
        context.TryAddSingleton<TUnit>();
        context.TryAddSingleton<RadioClass>(); // Unit radio

        var serviceProvider = context.ServiceCollection.BuildServiceProvider();

        var param = serviceProvider.GetRequiredService<BuiltUnitParameter>();
        param.ServiceProvider = serviceProvider;
        param.CommandList = ;

        return serviceProvider.GetRequiredService<TUnit>();
    }

    internal void Configure(UnitBuilderContext context)
    {
        if (this.configured)
        {
            throw new InvalidOperationException();
        }

        this.configured = true;

        // Unit builders
        foreach (var x in this.configureUnitBuilders)
        {
            x.Configure(context);
        }

        // Configure actions
        foreach (var x in this.configureActions)
        {
            x(context);
        }

        // Register commands to the service collection.
        foreach (var x in context.CommandList)
        {
            context.ServiceCollection.TryAddSingleton(x);
        }
    }

    public virtual UnitBuilder Configure(Action<UnitBuilderContext> configureDelegate)
    {
        this.configureActions.Add(configureDelegate);
        return this;
    }

    /*public UnitBuilder ConfigureServices(Action<UnitBuilderContext, IServiceCollection> configureDelegate)
    {
        this.configureServicesActions.Add(configureDelegate);
        return this;
    }

    public UnitBuilder ConfigureCommands(Action<UnitBuilderContext, ICommandCollection> configureDelegate)
    {
        this.configureCommandsActions.Add(configureDelegate);
        return this;
    }*/

    public UnitBuilder ConfigureBuilder(UnitBuilder unitBuilder)
    {
        this.configureUnitBuilders.Add(unitBuilder);
        return this;
    }

    /*public UnitBuilder ConfigurePostProcess(Action<UnitBuilderContext> configureDelegate)
    {
        this.configurePostProcessActions.Add(configureDelegate);
        return this;
    }*/

    private bool configured = false;
    private List<Action<UnitBuilderContext>> configureActions = new();
    // private List<Action<UnitBuilderContext>> configureCommandsActions = new();
    // private List<Action<UnitBuilderContext>> configurePostProcessActions = new();
    private List<UnitBuilder> configureUnitBuilders = new();
}
