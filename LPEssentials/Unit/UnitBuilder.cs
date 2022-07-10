// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

/// <summary>
/// Builder class of unit, for customizing dependencies.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// </summary>
/// <typeparam name="TUnit">The type of unit.</typeparam>
public class UnitBuilder<TUnit> : UnitBuilder
    where TUnit : BuiltUnit
{
    public UnitBuilder()
    {
    }

    /// <inheritdoc/>
    public override TUnit Build() => this.Build<TUnit>();

    /// <inheritdoc/>
    public override UnitBuilder<TUnit> Configure(Action<UnitBuilderContext> configureDelegate)
        => (UnitBuilder<TUnit>)base.Configure(configureDelegate);

    /// <inheritdoc/>
    public override UnitBuilder<TUnit> ConfigureBuilder(UnitBuilder unitBuilder)
        => (UnitBuilder<TUnit>)base.ConfigureBuilder(unitBuilder);
}

/// <summary>
/// Builder class of unit, for customizing behaviors.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// </summary>
public class UnitBuilder
{
    public UnitBuilder()
    {
    }

    /// <summary>
    /// Runs the given actions and build a unit.
    /// </summary>
    /// <returns><see cref="BuiltUnit"/>.</returns>
    public virtual BuiltUnit Build() => this.Build<BuiltUnit>();

    /// <summary>
    /// Adds a delegate to the builder for configuring the unit.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="configureDelegate">The delegate for configuring the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder Configure(Action<UnitBuilderContext> configureDelegate)
    {
        this.configureActions.Add(configureDelegate);
        return this;
    }

    /// <summary>
    /// Adds a <see cref="UnitBuilder"/> instance to the builder for configuring the unit.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="unitBuilder"><see cref="UnitBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder ConfigureBuilder(UnitBuilder unitBuilder)
    {
        this.configureUnitBuilders.Add(unitBuilder);
        return this;
    }

    internal virtual TUnit Build<TUnit>()
        where TUnit : BuiltUnit
    {
        // Builder context.
        var context = new UnitBuilderContext();
        this.Configure(context);

        context.TryAddSingleton<UnitContext>();
        context.TryAddSingleton<TUnit>();
        context.TryAddSingleton<RadioClass>(); // Unit radio

        var serviceProvider = context.ServiceCollection.BuildServiceProvider();

        // Context to parameter.
        var param = serviceProvider.GetRequiredService<UnitContext>();
        param.FromBuilderContext(serviceProvider, context);

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

    private bool configured = false;
    private List<Action<UnitBuilderContext>> configureActions = new();
    private List<UnitBuilder> configureUnitBuilders = new();
}
