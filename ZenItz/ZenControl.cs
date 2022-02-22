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
using System.Security.Cryptography;
using DryIoc;
using SimpleCommandLine;

namespace ZenItz;

public class ZenControl
{
    public static void Register(Container container, List<Type>? commandList = null)
    {
        // Container instance
        containerInstance = container;

        // Main services
        container.Register<ZenControl>(Reuse.Singleton);
        container.Register<Zen>(Reuse.Singleton);
        container.Register<Itz>(Reuse.Singleton);

        // Subcommands
        var commandTypes = new Type[]
        {
            typeof(LP.Subcommands.ZenSubcommand),
            typeof(LP.Subcommands.ZenDirSubcommand),
        };

        LP.Subcommands.ZenSubcommand.Register(container);
        LP.Subcommands.ZenDirSubcommand.Register(container);

        commandList?.AddRange(commandTypes);
        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
        }

        SubcommandParserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = container,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
        };

        SubcommandParser = new(commandTypes, SubcommandParserOptions);
    }

    public static void QuickStart()
    {
        /*var netBase = containerInstance.Resolve<NetBase>();
        netBase.Initialize(enableServer, nodeName, options);
        netBase.AllowUnsafeConnection = allowUnsafeConnection;*/

        var netControl = containerInstance.Resolve<ZenControl>();

        Logger.Configure(null);
        Radio.Send(new Message.Configure());
        var message = new Message.Start(ThreadCore.Root);
        Radio.Send(message);
        if (message.Abort)
        {
            Radio.Send(new Message.Stop());
            return;
        }
    }

    public ZenControl(Zen zen, Itz itz)
    {
        this.ServiceProvider = (IServiceProvider)containerInstance;

        this.Zen = zen;
        this.Itz = itz;

        Radio.Open<Message.Configure>(this.Configure);
    }

    public void Configure(Message.Configure message)
    {
    }

    public static SimpleParser SubcommandParser { get; private set; } = default!;

    public static SimpleParserOptions SubcommandParserOptions { get; private set; } = default!;

    public IServiceProvider ServiceProvider { get; }

    public Zen Zen { get; }

    public Itz Itz { get; }

    public bool ExaltationOfIntegrality { get; } = true; // by Baxter.

    private static Container containerInstance = default!;
}
