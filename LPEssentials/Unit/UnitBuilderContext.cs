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
    /// Gets <see cref="ServiceCollection"/>.
    /// </summary>
    public ServiceCollection ServiceCollection { get; } = new();

    public HashSet<Type> CreateInstanceSet { get; } = new();

    public Dictionary<Type, CommandGroup> CommandGroups { get; } = new();

    /// <summary>
    /// Adds the specified <see cref="Type"/> to the creation list.
    /// </summary>
    /// <typeparam name="T">The type to be instantiated.</typeparam>
    public void CreateInstance<T>()
        => this.CreateInstanceSet.Add(typeof(T));

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

    public CommandGroup GetCommandGroup() => this.GetCommandGroup(typeof(TopCommand));

    public CommandGroup GetSubcommandGroup() => this.GetCommandGroup(typeof(SubCommand));

    public bool AddCommand(Type commandType)
    {
        var group = this.GetCommandGroup();
        return group.AddCommand(commandType);
    }

    public bool AddSubcommand(Type commandType)
    {
        var group = this.GetSubcommandGroup();
        return group.AddCommand(commandType);
    }

    public void AddSingleton<TService>()
        where TService : class => this.ServiceCollection.AddSingleton<TService>();

    public void AddScoped<TService>()
        where TService : class => this.ServiceCollection.AddScoped<TService>();

    public void AddTransient<TService>()
        where TService : class => this.ServiceCollection.AddTransient<TService>();

    public void AddSingleton(Type serviceType) => this.ServiceCollection.AddSingleton(serviceType);

    public void AddScoped(Type serviceType) => this.ServiceCollection.AddSingleton(serviceType);

    public void AddTransient(Type serviceType) => this.ServiceCollection.AddTransient(serviceType);

    public void AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.ServiceCollection.AddSingleton<TService, TImplementation>();

    public void AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.ServiceCollection.AddScoped<TService, TImplementation>();

    public void AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.ServiceCollection.AddTransient<TService, TImplementation>();

    public void TryAddSingleton<TService>()
        where TService : class => this.ServiceCollection.TryAddSingleton<TService>();

    public void TryAddScoped<TService>()
        where TService : class => this.ServiceCollection.TryAddScoped<TService>();

    public void TryAddTransient<TService>()
        where TService : class => this.ServiceCollection.TryAddTransient<TService>();

    public void TryAddSingleton(Type serviceType) => this.ServiceCollection.AddSingleton(serviceType);

    public void TryAddScoped(Type serviceType) => this.ServiceCollection.AddSingleton(serviceType);

    public void TryAddTransient(Type serviceType) => this.ServiceCollection.AddTransient(serviceType);

    public void TryAddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.ServiceCollection.TryAddSingleton<TService, TImplementation>();

    public void TryAddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.ServiceCollection.TryAddScoped<TService, TImplementation>();

    public void TryAddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService => this.ServiceCollection.TryAddTransient<TService, TImplementation>();

    internal class TopCommand
    {
    }

    internal class SubCommand
    {
    }
}
