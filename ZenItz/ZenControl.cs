// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1208 // System using directives should be placed before other using directives
#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.Net;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Arc.Unit;
global using CrossChannel;
global using LP;
global using LP.Block;
global using LP.Data;
global using Tinyhand;
global using ValueLink;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using BigMachines;
using SimpleCommandLine;

namespace ZenItz;

public class ZenControl
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                LPBase.Configure(context);

                // Main services
                context.AddSingleton<ZenControl>();
                context.AddSingleton<Zen>();
                context.AddSingleton<Itz>();

                // Subcommands
                Subcommands.ZenDirSubcommand.Configure(context);
                Subcommands.ZenTempSubcommand.Configure(context);
            });
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

    public ZenControl(Zen zen, Itz itz)
    {
        this.Zen = zen;
        this.Itz = itz;
    }

    public Zen Zen { get; }

    public Itz Itz { get; }

    public bool ExaltationOfIntegrality { get; } = true; // by Baxter.
}
