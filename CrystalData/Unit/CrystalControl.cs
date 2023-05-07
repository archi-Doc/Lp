// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using Tinyhand;
global using ValueLink;
using CrystalData.Storage;
using CrystalData.UserInterface;
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
                context.AddSingleton<CrystalControl>();
                context.AddSingleton<CrystalizerConfiguration>();
                context.AddSingleton<CrystalizerOptions>();
                context.AddSingleton<Crystalizer>();
                context.AddSingleton<IStorageKey, StorageKey>();
                context.TryAddSingleton<ICrystalDataQuery, CrystalDataQueryDefault>();

                // Crystalizer
                var crystalContext = new UnitCrystalContext();
                foreach (var x in this.crystalActions)
                {
                    x(crystalContext);
                }

                crystalContext.Configure(context);
            });
        }

        public new Builder Preload(Action<IUnitPreloadContext> @delegate)
        {
            base.Preload(@delegate);
            return this;
        }

        public new Builder Configure(Action<IUnitConfigurationContext> @delegate)
        {
            base.Configure(@delegate);
            return this;
        }

        public Builder ConfigureCrystal(Action<IUnitCrystalContext> @delegate)
        {
            this.crystalActions.Add(@delegate);
            return this;
        }

        private List<Action<IUnitCrystalContext>> crystalActions = new();
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public Unit(UnitContext context)
            : base(context)
        {
        }
    }

    public CrystalControl(UnitContext unitContext)
    {
        this.unitContext = unitContext;
    }

    public bool ExaltationOfIntegrality { get; } = true; // ZenItz by Baxter.

    private readonly UnitContext unitContext;
}
