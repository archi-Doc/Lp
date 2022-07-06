// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1208 // System using directives should be placed before other using directives
#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.Net;
global using System.Threading.Tasks;
global using Arc.Threading;
global using CrossChannel;
global using LP;
global using LP.Block;
global using LP.Options;
global using Tinyhand;
global using ValueLink;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using BigMachines;
using DryIoc;
using LP.Unit;
using SimpleCommandLine;
using ZenItz.Subcommand.Zen;

namespace ZenItz;

public class ZenControl
{
    private static Type[] commandTypes = new[]
    {
        typeof(ZenSubcommand),
        typeof(LP.Subcommands.ZenDirSubcommand),
    };

    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder(bool registerSubcommand = true)
            : base()
        {
            this.Configure(context =>
            {
                // Main services
                context.AddSingleton<ZenControl>();
                context.AddSingleton<Zen>();
                context.AddSingleton<Itz>();
            });

            if (!registerSubcommand)
            {
                return;
            }

            this.Configure(context =>
            {
                ZenSubcommand.Register(context);
                LP.Subcommands.ZenDirSubcommand.Register(context);

                // Subcommands
                foreach (var x in commandTypes)
                {
                    context.AddCommand(x);
                }
            });
        }
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public record Param();

        public Unit(UnitParameter parameter)
            : base(parameter)
        {
        }

        public void RunStandalone(Param param)
        {
        }
    }

    public ZenControl(IServiceProvider serviceProvider, Zen zen, Itz itz)
    {
        // this.ServiceProvider = serviceProvider;
        this.Zen = zen;
        this.Itz = itz;

        this.SubcommandParserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = serviceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
        };

        this.SubcommandParser = new(commandTypes, this.SubcommandParserOptions);
    }

    public SimpleParser SubcommandParser { get; private set; } = default!;

    public SimpleParserOptions SubcommandParserOptions { get; private set; } = default!;

    // public IServiceProvider ServiceProvider { get; }

    public Zen Zen { get; }

    public Itz Itz { get; }

    public bool ExaltationOfIntegrality { get; } = true; // by Baxter.
}
