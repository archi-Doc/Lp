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
using LP.Services;
using LP.Unit;
using LPEssentials.Radio;
using Netsphere;
using SimpleCommandLine;
using ZenItz;
using Microsoft.Extensions.DependencyInjection.Extensions;
using LP.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LP;

public class Control
{
    public class Builder : UnitBuilder<Unit>
    {
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                // Base
                context.TryAddSingleton<BigMachine<Identifier>>();

                // Main services
                context.AddSingleton<Control>();
                context.AddSingleton<LPBase>();
                context.ServiceCollection.TryAddSingleton<IUserInterfaceService, ConsoleUserInterfaceService>();

                // RPC / Services
                context.AddTransient<Services.BenchmarkServiceImpl>();

                // RPC / Filters
                context.AddTransient<Services.TestOnlyFilter>();

                // Machines
                context.AddTransient<Machines.SingleMachine>();

                // Subcommands
                context.AddSubcommand(typeof(LP.Subcommands.MicsSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.ExitSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.GCSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.PingSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.NetBenchSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.PunchSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.BenchmarkSubcommand));

                LP.Subcommands.DumpSubcommand.Configure(context);
                LP.Subcommands.KeyVaultSubcommand.Configure(context);
            });

            this.ConfigureBuilder(new NetControl.Builder());
            this.ConfigureBuilder(new ZenControl.Builder());
        }
    }

    public class Unit : BuiltUnit
    {
        public Unit(UnitContext context)
            : base(context)
        {
            SubcommandParserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = context.ServiceProvider,
                RequireStrictCommandName = true,
                RequireStrictOptionName = true,
                DoNotDisplayUsage = true,
                DisplayCommandListAsHelp = true,
            };

            subcommandParser = new SimpleParser(context.GetCommandTypes(typeof(object)), SubcommandParserOptions);
        }

        public async Task Run(LPOptions options)
        {
            // Load strings
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            try
            {
                HashedString.LoadAssembly(null, asm, "Strings.strings-en.tinyhand");
                HashedString.LoadAssembly("ja", asm, "Strings.strings-en.tinyhand");
            }
            catch
            {
            }

            // Load options
            if (!string.IsNullOrEmpty(options.OptionsPath))
            {
                var originalPath = options.OptionsPath;
                try
                {
                    var utf8 = File.ReadAllBytes(originalPath);
                    var op = TinyhandSerializer.DeserializeFromUtf8<LPOptions>(utf8);
                    if (op != null)
                    {
                        options = op;
                        Console.WriteLine(HashedString.Get(Hashed.Success.Loaded, originalPath));
                    }
                }
                catch
                {
                    Console.WriteLine(HashedString.Get(Hashed.Error.Load, originalPath));
                }
            }

            this.lpBase = this.ServiceProvider.GetRequiredService<LPBase>();
            this.lpBase.Initialize(options, true, "relay");

            this.netBase = this.ServiceProvider.GetRequiredService<NetBase>();
            this.netBase.Initialize(true, string.Empty, options.NetsphereOptions);
            this.netBase.AllowUnsafeConnection = true; // betacode

            var control = this.ServiceProvider.GetRequiredService<Control>();
            try
            {
                // Logger
                Logger.Configure(control.LPBase);
                Logger.Default.Information("LP Start");

                // Create optional instances
                this.CreateInstances();

                // Configure
                this.SendPrepare(new());
            }
            catch (PanicException)
            {
                control.Terminate(true);
                return;
            }

            try
            {// Load
                await control.LoadAsync();
            }
            catch (PanicException)
            {
                await control.AbortAsync();
                control.Terminate(true);
                return;
            }

            try
            {// Start, Main loop
                await control.StartAsync();

                control.MainLoop();

                await control.StopAsync();
                await control.SaveAsync();
                control.Terminate(false);
            }
            catch (PanicException)
            {
                await control.StopAsync();
                await control.SaveAsync();
                control.Terminate(true);
                return;
            }
        }

        private LPBase lpBase = default!;
        private NetBase netBase = default!;
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

    public Control(IUserInterfaceService viewService, LPBase lpBase, BigMachine<Identifier> bigMachine, NetControl netsphere, ZenControl zenControl)
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

    public async Task LoadAsync()
    {
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

        // tempcode await this.LoadKeyVaultAsync().ConfigureAwait(false);
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
        Logger.Default.Information($"Console: {this.LPBase.IsConsole}, Root directory: {this.LPBase.RootDirectory}");
        Logger.Default.Information(this.LPBase.ToString());

        await Radio.SendAsync(new Message.StartAsync(this.Core)).ConfigureAwait(false);
        this.BigMachine.Start();

        Logger.Default.Information($"Test: {this.LPBase.LPOptions.NetsphereOptions.EnableTestFeatures}");
        Logger.Default.Information($"Alternative: {this.LPBase.LPOptions.NetsphereOptions.EnableAlternative}");
        Logger.Console.Information("Press Enter key to switch to console mode.");
        Logger.Console.Information("Press Ctrl+C to exit.");
        Logger.Console.Information("Running");
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

    private void MainLoop()
    {
        while (!this.Core.IsTerminated)
        {
            if (Logger.ViewMode)
            {// View mode
                if (this.SafeKeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                    { // To console mode
                        Logger.ViewMode = false;
                        Console.Write("> ");
                    }
                    else
                    {
                        while (this.SafeKeyAvailable)
                        {
                            Console.ReadKey(true);
                        }
                    }
                }
            }
            else
            {// Console mode
                var command = Console.ReadLine();
                if (!string.IsNullOrEmpty(command))
                {
                    if (string.Compare(command, "exit", true) == 0)
                    {// Exit
                        // To view mode
                        Logger.ViewMode = true;
                        return;
                    }
                    else
                    {// Subcommand
                        try
                        {
                            if (!this.Subcommand(command))
                            {
                                Console.Write("> ");
                                continue;
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                // To view mode
                Logger.ViewMode = true;
            }

            this.Core.Sleep(100, 100);
        }

        // To view mode
        Logger.ViewMode = true;
    }

    public static SimpleParserOptions SubcommandParserOptions { get; private set; } = default!;

    public ThreadCoreGroup Core { get; }

    public IUserInterfaceService ViewService { get; }

    public LPBase LPBase { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public NetControl NetControl { get; }

    public ZenControl ZenControl { get; }

    private static SimpleParser subcommandParser = default!;

    private bool SafeKeyAvailable
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

    private async Task<bool> LoadKeyVaultAsync()
    {
        var st = await this.ViewService.RequestString("Enter");
        Logger.Default.Information(st);

        var keyVault = await KeyVault.Load(this.ViewService, this.LPBase.LPOptions.KeyVault);
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
