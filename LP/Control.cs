// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.IO;
global using Arc.Threading;
global using BigMachines;
global using LP;
global using Serilog;
using DryIoc;
using LP.Net;

namespace LP;

public class Control
{
    public static void Register(Container container)
    {
        // Base
        container.Register<Hash>(Reuse.Transient);
        container.RegisterDelegate(x => new BigMachine<Identifier>(ThreadCore.Root, container), Reuse.Singleton);

        // Main services
        container.Register<Information>(Reuse.Singleton);
        container.Register<Control>(Reuse.Singleton);
        container.Register<Netsphere>(Reuse.Singleton);
    }

    public Control(Information info, BigMachine<Identifier> bigMachine, Netsphere netsphere)
    {
        this.Info = info;
        this.BigMachine = bigMachine;
        this.Netsphere = netsphere;

        this.Core = new(ThreadCore.Root);
    }

    public void ConfigureLogger()
    {
        // Logger: Debug, Information, Warning, Error, Fatal
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(this.Info.RootDirectory, "logs", "log.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
        .CreateLogger();
    }

    public void Start()
    {
        var s = this.Info.IsConsole ? " (Console)" : string.Empty;
        Log.Information("LP Start" + s);

        this.Netsphere.Start(this.Core);
    }

    public void Terminate()
    {
        this.BigMachine.Core.Terminate();
        this.Core.Terminate();

        this.BigMachine.Core.WaitForTermination(-1);
        this.Core.WaitForTermination(-1);

        Log.Information("LP Teminated");
        Log.CloseAndFlush();
    }

    public ThreadCoreGroup Core { get; }

    public Information Info { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public Netsphere Netsphere { get; }
}
