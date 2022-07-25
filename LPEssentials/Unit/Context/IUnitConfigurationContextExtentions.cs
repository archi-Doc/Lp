// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

public static class IUnitConfigurationContextExtentions
{
    public static void AddSingleton<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.AddSingleton<TService>();

    public static void AddScoped<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.AddScoped<TService>();

    public static void AddTransient<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.AddTransient<TService>();

    public static void AddSingleton(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddSingleton(serviceType);

    public static void AddScoped(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddSingleton(serviceType);

    public static void AddTransient(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddTransient(serviceType);

    public static void AddSingleton<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.AddSingleton<TService, TImplementation>();

    public static void AddScoped<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.AddScoped<TService, TImplementation>();

    public static void AddTransient<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.AddTransient<TService, TImplementation>();

    public static void TryAddSingleton<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.TryAddSingleton<TService>();

    public static void TryAddScoped<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.TryAddScoped<TService>();

    public static void TryAddTransient<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.TryAddTransient<TService>();

    public static void TryAddSingleton(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddSingleton(serviceType);

    public static void TryAddScoped(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddSingleton(serviceType);

    public static void TryAddTransient(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddTransient(serviceType);

    public static void TryAddSingleton<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.TryAddSingleton<TService, TImplementation>();

    public static void TryAddScoped<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.TryAddScoped<TService, TImplementation>();

    public static void TryAddTransient<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.TryAddTransient<TService, TImplementation>();
}
