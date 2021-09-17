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

public class LPCore
{
    public static void Register(Container container)
    {
        // Base
        container.Register<Hash>(Reuse.Transient);
        container.RegisterDelegate(x => new BigMachine<Identifier>(ThreadCore.Root, container), Reuse.Singleton);

        // Main services
        container.Register<LPCore>(Reuse.Singleton);
        container.Register<LPInfo>(Reuse.Singleton);
        container.Register<Netsphere>(Reuse.Singleton);
    }

    public LPCore(LPInfo info, Netsphere netsphere)
    {
        this.Core = new(ThreadCore.Root);

        this.Info = info;
        this.Netsphere = netsphere;
    }

    public void Initialize(bool isConsole, string directory)
    {
        this.Info.IsConsole = isConsole;
        this.Info.Directory = directory;

        // Logger: Debug, Information, Warning, Error, Fatal
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(directory, "logs", "log.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
        .CreateLogger();
    }

    public void Prepare(LPConsoleOptions options)
    {
    }

    public void Start()
    {
        var s = this.Info.IsConsole ? " (Console)" : string.Empty;
        Log.Information("LP Start" + s);
    }

    public void Terminate()
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);
    }

    public ThreadCoreGroup Core { get; }

    public LPInfo Info { get; }

    public Netsphere Netsphere { get; }
}
