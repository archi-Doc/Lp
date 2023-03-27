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
            });
        }

        public void Crystalize()
        {
            this.
        }
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
