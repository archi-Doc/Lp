// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrystalData;

internal class UnitCrystalContext : IUnitCrystalContext
{
    void IUnitCrystalContext.AddCrystal<TData>(CrystalConfiguration dataConfiguration)
    {
        this.typeToCrystalConfiguration[typeof(TData)] = dataConfiguration;
    }

    void IUnitCrystalContext.AddBigCrystal<TData>(BigCrystalConfiguration crystalConfiguration, CrystalConfiguration dataConfiguration)
    {
        this.typeToCrystalConfiguration[typeof(TData)] = dataConfiguration;
        this.typeToBigCrystalConfiguration[typeof(TData)] = crystalConfiguration;
    }

    internal void Configure(IUnitConfigurationContext context)
    {
        foreach (var x in this.typeToCrystalConfiguration)
        {// This is slow, but it is Singleton anyway.
            // Singleton: ICrystal<T> => Crystalizer.GetCrystal<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key)));

            // Singleton: T => Crystalizer.GetObject<T>()
            context.Services.Add(ServiceDescriptor.Singleton(x.Key, provider => provider.GetRequiredService<Crystalizer>().GetObject(x.Key)));

            // Transient: IFiler<TData> => Crystalizer.GetCrystal<T>().Filer
            // context.Services.Add(ServiceDescriptor.Transient(typeof(IFiler<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key).Filer));

            // Transient: IStorage<TData> => Crystalizer.GetCrystal<T>().Storage
            // context.Services.Add(ServiceDescriptor.Transient(typeof(IStorage<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key).Storage));
        }

        foreach (var x in this.typeToBigCrystalConfiguration)
        {
            // Singleton: IBigCrystal<T> => Crystalizer.GetCrystalData<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(IBigCrystal<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetBigCrystal(x.Key)));
        }

        var configuration = new CrystalizerConfiguration(this.typeToCrystalConfiguration, this.typeToBigCrystalConfiguration);
        context.SetOptions(configuration);

        var options = new CrystalizerOptions();
        options.RootPath = context.DataDirectory;
        context.SetOptions(options);
    }

    private Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration = new();
    private Dictionary<Type, BigCrystalConfiguration> typeToBigCrystalConfiguration = new();
}
