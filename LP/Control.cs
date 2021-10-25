// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.IO;
global using System.Threading.Tasks;
global using Arc.Threading;
global using BigMachines;
global using CrossChannel;
global using LP;
global using Serilog;
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
        container.Register<Information>(Reuse.Singleton);
        container.Register<Control>(Reuse.Singleton);
        container.Register<Netsphere>(Reuse.Singleton);
        container.Register<Node>(Reuse.Singleton);
        container.Register<Pipe>(Reuse.Singleton);

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
        this.ConfigureLogger();
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

    public void ConfigureLogger()
    {
        // Logger: Debug, Information, Warning, Error, Fatal
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
        .WriteTo.File(
            Path.Combine(this.Info.RootDirectory, "logs", "log.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
        .CreateLogger();
    }

    public void ConfigureControl()
    {
    }

    public void Start()
    {
        var s = this.Info.IsConsole ? " (Console)" : string.Empty;
        Log.Information("LP Start" + s + " : Press any key to exit");

        Log.Information($"Console: {this.Info.IsConsole}, Root directory: {this.Info.RootDirectory}");
        Log.Information(this.Info.ToString());
        Log.Information($"Current time: {Time.StartupTime}");

        Radio.Send(new Message.Start(this.Core));

        this.BigMachine.Start();
    }

    public void Stop()
    {
        Log.Information("LP Termination process initiated");

        Radio.Send(new Message.Stop());
    }

    public void MainLoop()
    {
        while (!this.Core.IsTerminated)
        {
            if (this.SafeKeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.D)
                {
                    this.Dump();
                }
                else
                {
                    break;
                }
            }

            this.Core.Sleep(100, 100);
        }
    }

    public void Terminate()
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);

        Log.Information("LP Teminated");
        Log.CloseAndFlush();
    }

    public bool SafeKeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }

    public ThreadCoreGroup Core { get; }

    public Information Info { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public Netsphere Netsphere { get; }

    private void Dump()
    {
        Log.Information($"Dump:");
        Log.Information($"MyStatus: {this.Netsphere.MyStatus.Type}");
    }
}
