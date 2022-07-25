// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;
using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleCommandLine;
using static Arc.Unit.UnitBuilder;

namespace Arc.Unit;

/// <summary>
/// Builder class of unit, for customizing dependencies.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// </summary>
/// <typeparam name="TUnit">The type of unit.</typeparam>
public class UnitBuilder<TUnit> : UnitBuilder
    where TUnit : BuiltUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder{TUnit}"/> class.
    /// </summary>
    public UnitBuilder()
    {
    }

    /// <inheritdoc/>
    public override TUnit Build(string? args = null) => this.Build<TUnit>(args);

    /// <inheritdoc/>
    public override TUnit Build(string[] args) => this.Build<TUnit>(args);

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
    private record SetupItem(Type Type, Action<IUnitSetupContext, object> Action);

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder"/> class.
    /// </summary>
    public UnitBuilder()
    {
    }

    /// <summary>
    /// Runs the given actions and build a unit.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="BuiltUnit"/>.</returns>
    public virtual BuiltUnit Build(string[] args) => this.Build<BuiltUnit>(args);

    /// <summary>
    /// Runs the given actions and build a unit.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="BuiltUnit"/>.</returns>
    public virtual BuiltUnit Build(string? args = null) => this.Build<BuiltUnit>(args);

    /// <summary>
    /// Adds a delegate to the builder for preloading the unit.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="delegate">The delegate for preloading the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder Preload(Action<IUnitPreloadContext> @delegate)
    {
        this.preloadActions.Add(@delegate);
        return this;
    }

    /// <summary>
    /// Adds a delegate to the builder for configuring the unit.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="delegate">The delegate for configuring the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    /*public virtual UnitBuilder Configure(Action<IUnitConfigurationContext> @delegate)
    {
        // this.configureActions.Add(@delegate);
        return this;
    }*/

    // public delegate void SetupDelegate<TOptions>(IUnitSetupContext context, in TOptions options);

    /// <summary>
    /// Adds a delegate to the builder for setting up the option.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="delegate">The delegate for setting up the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder SetupOptions<TOption>(Action<IUnitSetupContext, TOption> @delegate)
        where TOption : class
    {
        var ac = new Action<IUnitSetupContext, object>((context, options) => @delegate(context, (TOption)options));
        var item = new SetupItem(typeof(TOption), ac);
        this.setupItems.Add(item);
        return this;
    }

    /// <summary>
    /// Adds a delegate to the builder for configuring the unit.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="delegate">The delegate for configuring the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder Configure(Action<UnitBuilderContext> @delegate)
    {
        this.configureActions.Add(@delegate);
        return this;
    }

    /// <summary>
    /// Adds a delegate to the builder for configuring logging.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="delegate">The delegate for configuring the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder ConfigureLogging(Action<UnitBuilderContext> @delegate)
    {
        this.configureLogging.Add(@delegate);
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

    internal virtual TUnit Build<TUnit>(string[] args)
        where TUnit : BuiltUnit
    {
        var s = args == null ? null : string.Join(' ', args);
        return this.Build<TUnit>(s);
    }

    internal virtual TUnit Build<TUnit>(string? args)
        where TUnit : BuiltUnit
    {
        if (this.built)
        {
            throw new InvalidOperationException();
        }

        this.built = true;

        // Builder context.
        var builderContext = new UnitBuilderContext();

        // Preload
        this.PreloadInternal(builderContext, args);

        // Configure
        UnitLogger.Configure(builderContext); // Logger
        this.ConfigureInternal(builderContext);

        builderContext.TryAddSingleton<UnitCore>();
        builderContext.TryAddSingleton<UnitContext>();
        builderContext.TryAddSingleton<TUnit>();
        builderContext.TryAddSingleton<RadioClass>(); // Unit radio

        // Setup classes
        foreach (var x in this.setupItems)
        {
            builderContext.TryAddSingleton(x.Type);
        }

        var serviceProvider = builderContext.Services.BuildServiceProvider();

        // BuilderContext to UnitContext.
        var unitContext = serviceProvider.GetRequiredService<UnitContext>();
        unitContext.FromBuilderContext(serviceProvider, builderContext);

        // Setup
        this.SetupInternal(builderContext, unitContext);

        return serviceProvider.GetRequiredService<TUnit>();
    }

    internal void PreloadInternal(UnitBuilderContext context, string? args)
    {
        // Arguments
        if (args != null)
        {
            context.arguments.Add(args);
        }

        // Directory
        context.SetDirectory();

        // Unit builders
        foreach (var x in this.configureUnitBuilders)
        {
            x.PreloadInternal(context, args);
        }

        // Actions
        foreach (var x in this.preloadActions)
        {
            x(context);
        }
    }

    internal void SetupInternal(UnitBuilderContext builderContext, UnitContext unitContext)
    {
        // Unit builders
        foreach (var x in this.configureUnitBuilders)
        {
            x.SetupInternal(builderContext, unitContext);
        }

        // Actions
        foreach (var x in this.setupItems)
        {
            var instance = unitContext.ServiceProvider.GetRequiredService(x.Type);
            x.Action(builderContext, instance);
        }
    }

    internal void ConfigureInternal(UnitBuilderContext context)
    {
        // Unit builders
        foreach (var x in this.configureUnitBuilders)
        {
            x.ConfigureInternal(context);
        }

        // Configure logging
        foreach (var x in this.configureLogging)
        {
            x(context);
        }

        // Configure actions
        foreach (var x in this.configureActions)
        {
            x(context);
        }
    }

    private bool built = false;
    private List<Action<IUnitPreloadContext>> preloadActions = new();
    private List<Action<UnitBuilderContext>> configureActions = new();
    private List<Action<UnitBuilderContext>> configureLogging = new();
    private List<SetupItem> setupItems = new();
    private List<UnitBuilder> configureUnitBuilders = new();
}
