// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.IO;
global using System.Threading.Tasks;
global using Arc.Crypto;
global using Arc.Threading;
global using BigMachines;
global using CrossChannel;
global using LP;
global using Tinyhand;
using DryIoc;
using LP.Services;
using LPEssentials.Radio;
using Netsphere;
using SimpleCommandLine;
using ZenItz;

namespace LP;

public class Control
{
    public static void Register(Container container)
    {// DI, NetServices/Filters, Machines, Subcommands
        // Container instance
        containerInstance = container;

        // Base
        container.RegisterDelegate(x => new BigMachine<Identifier>(container), Reuse.Singleton);

        // Main services
        container.Register<Control>(Reuse.Singleton);
        container.Register<LPBase>(Reuse.Singleton);
        container.Register<IViewService, ConsoleViewService>(Reuse.Singleton);

        // RPC / Services
        container.Register<Services.BenchmarkServiceImpl>(Reuse.Transient);

        // RPC / Filters
        container.Register<Services.TestOnlyFilter>(Reuse.Transient);

        var commandList = new List<Type>();
        NetControl.Register(container, commandList);
        ZenControl.Register(container, commandList);

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
            typeof(LP.Subcommands.BenchmarkSubcommand),
        };

        LP.Subcommands.DumpSubcommand.Register(container);
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
            DisplayCommandListAsHelp = true,
        };

        commandList.AddRange(commandTypes);
        subcommandParser = new SimpleParser(commandList, SubcommandParserOptions);
    }

    public static bool ObjectToMemoryOwner(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        if (obj is LP.Fragments.FragmentBase flake &&
            FlakeFragmentService.TrySerialize<LP.Fragments.FragmentBase>(flake, out dataToBeMoved))
        {
            return true;
        }
        else
        {
            dataToBeMoved = default;
            return false;
        }
    }

    public static object? MemoryOwnerToObject(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        if (TinyhandSerializer.TryDeserialize<LP.Fragments.FragmentBase>(memoryOwner.Memory, out var value))
        {
            return value;
        }
        else
        {
            return null;
        }
    }

    public Control(IViewService viewService, LPBase lpBase, BigMachine<Identifier> bigMachine, NetControl netsphere, ZenControl zenControl)
    {
        this.ViewService = viewService;
        this.LPBase = lpBase;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.NetControl = netsphere;
        this.NetControl.SetupServer();
        this.ZenControl = zenControl;
        this.ZenControl.Zen.IO.SetRootDirectory(this.LPBase.RootDirectory);
        this.ZenControl.Zen.SetDelegate(ObjectToMemoryOwner, MemoryOwnerToObject);

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
        // Load strings
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        HashedString.LoadAssembly(null, asm, "Strings.strings-en.tinyhand");

        // Netsphere
        await this.NetControl.EssentialNode.LoadAsync(Path.Combine(this.LPBase.DataDirectory, EssentialNode.FileName)).ConfigureAwait(false);
        if (!await this.ZenControl.Itz.LoadAsync(Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzFile)).ConfigureAwait(false))
        {
            await this.ZenControl.Itz.LoadAsync(Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzBackup)).ConfigureAwait(false);
        }

        // ZenItz
        var result = await this.ZenControl.Zen.TryStartZen(new(Zen.DefaultZenDirectory, Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenBackup), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryBackup), QueryDelegate: null));
        if (result != ZenStartResult.Success)
        {
            throw new PanicException();
        }

        await this.LoadKeyVaultAsync().ConfigureAwait(false);
        await Radio.SendAsync(new Message.LoadAsync()).ConfigureAwait(false);
    }

    public async Task AbortAsync()
    {
        await this.ZenControl.Zen.AbortZen();
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(this.LPBase.DataDirectory);

        await this.NetControl.EssentialNode.SaveAsync(Path.Combine(this.LPBase.DataDirectory, EssentialNode.FileName)).ConfigureAwait(false);
        await this.ZenControl.Itz.SaveAsync(Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzFile), Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzBackup));

        await Radio.SendAsync(new Message.SaveAsync()).ConfigureAwait(false);
    }

    public async Task StartAsync()
    {
        Logger.Default.Information("LP Start");
        Logger.Default.Information($"Console: {this.LPBase.IsConsole}, Root directory: {this.LPBase.RootDirectory}");
        Logger.Default.Information(this.LPBase.ToString());
        Logger.Console.Information("Press Enter key to switch to console mode.");
        Logger.Console.Information("Press Ctrl+C to exit.");

        await Radio.SendAsync(new Message.StartAsync(this.Core)).ConfigureAwait(false);
        this.BigMachine.Start();

        Logger.Default.Information("Running");
    }

    public async Task StopAsync()
    {
        Logger.Default.Information("Termination process initiated");

        await this.ZenControl.Zen.StopZen(new(Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenBackup), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryBackup)));

        await Radio.SendAsync(new Message.StopAsync()).ConfigureAwait(false);
    }

    public void Terminate(bool abort)
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);

        Logger.Default.Information(abort ? "Aborted" : "Terminated");
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
        return false;

        /*if (subcommandParser.HelpCommand != string.Empty)
        {
            return false;
        }

        Console.WriteLine();
        return true;*/
    }

    public static SimpleParserOptions SubcommandParserOptions { get; private set; } = default!;

    public ThreadCoreGroup Core { get; }

    public IViewService ViewService { get; }

    public LPBase LPBase { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public NetControl NetControl { get; }

    public ZenControl ZenControl { get; }

    private static Container containerInstance = default!;

    private static SimpleParser subcommandParser = default!;

    private async Task<bool> LoadKeyVaultAsync()
    {
        var keyVault = await KeyVault.Load(this.ViewService, this.LPBase.ConsoleOptions.KeyVault);
        if (keyVault == null)
        {
            var reply = await this.ViewService.RequestYesOrNo(Hashed.Services.KeyVault.AskNew);
            if (!reply)
            {// No
                throw new PanicException();
            }
        }

        return true;
    }
}
