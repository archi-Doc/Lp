// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrystalData;

internal class UnitCrystalContext : IUnitCrystalContext
{
    void IUnitCrystalContext.TryAdd<T>(CrystalConfiguration crystalConfiguration)
        => this.typeToCrystalConfiguration.TryAdd(typeof(T), crystalConfiguration);

    internal void Configure(IUnitConfigurationContext context)
    {
        context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>), typeof(CrystalImpl<>)));

        foreach (var x in this.typeToCrystalConfiguration)
        {
            // ICrystal<T> => Crystalizer.Get<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetInternal(x.Key)));

            // T => Crystalizer.Get<T>().Object
            // context.Services.Add(ServiceDescriptor.Singleton(x.Key, provider => provider.GetRequiredService(typeof(ICrystal<>).MakeGenericType(x.Key)).Object));
        }

        var crystalOptions = new CrystalizerOptions(this.typeToCrystalConfiguration);
        context.SetOptions(crystalOptions);
    }

    private Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration = new();
}
