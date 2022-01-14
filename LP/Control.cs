﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.IO;
global using System.Threading.Tasks;
global using Arc.Threading;
global using BigMachines;
global using CrossChannel;
global using LP;
global using Tinyhand;
using DryIoc;
using Netsphere;
using SimpleCommandLine;

namespace LP;

public class Control
{
    public static void Register(Container container)
    {
        // Container instance
        containerInstance = container;

        // Base
        container.RegisterDelegate(x => new BigMachine<Identifier>(container), Reuse.Singleton);

        // Main services
        container.Register<Control>(Reuse.Singleton);
        container.Register<LPBase>(Reuse.Singleton);

        var commandList = new List<Type>();
        NetControl.Register(container, commandList);

        // Machines
        container.Register<Machines.SingleMachine>();

        // Subcommands
        RegisterSubcommands(container, commandList);
    }

    public static void RegisterSubcommands(Container container, List<Type> commandList)
    {
        // Subcommands
        var commandTypes = new Type[]
        {
            typeof(LP.Subcommands.MicsSubcommand),
            typeof(LP.Subcommands.DumpSubcommand),
            typeof(LP.Subcommands.GCSubcommand),
            typeof(LP.Subcommands.PingSubcommand),
            typeof(LP.Subcommands.NetBenchSubcommand),
            typeof(LP.Subcommands.PunchSubcommand),
            typeof(LP.Subcommands.KeyVaultSubcommand),
            typeof(LP.Subcommands.TestSubcommand),
        };

        LP.Subcommands.KeyVaultSubcommand.Register(container);

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

        commandList.AddRange(commandTypes);
        subcommandParser = new SimpleParser(commandList, SubcommandParserOptions);
    }

    public Control(LPBase information, BigMachine<Identifier> bigMachine, NetControl netsphere)
    {
        this.LPBase = information;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.NetControl = netsphere;

        this.Core = new(ThreadCore.Root);
        this.BigMachine.Core.ChangeParent(this.Core);
    }

    public void Configure()
    {
        Logger.Configure(this.LPBase);

        Radio.Send(new Message.Configure());
    }

    public async Task LoadAsync()
    {
        await Radio.SendAsync(new Message.LoadAsync()).ConfigureAwait(false);
        await this.NetControl.EssentialNode.LoadAsync(Path.Combine(this.LPBase.RootDirectory, EssentialNode.FileName)).ConfigureAwait(false);
    }

    public async Task SaveAsync()
    {
        await Radio.SendAsync(new Message.SaveAsync()).ConfigureAwait(false);
        await this.NetControl.EssentialNode.SaveAsync(Path.Combine(this.LPBase.RootDirectory, EssentialNode.FileName)).ConfigureAwait(false);
    }

    public bool TryStart()
    {
        var s = this.LPBase.IsConsole ? " (Console)" : string.Empty;
        Logger.Default.Information("LP Start" + s);

        Logger.Default.Information($"Console: {this.LPBase.IsConsole}, Root directory: {this.LPBase.RootDirectory}");
        Logger.Default.Information(this.LPBase.ToString());
        Logger.Console.Information("Press the Enter key to change to console mode.");
        Logger.Console.Information("Press Ctrl+C to exit.");

        var message = new Message.Start(this.Core);
        Radio.Send(message);
        if (message.Abort)
        {
            Radio.Send(new Message.Stop());
            return false;
        }

        this.BigMachine.Start();

        return true;
    }

    public void Stop()
    {
        Logger.Default.Information("LP Termination process initiated");

        Radio.Send(new Message.Stop());
    }

    public void Terminate()
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);

        Logger.Default.Information("LP Teminated");
        Logger.CloseAndFlush();
    }

    public bool Subcommand(string subcommand)
    {
        if (!subcommandParser.Parse(subcommand))
        {
            if (subcommandParser.HelpCommand != string.Empty)
            {
                subcommandParser.ShowHelp();
            }
            else
            {
                Console.WriteLine("Invalid subcommand.");
            }

            return false;
        }

        subcommandParser.Run();
        if (subcommandParser.HelpCommand != string.Empty)
        {
            return false;
        }

        Console.WriteLine();
        return true;
    }

    public static SimpleParserOptions SubcommandParserOptions { get; private set; } = default!;

    public ThreadCoreGroup Core { get; }

    public LPBase LPBase { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public NetControl NetControl { get; }

    private static Container containerInstance = default!;

    private static SimpleParser subcommandParser = default!;

    private void Dump()
    {
        Logger.Default.Information($"Dump:");
        Logger.Default.Information($"MyStatus: {this.NetControl.MyStatus.Type}");
    }
}
