// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrystalData;

internal class UnitCrystalContext : IUnitCrystalContext
{
    void IUnitCrystalContext.TryAdd<T>(CrystalConfiguration crystalConfiguration)
        => this.typeToCrystalConfiguration.TryAdd(typeof(T), crystalConfiguration);

    internal void Configure(IUnitConfigurationContext context)
    {
        foreach (var x in this.typeToCrystalConfiguration)
        {// This is slow, but it is Singleton anyway.
            // ICrystal<T> => Crystalizer.Get<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key)));

            // T => Crystalizer.Get<T>().Object
            context.Services.Add(ServiceDescriptor.Singleton(x.Key, provider => provider.GetRequiredService<Crystalizer>().GetObject(x.Key)));

            // Singleton: IFiler<TData> => FilerFactory<TData>
            context.Services.Add(ServiceDescriptor.Singleton(typeof(IFiler<>), typeof(FilerFactory<>)));
        }

        context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>), typeof(CrystalNotRegistered<>)));

        var crystalOptions = new CrystalizerOptions(this.typeToCrystalConfiguration);
        context.SetOptions(crystalOptions);
    }

    private Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration = new();
}
