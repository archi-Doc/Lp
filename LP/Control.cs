// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.IO;
global using System.Threading.Tasks;
global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using CrossChannel;
global using LP;
global using Tinyhand;
using LP.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netsphere;
using SimpleCommandLine;
using ZenItz;
using LP.Options;

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
                context.AddSingleton<KeyVault>();

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

            subcommandParser = new SimpleParser(context.Subcommands, SubcommandParserOptions);
        }

        public async Task RunAsync(LPOptions options)
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

            // LPBase
            this.lpBase = this.Context.ServiceProvider.GetRequiredService<LPBase>();
            this.lpBase.SetParameter(options, true, "relay");

            // NetBase
            this.netBase = this.Context.ServiceProvider.GetRequiredService<NetBase>();
            this.netBase.SetParameter(true, string.Empty, options.NetsphereOptions);
            this.netBase.AllowUnsafeConnection = true; // betacode
            this.netBase.NetsphereOptions.EnableTestFeatures = true; // betacode

            var control = this.Context.ServiceProvider.GetRequiredService<Control>();
            try
            {
                // Logger
                Logger.Configure(control.LPBase);

                // KeyVault
                await control.LoadKeyVaultAsync();

                // Start
                Logger.Default.Information("LP Start");

                // Create optional instances
                this.Context.CreateInstances();

                // Prepare
                this.Context.SendPrepare(new());
            }
            catch (PanicException)
            {
                control.Terminate(true);
                return;
            }

            try
            {// Load
                await control.LoadAsync(this.Context);
            }
            catch (PanicException)
            {
                await control.AbortAsync();
                control.Terminate(true);
                return;
            }

            try
            {// Start, Main loop
                await control.RunAsync(this.Context);

                control.MainLoop();

                await control.TerminateAsync(this.Context);
                await control.SaveAsync(this.Context);
                control.Terminate(false);
            }
            catch (PanicException)
            {
                await control.TerminateAsync(this.Context);
                await control.SaveAsync(this.Context);
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

    public Control(IUserInterfaceService userInterfaceService, LPBase lpBase, BigMachine<Identifier> bigMachine, NetControl netsphere, ZenControl zenControl, KeyVault keyVault)
    {
        this.UserInterfaceService = userInterfaceService;
        this.LPBase = lpBase;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.NetControl = netsphere;
        this.NetControl.SetupServer();
        this.ZenControl = zenControl;
        this.ZenControl.Zen.IO.SetRootDirectory(this.LPBase.RootDirectory);
        this.ZenControl.Zen.SetDelegate(ObjectToMemoryOwner, MemoryOwnerToObject);
        this.KeyVault = keyVault;

        this.Core = new(ThreadCore.Root);
        this.BigMachine.Core.ChangeParent(this.Core);
    }

    public async Task<bool> TryTerminate()
    {
        if (!this.LPBase.LPOptions.ConfirmExit)
        {// No confirmation
            this.Core.Terminate(); // this.Terminate(false);
            return true;
        }

        var result = await this.UserInterfaceService.RequestYesOrNo(Hashed.Dialog.ConfirmExit);
        if (result == true)
        {
            this.Core.Terminate(); // this.Terminate(false);
            return true;
        }

        return false;
    }

    public async Task LoadAsync(UnitContext context)
    {
        // Netsphere
        await this.NetControl.EssentialNode.LoadAsync(Path.Combine(this.LPBase.DataDirectory, EssentialNode.FileName)).ConfigureAwait(false);

        // ZenItz
        if (!await this.ZenControl.Itz.LoadAsync(Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzFile)).ConfigureAwait(false))
        {
            await this.ZenControl.Itz.LoadAsync(Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzBackup)).ConfigureAwait(false);
        }

        var result = await this.ZenControl.Zen.TryStartZen(new(Zen.DefaultZenDirectory, Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenBackup), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryBackup), QueryDelegate: null));
        if (result != ZenStartResult.Success)
        {
            throw new PanicException();
        }

        await context.SendLoadAsync(new());
    }

    public async Task AbortAsync()
    {
        await this.ZenControl.Zen.AbortZen();
    }

    public async Task SaveAsync(UnitContext context)
    {
        Directory.CreateDirectory(this.LPBase.DataDirectory);

        await this.SaveKeyVaultAsync();
        await this.NetControl.EssentialNode.SaveAsync(Path.Combine(this.LPBase.DataDirectory, EssentialNode.FileName)).ConfigureAwait(false);
        await this.ZenControl.Itz.SaveAsync(Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzFile), Path.Combine(this.LPBase.DataDirectory, Itz.DefaultItzBackup));

        await context.SendSaveAsync(new());
    }

    public async Task RunAsync(UnitContext context)
    {
        this.BigMachine.Start();
        await context.SendRunAsync(new(this.Core));
        Console.WriteLine();

        this.ShowInformation();
        this.LPBase.LPOptions.NetsphereOptions.ShowInformation();

        Logger.Console.Information("Press Enter key to switch to console mode.");
        Logger.Console.Information("Press Ctrl+C to exit.");
        Logger.Console.Information("Running");
    }

    public void ShowInformation()
    {
        Logger.Default.Information($"Console: {this.LPBase.IsConsole}, Root directory: {this.LPBase.RootDirectory}");
        // Logger.Default.Information(this.LPBase.ToString());
    }

    public async Task TerminateAsync(UnitContext context)
    {
        Logger.Default.Information("Termination process initiated");

        await this.ZenControl.Zen.StopZen(new(Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenBackup), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryFile), Path.Combine(this.LPBase.DataDirectory, Zen.DefaultZenDirectoryBackup)));
        await context.SendTerminateAsync(new());
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
                var command = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    if (string.Compare(command, "exit", true) == 0)
                    {// Exit
                        if (this.TryTerminate().Result == true)
                        { // To view mode
                            Logger.ViewMode = true;
                            return;
                        }
                        else
                        {
                            Console.Write("> ");
                            continue;
                        }
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
                else
                {
                    Console.WriteLine();
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

    public IUserInterfaceService UserInterfaceService { get; }

    public LPBase LPBase { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public NetControl NetControl { get; }

    public ZenControl ZenControl { get; }

    public KeyVault KeyVault { get; }

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

    private async Task LoadKeyVaultAsync()
    {
        if (this.LPBase.IsFirstRun)
        {// First run
        }
        else
        {
            var result = await this.KeyVault.LoadAsync(this.LPBase.CombineDataPath(this.LPBase.LPOptions.KeyVault, KeyVault.Filename));
            if (result)
            {
                goto LoadKeyVaultObjects;
            }

            // Could not load KeyVault
            var reply = await this.UserInterfaceService.RequestYesOrNo(Hashed.KeyVault.AskNew);
            if (reply != true)
            {// No
                throw new PanicException();
            }
        }

        Console.WriteLine(HashedString.Get(Hashed.KeyVault.Create));
        // await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Information, Hashed.KeyVault.Create);

        // New KeyVault
        var password = await this.UserInterfaceService.RequestPasswordAndConfirm(Hashed.KeyVault.EnterPassword, Hashed.Dialog.Password.Confirm);
        if (password == null)
        {
            throw new PanicException();
        }

        this.KeyVault.Create(password);

LoadKeyVaultObjects:
        await this.LoadKeyVault_NodeKey();
    }

    private async Task LoadKeyVault_NodeKey()
    {
        if (!this.KeyVault.TryGetAndDeserialize<NodePrivateKey>(NodePrivateKey.Filename, out var key))
        {// Failure
            if (!this.KeyVault.Created)
            {
                await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Error, Hashed.KeyVault.NoData, NodePrivateKey.Filename);
            }

            return;
        }

        this.NetControl.NetBase.SetNodeKey(key);
    }

    private async Task SaveKeyVaultAsync()
    {
        this.KeyVault.Add(NodePrivateKey.Filename, this.NetControl.NetBase.SerializeNodeKey());

        await this.KeyVault.SaveAsync(this.LPBase.CombineDataPath(this.LPBase.LPOptions.KeyVault, KeyVault.Filename));
    }
}
