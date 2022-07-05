// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LP.Unit;

public class UnitBuilderContext
{
    public UnitBuilderContext()
    {
    }

    public string UnitName { get; set; } = string.Empty;

    public string RootDirectory { get; set; } = string.Empty;

    public CommandCollection CommandCollection { get; } = new();

    // public UnitCollection UnitCollection { get; } = new();

    public ServiceCollection ServiceCollection { get; } = new();

    public void AddCommand(Type commandType) => this.CommandCollection.AddCommand(commandType);

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
}

// public record UnitBuilderContext(string UnitName, string RootDirectory, IServiceProvider ServiceProvider);
