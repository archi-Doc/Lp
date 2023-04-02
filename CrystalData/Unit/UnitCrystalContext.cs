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
            // Singleton: ICrystal<T> => Crystalizer.GetCrystal<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key)));

            // Singleton: T => Crystalizer.GetObject<T>()
            context.Services.Add(ServiceDescriptor.Singleton(x.Key, provider => provider.GetRequiredService<Crystalizer>().GetObject(x.Key)));

            // Transient: IFiler<TData> => Crystalizer.GetCrystal<T>().Filer
            context.Services.Add(ServiceDescriptor.Transient(typeof(IFiler<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key).Filer));

            // Singleton: IFiler<TData> => FilerFactory<TData>
            // context.Services.Add(ServiceDescriptor.Singleton(typeof(IFiler<>), typeof(FilerFactory<>)));
        }

        var crystalOptions = new CrystalOptions(this.typeToCrystalConfiguration, context.DataDirectory);
        context.SetOptions(crystalOptions);
    }

    private Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration = new();
}
