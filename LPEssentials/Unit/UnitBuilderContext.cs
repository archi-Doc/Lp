// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

/// <summary>
/// Contextual information provided to <see cref="UnitBuilder"/>.<br/>
/// </summary>
public sealed class UnitBuilderContext
{
    public UnitBuilderContext()
    {
        this.UnitName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        this.RootDirectory = Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets or sets a unit name.
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// Gets or sets a root directory.
    /// </summary>
    public string RootDirectory { get; set; }

    /// <summary>
    /// Gets <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    public HashSet<Type> CreateInstanceSet { get; } = new();

    public Dictionary<Type, CommandGroup> CommandGroups { get; } = new();

    public List<LoggerResolverDelegate> LoggerResolvers { get; } = new();

    public void ClearLoggerResolver() => this.LoggerResolvers.Clear();

    public void AddLoggerResolver(LoggerResolverDelegate resolver) => this.LoggerResolvers.Add(resolver);

    /// <summary>
    /// Adds the specified <see cref="Type"/> to the creation list.
    /// Note that instances are actually created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    /// <typeparam name="T">The type to be instantiated.</typeparam>
    public void CreateInstance<T>()
        => this.CreateInstanceSet.Add(typeof(T));

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of the specified command type.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns><see cref="CommandGroup"/>.</returns>
    public CommandGroup GetCommandGroup(Type type)
    {
        if (!this.CommandGroups.TryGetValue(type, out var commandGroup))
        {
            this.TryAddSingleton(type);
            commandGroup = new(this);
            this.CommandGroups.Add(type, commandGroup);
        }

        return commandGroup;
    }

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of command.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    public CommandGroup GetCommandGroup() => this.GetCommandGroup(typeof(TopCommand));

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of subcommand.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    public CommandGroup GetSubcommandGroup() => this.GetCommandGroup(typeof(SubCommand));

    /// <summary>
    /// Adds command.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns><see langword="true"/>: Successfully added.</returns>
    public bool AddCommand(Type commandType)
    {
        var group = this.GetCommandGroup();
        return group.AddCommand(commandType);
    }

    /// <summary>
    /// Adds subcommand.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns><see langword="true"/>: Successfully added.</returns>
    public bool AddSubcommand(Type commandType)
    {
        var group = this.GetSubcommandGroup();
        return group.AddCommand(commandType);
    }

    public void AddSingleton<TService>()
        where TService : class => this.Services.AddSingleton<TService>();

    public void AddScoped<TService>()
        where TService : class => this.Services.AddScoped<TService>();

    public void AddTransient<TService>()
        where TService : class => this.Services.AddTransient<TService>();

    public void AddSingleton(Type serviceType) => this.Services.AddSingleton(serviceType);

    public void AddScoped(Type serviceType) => this.Services.AddSingleton(serviceType);

    public void AddTransient(Type serviceType) => this.Services.AddTransient(serviceType);

    public void AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.Services.AddSingleton<TService, TImplementation>();

    public void AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.Services.AddScoped<TService, TImplementation>();

    public void AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.Services.AddTransient<TService, TImplementation>();

    public void TryAddSingleton<TService>()
        where TService : class => this.Services.TryAddSingleton<TService>();

    public void TryAddScoped<TService>()
        where TService : class => this.Services.TryAddScoped<TService>();

    public void TryAddTransient<TService>()
        where TService : class => this.Services.TryAddTransient<TService>();

    public void TryAddSingleton(Type serviceType) => this.Services.AddSingleton(serviceType);

    public void TryAddScoped(Type serviceType) => this.Services.AddSingleton(serviceType);

    public void TryAddTransient(Type serviceType) => this.Services.AddTransient(serviceType);

    public void TryAddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.Services.TryAddSingleton<TService, TImplementation>();

    public void TryAddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.Services.TryAddScoped<TService, TImplementation>();

    public void TryAddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.Services.TryAddTransient<TService, TImplementation>();

    internal class TopCommand
    {
    }

    internal class SubCommand
    {
    }
}
