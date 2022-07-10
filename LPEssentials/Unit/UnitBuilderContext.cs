// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LP.Unit;

public class UnitBuilderContext
{
    public UnitBuilderContext()
    {
        this.UnitName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        this.RootDirectory = Directory.GetCurrentDirectory();
    }

    public string UnitName { get; set; }

    public string RootDirectory { get; set; }

    public ServiceCollection ServiceCollection { get; } = new();

    public HashSet<Type> CreateInstanceSet { get; } = new();

    public List<Type> CommandList { get; } = new();

    public Dictionary<Type, CommandGroup> CommandGroups { get; } = new();

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

    public bool AddCommand(Type commandType)
    {
        if (this.commandSet.Contains(commandType))
        {
            return false;
        }
        else
        {
            this.commandSet.Add(commandType);
            this.CommandList.Add(commandType);
            return true;
        }
    }

    public bool AddSubcommand(Type commandType, Type? parentCommand = null)
    {
        parentCommand ??= typeof(object);

        // Add a command type to the parent.
        var group = this.GetCommandGroup(parentCommand);
        return group.AddCommand(commandType);
    }

    /*public void AddUnit<TUnit>(bool createInstance = true)
        where TUnit : UnitBase => this.UnitCollection.AddUnit<TUnit>(createInstance);*/

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

    private HashSet<Type> commandSet = new();
}
