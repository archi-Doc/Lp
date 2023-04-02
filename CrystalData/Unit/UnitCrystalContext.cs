// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace CrystalData;

internal class UnitCrystalContext : IUnitCrystalContext
{
    void IUnitCrystalContext.AddData<TData>(DataConfiguration dataConfiguration)
    {
        this.typeToDataConfiguration[typeof(TData)] = dataConfiguration;
    }

    void IUnitCrystalContext.AddCrystalData<TData>(CrystalConfiguration crystalConfiguration, DataConfiguration dataConfiguration)
    {
        this.typeToDataConfiguration[typeof(TData)] = dataConfiguration;
        this.typeToCrystalConfiguration[typeof(TData)] = crystalConfiguration;
    }

    internal void Configure(IUnitConfigurationContext context)
    {
        foreach (var x in this.typeToDataConfiguration)
        {// This is slow, but it is Singleton anyway.
            // Singleton: ICrystal<T> => Crystalizer.GetCrystal<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key)));

            // Singleton: T => Crystalizer.GetObject<T>()
            context.Services.Add(ServiceDescriptor.Singleton(x.Key, provider => provider.GetRequiredService<Crystalizer>().GetObject(x.Key)));

            // Transient: IFiler<TData> => Crystalizer.GetCrystal<T>().Filer
            context.Services.Add(ServiceDescriptor.Transient(typeof(IFiler<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystal(x.Key).Filer));

            // Singleton: IFiler<TData> => FilerFactory<TData>
            // context.Services.Add(ServiceDescriptor.Singleton(typeof(IFiler<>), typeof(FilerFactory<>)));

            // Singleton: ICrystalData<T> => Crystalizer.GetCrystalData<T>()
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystalData<>).MakeGenericType(x.Key), provider => provider.GetRequiredService<Crystalizer>().GetCrystalData(x.Key)));
        }

        var crystalOptions = new CrystalOptions(this.typeToCrystalConfiguration, this.typeToDataConfiguration, context.DataDirectory);
        context.SetOptions(crystalOptions);
    }

    private Dictionary<Type, DataConfiguration> typeToDataConfiguration = new();
    private Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration = new();
}
