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

    public List<Type> CommandList { get; } = new();

    // public UnitCollection UnitCollection { get; } = new();

    public ServiceCollection ServiceCollection { get; } = new();

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

    /*public void AddUnit<TUnit>(bool createInstance = true)
        where TUnit : UnitBase => this.UnitCollection.AddUnit<TUnit>(createInstance);*/

    public void AddSingleton<TService>()
        where TService : class => this.ServiceCollection.AddSingleton<TService>();

    public void AddScoped<TService>()
        where TService : class => this.ServiceCollection.AddScoped<TService>();

    public void AddTransient<TService>()
        where TService : class => this.ServiceCollection.AddTransient<TService>();

    public void TryAddSingleton<TService>()
        where TService : class => this.ServiceCollection.TryAddSingleton<TService>();

    public void TryAddScoped<TService>()
        where TService : class => this.ServiceCollection.TryAddScoped<TService>();

    public void TryAddTransient<TService>()
        where TService : class => this.ServiceCollection.TryAddTransient<TService>();

    private HashSet<Type> commandSet = new();
}
