// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection;
using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleCommandLine;

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
    public override TUnit Build(string[]? args = null) => this.Build<TUnit>(args);

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
    public virtual BuiltUnit Build(string[]? args = null) => this.Build<BuiltUnit>(args);

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
    public virtual UnitBuilder Configure(Action<IUnitConfigurationContext> @delegate)
    {
        // this.configureActions.Add(@delegate);
        return this;
    }

    /// <summary>
    /// Adds a delegate to the builder for setting up the unit.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="delegate">The delegate for setting up the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder Setup(Action<IUnitSetupContext> @delegate)
    {
        this.setupActions.Add(@delegate);
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

    internal virtual TUnit Build<TUnit>(string[]? args)
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

        // Setup
        this.SetupInternal(builderContext);

        // Configure
        UnitLogger.Configure(builderContext); // Logger
        this.ConfigureInternal(builderContext);

        builderContext.TryAddSingleton<UnitCore>();
        builderContext.TryAddSingleton<UnitContext>();
        builderContext.TryAddSingleton<TUnit>();
        builderContext.TryAddSingleton<RadioClass>(); // Unit radio

        var serviceProvider = builderContext.Services.BuildServiceProvider();

        // BuilderContext to UnitContext.
        var unitContext = serviceProvider.GetRequiredService<UnitContext>();
        unitContext.FromBuilderContext(serviceProvider, builderContext);

        return serviceProvider.GetRequiredService<TUnit>();
    }

    internal void PreloadInternal(UnitBuilderContext context, string? args)
    {
        // Arguments
        this.PreloadArguments(context, args);

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

    internal void PreloadArguments(UnitBuilderContext context, string? args)
    {
        if (args != null)
        {
            var arguments = args.FormatArguments();
            string optionString = null;
            var options = new Dictionary<string, string>();
            foreach (var x in arguments)
            {
                if (x.IsOptionString())
                {// -option
                    ClearOptionString(null);
                    optionString = x.Trim('-');
                }
                else
                {// value
                    ClearOptionString(x);
                }
            }

            void ClearOptionString(string? valueString)
            {

            }
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
    private List<Action<IUnitSetupContext>> setupActions = new();
    private List<UnitBuilder> configureUnitBuilders = new();
}
