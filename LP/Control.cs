// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
using LP.Net;

namespace LP;

public class Control
{
    public static void Register(Container container)
    {
        // Base
        container.RegisterDelegate(x => new BigMachine<Identifier>(container), Reuse.Singleton);

        // Main services
        container.Register<Control>(Reuse.Singleton);
        container.Register<Information>(Reuse.Singleton);
        container.Register<Netsphere>(Reuse.Singleton);
        container.Register<Terminal>(Reuse.Singleton);
        container.Register<EssentialNode>(Reuse.Singleton);
        container.Register<NetSocket>(Reuse.Singleton);

        // Machines
        container.Register<Machines.SingleMachine>();
        container.Register<Machines.EssentialNetMachine>();
    }

    public Control(Information info, BigMachine<Identifier> bigMachine, Netsphere netsphere)
    {
        this.Info = info;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.Netsphere = netsphere;

        this.Core = new(ThreadCore.Root);
        this.BigMachine.Core.ChangeParent(this.Core);
    }

    public void Configure()
    {
        Logger.Configure(this.Info);
        this.ConfigureControl();

        Radio.Send(new Message.Configure());
    }

    public async Task LoadAsync()
    {
        await Radio.SendAsync(new Message.LoadAsync());
    }

    public async Task SaveAsync()
    {
        await Radio.SendAsync(new Message.SaveAsync());
    }

    public void ConfigureControl()
    {
    }

    public bool TryStart()
    {
        var s = this.Info.IsConsole ? " (Console)" : string.Empty;
        Logger.Default.Information("LP Start" + s);

        Logger.Default.Information($"Console: {this.Info.IsConsole}, Root directory: {this.Info.RootDirectory}");
        Logger.Default.Information(this.Info.ToString());
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

    public ThreadCoreGroup Core { get; }

    public Information Info { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public Netsphere Netsphere { get; }

    private void Dump()
    {
        Logger.Default.Information($"Dump:");
        Logger.Default.Information($"MyStatus: {this.Netsphere.MyStatus.Type}");
    }
}
