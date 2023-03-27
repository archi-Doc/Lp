// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Threading.Tasks;
global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using Tinyhand;
global using ValueLink;
using CrystalData.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CrystalData;

internal class UnitCrystalizeContext : IUnitCrystalizeContext
{
    void IUnitCrystalizeContext.TryAdd<T>(CrystalPolicy crystalPolicy)
        => this.typeToCrystalPolicy.TryAdd(typeof(T), crystalPolicy);

    void IUnitCrystalizeContext.TryAdd<T>(Crystalization crystalization)
        => this.typeToCrystalPolicy.TryAdd(typeof(T), new CrystalPolicy() with { Crystalization = crystalization, });

    internal void Configure(IUnitConfigurationContext contextj)
    {
        this.typeToCrystalPolicy.
        foreach (var x in this.typeToCrystalPolicy)
        {
            context.Services.Add(ServiceDescriptor.Singleton(typeof(ManualClass), provider => provider.GetRequiredService<ICrystal<ManualClass>>().Object));
        }
    }

    private ThreadsafeTypeKeyHashTable<CrystalPolicy> typeToCrystalPolicy = new();
}

public interface IUnitCrystalizeContext
{
    void TryAdd<T>(CrystalPolicy crystalPolicy)
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>;

    void TryAdd<T>(Crystalization crystalization)
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>;
}

public class CrystalControl
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                // Main services
                context.AddSingleton<CrystalizerClass>();
                context.Services.Add(ServiceDescriptor.Singleton(typeof(ICrystal<>), typeof(CrystalImpl<>)));

                context.AddSingleton<CrystalControl>();
                context.AddSingleton<CrystalOptions>();
                context.AddSingleton<IStorageKey, StorageKey>();

                // Crystalize
                var crystalizeContext = new UnitCrystalizeContext();
                foreach (var x in this.crystalizeActions)
                {
                    x(crystalizeContext);
                }

                crystalizeContext.Configure(context);
            });
        }

        public new Builder Configure(Action<IUnitConfigurationContext> configureDelegate)
        {
            base.Configure(configureDelegate);
            return this;
        }

        public Builder Crystalize(Action<IUnitCrystalizeContext> @delegate)
        {
            this.crystalizeActions.Add(@delegate);
            return this;
        }

        private List<Action<IUnitCrystalizeContext>> crystalizeActions = new();
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public record Param();

        public Unit(UnitContext context)
            : base(context)
        {
        }
    }

    public CrystalControl(UnitContext unitContext)
    {
        this.unitContext = unitContext;
    }

    public Crystal<TData> CreateCrystal<TData>(CrystalOptions options)
        where TData : BaseData
    {
        return new Crystal<TData>(
            this.unitContext.ServiceProvider.GetRequiredService<UnitCore>(),
            options,
            this.unitContext.ServiceProvider.GetRequiredService<ILogger<Crystal<TData>>>(),
            this.unitContext.ServiceProvider.GetRequiredService<UnitLogger>(),
            this.unitContext.ServiceProvider.GetRequiredService<IStorageKey>());
    }

    public bool ExaltationOfIntegrality { get; } = true; // ZenItz by Baxter.

    private readonly UnitContext unitContext;
}
