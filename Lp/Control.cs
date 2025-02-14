// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using Arc;
global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using CrystalData;
global using Lp;
global using Netsphere;
global using Tinyhand;
global using ValueLink;
using Lp.Data;
using Lp.Net;
using Lp.NetServices;
using Lp.Services;
using Lp.T3cs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netsphere.Crypto;
using Netsphere.Interfaces;
using Netsphere.Relay;
using SimpleCommandLine;

namespace Lp;

public class Control
{
    public class Builder : UnitBuilder<Unit>
    {
        public Builder()
            : base()
        {
            this.Preload(context =>
            {
                this.LoadStrings();
                this.LoadLpOptions(context);
            });

            this.Configure(context =>
            {
                // Base
                LpBase.Configure(context);

                // Main services
                context.AddSingleton<Control>();
                context.AddSingleton<LpBase>();
                context.AddSingleton<LpStats>();
                context.AddSingleton<LpService>();
                context.Services.TryAddSingleton<IConsoleService, ConsoleUserInterfaceService>();
                context.Services.TryAddSingleton<IUserInterfaceService, ConsoleUserInterfaceService>();
                context.AddSingleton<VaultControl>();
                context.AddTransient<Vault>();
                context.AddSingleton<IStorageKey, StorageKeyVault>();
                context.AddSingleton<AuthorityControl>();
                context.AddSingleton<Seedphrase>();
                context.AddSingleton<Credentials>();
                context.AddSingleton<Merger>();
                context.AddSingleton<RelayMerger>();
                context.AddSingleton<Linker>();
                ConfigureRelay(context);

                // RPC / Services
                context.AddSingleton<NetServices.RemoteBenchControl>();
                context.AddSingleton<NetServices.RemoteBenchHostAgent>();
                context.AddTransient<Lp.T3cs.MergerClientAgent>();
                context.AddTransient<Lp.Net.BasalServiceAgent>();
                context.AddTransient<RelayMergerServiceAgent>();
                context.AddTransient<MergerRemoteAgent>();

                // RPC / Filters
                context.AddTransient<NetServices.TestOnlyFilter>();
                context.AddTransient<NetServices.MergerOrTestFilter>();

                // Machines
                context.AddSingleton<BigMachine>();
                context.AddSingleton<BigMachineBase, BigMachine>();
                context.AddTransient<Machines.TemplateMachine>();
                context.AddTransient<Machines.LogTesterMachine>();
                context.AddTransient<Machines.LpControlMachine>();
                context.AddSingleton<Machines.RelayPeerMachine>();
                context.AddSingleton<Machines.NodeControlMachine>();

                // Subcommands
                context.AddSubcommand(typeof(Lp.Subcommands.TestSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.MicsSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.GCSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.PingSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.RestartRemoteContainerSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.RemoteBenchSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.RemoteDataSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.PunchSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.BenchmarkSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.NewSeedphraseSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.MergerClient.Command));
                context.AddSubcommand(typeof(Lp.Subcommands.MergerRemote.Command));
                context.AddSubcommand(typeof(Lp.Subcommands.NewTokenSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.ShowPeerNetNodeSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.ShowOwnNetNodeSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.ShowNodeControlStateSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.AddNetNodeSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.GetNetNodeSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.GetNodeInformationSubcommand));

                context.AddSubcommand(typeof(Lp.Subcommands.Credential.ShowMergerCredentialsCommand));

                // Lp.Subcommands.CrystalData.CrystalStorageSubcommand.Configure(context);
                // Lp.Subcommands.CrystalData.CrystalDataSubcommand.Configure(context);

                Lp.Subcommands.InfoSubcommand.Configure(context);
                Lp.Subcommands.ExportSubcommand.Configure(context);
                Lp.Subcommands.FlagSubcommand.Configure(context);
                Lp.Subcommands.AuthorityCommand.Subcommand.Configure(context);
                Lp.Subcommands.VaultCommand.Subcommand.Configure(context);
                Lp.Subcommands.CommandGroup.Configure(context);
                Lp.Subcommands.MergerClient.NestedCommand.Configure(context);
                Lp.Subcommands.MergerRemote.NestedCommand.Configure(context);
                Lp.Subcommands.Relay.Subcommand.Configure(context);
                Lp.Subcommands.KeyCommand.Subcommand.Configure(context);
            });

            this.SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/Log.txt";
                if (context.TryGetOptions<LpOptions>(out var lpOptions))
                {
                    options.Path = Path.Combine(lpOptions.RootDirectory, logfile);
                }
                else
                {
                    options.Path = Path.Combine(context.RootDirectory, logfile);
                }

                options.MaxLogCapacity = 20;
            });

            this.SetupOptions<Lp.Logging.NetsphereLoggerOptions>((context, options) =>
            {// NetsphereLoggerOptions, LogLowLevelNet
                var logfile = "Logs/Net.txt";
                if (context.TryGetOptions<LpOptions>(out var lpOptions))
                {
                    options.Path = Path.Combine(lpOptions.RootDirectory, logfile);
                }
                else
                {
                    options.Path = Path.Combine(context.RootDirectory, logfile);
                }

                options.MaxLogCapacity = 100;
                options.Formatter.TimestampFormat = "mm:ss.ffffff K";
                options.ClearLogsAtStartup = true;
                options.MaxQueue = 100_000;
            });

            this.SetupOptions<ConsoleLoggerOptions>((context, options) =>
            {// ConsoleLoggerOptions
                if (context.TryGetOptions<LpOptions>(out var lpOptions))
                {
                    options.Formatter.EnableColor = lpOptions.ColorConsole;
                }
                else
                {
                    options.Formatter.EnableColor = true;
                }
            });

            this.SetupOptions<LpBase>((context, lpBase) =>
            {// LpBase
                context.GetOptions<LpOptions>(out var options);
                lpBase.Initialize(options, true, "merger");
            });

            this.SetupOptions<NetBase>((context, netBase) =>
            {// NetBase
                context.GetOptions<LpOptions>(out var options);
                netBase.SetOptions(options.ToNetOptions());

                netBase.AllowUnsafeConnection = true; // betacode
                netBase.NetOptions.EnableServer = true; // betacode
                netBase.DefaultAgreement = netBase.DefaultAgreement with { MaxStreamLength = 100_000_000, }; // betacode
            });

            this.SetupOptions<CrystalizerOptions>((context, options) =>
            {// CrystalizerOptions
                context.GetOptions<LpOptions>(out var lpOptions);
                // options.RootPath = lpOptions.RootDirectory;
                options.DefaultSaveFormat = SaveFormat.Utf8;
                options.DefaultSavePolicy = SavePolicy.Periodic;
                options.DefaultSaveInterval = TimeSpan.FromMinutes(10);
                options.GlobalDirectory = new LocalDirectoryConfiguration(LpBase.DataDirectoryName);
                options.EnableFilerLogger = false;
            });

            var crystalControlBuilder = CrystalBuilder();

            this.AddBuilder(new NetControl.Builder());
            this.AddBuilder(crystalControlBuilder);
            this.AddBuilder(new Lp.Logging.LpLogger.Builder());
        }

        private static void ConfigureRelay(IUnitConfigurationContext context)
        {
            if (context.TryGetOptions<LpOptions>(out var options))
            {
                if (SignaturePublicKey.TryParse(options.CertificateRelayPublicKey, out var relayPublicKey, out _))
                {// CertificateRelayControl
                    context.AddSingleton<IRelayControl, CertificateRelayControl>();
                }
            }
        }

        private static CrystalControl.Builder CrystalBuilder()
        {
            return new CrystalControl.Builder()
                .ConfigureCrystal((Action<ICrystalUnitContext>)(context =>
                {
                    context.AddCrystal<LpSettings>(new()
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(LpSettings.Filename),
                        RequiredForLoading = true,
                    });

                    context.AddCrystal<LpStats>(new CrystalConfiguration() with
                    {
                        // SaveFormat = SaveFormat.Binary,
                        NumberOfFileHistories = 2,
                        FileConfiguration = new GlobalFileConfiguration(LpStats.Filename),
                    });

                    context.AddCrystal<Credentials>(new()
                    {
                        // SaveFormat = SaveFormat.Binary,
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(),
                    });

                    context.AddCrystal<Mono>(new()
                    {
                        SaveFormat = SaveFormat.Binary,
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(Mono.Filename),
                    });

                    context.AddCrystal<Netsphere.Stats.NetStats>(new CrystalConfiguration() with
                    {
                        // SaveFormat = SaveFormat.Binary,
                        NumberOfFileHistories = 2,
                        FileConfiguration = new GlobalFileConfiguration(Netsphere.Stats.NetStats.Filename),
                    });

                    context.AddCrystal<Netsphere.Misc.NtpCorrection>(new CrystalConfiguration() with
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(Netsphere.Misc.NtpCorrection.Filename),
                    });
                }));
        }

        private void LoadStrings()
        {// Load strings
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            try
            {
                HashedString.LoadAssembly(null, asm, "Misc.Strings.strings-en.tinyhand");
                HashedString.LoadAssembly("ja", asm, "Misc.Strings.strings-en.tinyhand");
            }
            catch
            {
            }
        }

        private void LoadLpOptions(IUnitPreloadContext context)
        {
            var args = context.Arguments.RawArguments;
            LpOptions? options = null;

            if (context.Arguments.TryGetOption("loadoptions", out var optionFile))
            {// 1st: Option file
                if (!string.IsNullOrEmpty(optionFile))
                {
                    var originalPath = optionFile;
                    try
                    {
                        var utf8 = File.ReadAllBytes(originalPath);
                        var op = TinyhandSerializer.DeserializeFromUtf8<LpOptions>(utf8);
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
            }

            // 2nd: Arguments
            SimpleParser.TryParseOptions<LpOptions>(args, out options, options);

            if (options is not null)
            {
                context.SetOptions(options);
            }
        }
    }

    public class Unit : BuiltUnit
    {
        public Unit(UnitContext context)
            : base(context)
        {
            TinyhandSerializer.ServiceProvider = context.ServiceProvider;
        }

        public async Task RunAsync(LpOptions options)
        {
            try
            {
                // Crystalizer
                var crystalizer = this.Context.ServiceProvider.GetRequiredService<Crystalizer>();

                // Vault
                var vaultControl = this.Context.ServiceProvider.GetRequiredService<VaultControl>();
                await vaultControl.LoadAsync();
                ((StorageKeyVault)this.Context.ServiceProvider.GetRequiredService<IStorageKey>()).VaultControl = vaultControl;

                // Load
                var result = await crystalizer.PrepareAndLoadAll();
                if (result != CrystalResult.Success)
                {
                    throw new PanicException();
                }
            }
            catch
            {
                ThreadCore.Root.Terminate();
                return;
            }

            var control = this.Context.ServiceProvider.GetRequiredService<Control>();
            try
            {
                LpConstants.Initialize();

                // Start
                control.Logger.Get<DefaultLog>().Log($"Lp ({Netsphere.Version.VersionHelper.VersionString})");

                // Prepare
                await control.PrepareMerger(this.Context);
                await control.PrepareRelay(this.Context);
                await control.PrepareLinker(this.Context);
                await control.PreparePeer(this.Context);

                // Vault -> NodeKey
                await control.LoadKeyVault_NodeKey();

                // Create optional instances
                this.Context.CreateInstances();

                // Prepare
                this.Context.SendPrepare(new());
            }
            catch
            {
                control.Terminate(true);
                return;
            }

            try
            {// Load
                await control.LoadAsync(this.Context);
            }
            catch
            {
                await control.AbortAsync();
                control.Terminate(true);
                return;
            }

            try
            {// Start, Main loop
                await control.StartAsync(this.Context);

                await control.MainAsync();

                this.Context.SendStop(new());
                await control.TerminateAsync(this.Context);
                await control.SaveAsync(this.Context);
                control.Terminate(false);
            }
            catch
            {
                await control.TerminateAsync(this.Context);
                await control.SaveAsync(this.Context);
                control.Terminate(true);
                return;
            }
        }
    }

    public Control(UnitContext context, UnitCore core, UnitLogger logger, IUserInterfaceService userInterfaceService, LpBase lpBase, BigMachine bigMachine, NetControl netsphere, Crystalizer crystalizer, VaultControl vault, AuthorityControl authorityControl, LpSettings settings, Merger merger, RelayMerger relayMerger, Linker linker)
    {
        this.Logger = logger;
        this.UserInterfaceService = userInterfaceService;
        this.LpBase = lpBase;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.NetControl = netsphere;
        this.Crystalizer = crystalizer;
        this.VaultControl = vault;
        this.AuthorityControl = authorityControl;
        this.LpBase.Settings = settings;
        this.Merger = merger;
        this.RelayMerger = relayMerger;
        this.Linker = linker;

        if (this.LpBase.Options.TestFeatures)
        {
            // NetAddress.SkipValidation = true;
            this.NetControl.Services.Register<IRemoteBenchHost, RemoteBenchHostAgent>();
        }

        this.Core = core;

        SubcommandParserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = context.ServiceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
            AutoAlias = true,
        };

        this.subcommandParser = new SimpleParser(context.Subcommands, SubcommandParserOptions);
    }

    public static SimpleParserOptions SubcommandParserOptions { get; private set; } = default!;

    public UnitLogger Logger { get; }

    public UnitCore Core { get; }

    public IUserInterfaceService UserInterfaceService { get; }

    public LpBase LpBase { get; }

    public BigMachine BigMachine { get; }

    public NetControl NetControl { get; }

    public Merger Merger { get; }

    public RelayMerger RelayMerger { get; }

    public Linker Linker { get; }

    public Crystalizer Crystalizer { get; }

    public VaultControl VaultControl { get; }

    public AuthorityControl AuthorityControl { get; }

    private SimpleParser subcommandParser;

    public async Task PreparePeer(UnitContext context)
    {
        this.NetControl.Services.Register<IBasalService, BasalServiceAgent>();

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayPeerPrivault))
        {// RelayPeerPrivault is valid
            var privault = this.LpBase.Options.RelayPeerPrivault;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.NewSignature();
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }
        }

        if (!string.IsNullOrEmpty(this.LpBase.Options.ContentPeerPrivault))
        {// ContentPeerPrivault is valid
            var privault = this.LpBase.Options.ContentPeerPrivault;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.NewSignature();
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }
        }
    }

    public async Task PrepareRelay(UnitContext context)
    {
        if (context.ServiceProvider.GetService<IRelayControl>() is CertificateRelayControl certificateRelayControl)
        {
            if (SignaturePublicKey.TryParse(this.LpBase.Options.CertificateRelayPublicKey, out var relayPublicKey, out _))
            {
                certificateRelayControl.SetCertificatePublicKey(relayPublicKey);
                this.Logger.Get<CertificateRelayControl>().Log($"Active: {relayPublicKey.ToString()}");
            }
        }
    }

    public async Task PrepareMerger(UnitContext context)
    {
        var crystalizer = context.ServiceProvider.GetRequiredService<Crystalizer>();
        if (!string.IsNullOrEmpty(this.LpBase.Options.MergerPrivault))
        {// MergerPrivault is valid
            var privault = this.LpBase.Options.MergerPrivault;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.New(KeyOrientation.Signature);
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }

            context.ServiceProvider.GetRequiredService<Merger>().Initialize(crystalizer, seedKey);
            this.NetControl.Services.Register<IMergerClient, MergerClientAgent>();
            this.NetControl.Services.Register<IMergerRemote, MergerRemoteAgent>();
        }

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayMergerPrivault))
        {// RelayMergerPrivault is valid
            var privault = this.LpBase.Options.RelayMergerPrivault;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.New(KeyOrientation.Signature);
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }

            context.ServiceProvider.GetRequiredService<RelayMerger>().Initialize(crystalizer, seedKey);
            this.NetControl.Services.Register<IRelayMergerService, RelayMergerServiceAgent>();
            this.NetControl.Services.Register<IMergerRemote, MergerRemoteAgent>();
        }
    }

    public async Task PrepareLinker(UnitContext context)
    {
        var crystalizer = context.ServiceProvider.GetRequiredService<Crystalizer>();
        if (!string.IsNullOrEmpty(this.LpBase.Options.LinkerPrivault))
        {// LinkerPrivault is valid
            var privault = this.LpBase.Options.LinkerPrivault;
            if (!SeedKey.TryParse(privault, out var privateKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out privateKey, out _))
                {
                    await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Linker.NoPrivateKey, privault);
                    privateKey = SeedKey.New(KeyOrientation.Signature);
                    this.VaultControl.Root.AddObject(privault, privateKey);
                }
            }

            context.ServiceProvider.GetRequiredService<Linker>().Initialize(crystalizer, privateKey);
            // this.NetControl.Services.Register<IMergerClient, MergerClientAgent>();
            // this.NetControl.Services.Register<IMergerRemote, MergerRemoteAgent>();
        }
    }

    public async Task LoadAsync(UnitContext context)
    {
        await context.SendLoadAsync(new(this.LpBase.DataDirectory));
    }

    public async Task AbortAsync()
    {
        // await this.Crystalizer.SaveAllAndTerminate();
    }

    public async Task SaveAsync(UnitContext context)
    {
        Directory.CreateDirectory(this.LpBase.DataDirectory);

        // Vault
        this.VaultControl.Root.AddObject(NetConstants.NodeSecretKeyName, this.NetControl.NetBase.NodeSeedKey);
        await this.VaultControl.SaveAsync();

        await context.SendSaveAsync(new(this.LpBase.DataDirectory));

        await this.Crystalizer.SaveAllAndTerminate();
    }

    public async Task StartAsync(UnitContext context)
    {
        await context.SendStartAsync(new(this.Core));

        this.BigMachine.Start(null);
        this.RunMachines(); // Start machines after context.SendStartAsync (some machines require NetTerminal).

        this.UserInterfaceService.WriteLine();
        var logger = this.Logger.Get<DefaultLog>(LogLevel.Information);
        this.LogInformation(logger);

        logger.Log("Press Enter key to switch to console mode.");
        logger.Log("Press Ctrl+C to exit.");
        logger.Log("Running");
    }

    public void LogInformation(ILogWriter logger)
    {
        logger.Log($"Utc: {Mics.GetUtcNow().MicsToDateTimeString()}");
        this.LpBase.LogInformation(logger);
    }

    public async Task<bool> TryTerminate(bool forceTerminate = false)
    {
        if (forceTerminate)
        {// Force termination
            this.Core.Terminate(); // this.Terminate(false);
            return true;
        }

        if (this.UserInterfaceService.IsInputMode)
        {// Input mode
            return false;
        }

        if (!this.LpBase.Options.ConfirmExit)
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

    public bool Subcommand(string subcommand)
    {
        if (!this.subcommandParser.Parse(subcommand))
        {
            if (this.subcommandParser.HelpCommand != string.Empty)
            {
                this.subcommandParser.ShowHelp();
                return true;
            }
            else
            {
                this.UserInterfaceService.WriteLine("Invalid subcommand.");
                return false;
            }
        }

        this.subcommandParser.Run();
        return true;

        /*if (subcommandParser.HelpCommand != string.Empty)
        {
            return false;
        }

        this.ConsoleService.WriteLine();
        return true;*/
    }

    private async Task MainAsync()
    {
        while (!this.Core.IsTerminated)
        {
            var currentMode = this.UserInterfaceService.CurrentMode;
            if (currentMode == IUserInterfaceService.Mode.Console)
            {// Console mode
                string? command = null;
                try
                {
                    command = await Task.Run(() =>
                    {
                        return this.UserInterfaceService.ReadLine()?.Trim();
                    }).WaitAsync(this.Core.CancellationToken).ConfigureAwait(false);
                }
                catch
                {
                }

                // var command = this.UserInterfaceService.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    if (string.Compare(command, "exit", true) == 0)
                    {// Exit
                        if (this.TryTerminate(true).Result == true)
                        { // To view mode
                            this.UserInterfaceService.ChangeMode(IUserInterfaceService.Mode.View);
                            return;
                        }
                        else
                        {
                            this.UserInterfaceService.Write("> ");
                            continue;
                        }
                    }
                    else
                    {// Subcommand
                        try
                        {
                            this.Subcommand(command);
                            this.UserInterfaceService.Write("> ");
                            continue;
                        }
                        catch (Exception e)
                        {
                            this.UserInterfaceService.WriteLine(e.ToString());
                            break;
                        }
                    }
                }
                else
                {
                    this.UserInterfaceService.WriteLine();
                }

                // To view mode
                this.UserInterfaceService.ChangeMode(IUserInterfaceService.Mode.View);
            }
            else if (currentMode == IUserInterfaceService.Mode.View)
            {// View mode
                if (this.UserInterfaceService.KeyAvailable)
                {
                    var keyInfo = this.UserInterfaceService.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                    { // To console mode
                        this.UserInterfaceService.ChangeMode(IUserInterfaceService.Mode.Console);
                        this.UserInterfaceService.Write("> ");
                    }
                    else
                    {
                        while (this.UserInterfaceService.KeyAvailable)
                        {
                            this.UserInterfaceService.ReadKey(true);
                        }
                    }
                }
            }

            this.Core.Sleep(100, 100);
        }

        // To view mode
        this.UserInterfaceService.ChangeMode(IUserInterfaceService.Mode.View);
    }

    private void RunMachines()
    {
        _ = this.BigMachine.NtpMachine.GetOrCreate().RunAsync();
        // _ = this.BigMachine.NetStatsMachine.GetOrCreate().RunAsync();
        _ = this.BigMachine.NodeControlMachine.GetOrCreate().RunAsync();
        this.BigMachine.LpControlMachine.GetOrCreate(); // .RunAsync();

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayPeerPrivault))
        {
            this.BigMachine.RelayPeerMachine.GetOrCreate();
        }
    }

    private async Task LoadKeyVault_NodeKey()
    {
        if (this.NetControl.NetBase.IsValidNodeKey)
        {
            return;
        }

        if (!this.VaultControl.Root.TryGetObject<SeedKey>(NetConstants.NodeSecretKeyName, out var key, out _))
        {// Failure
            if (!this.VaultControl.NewlyCreated)
            {
                await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Vault.NoData, NetConstants.NodeSecretKeyName);
            }

            return;
        }

        if (!this.NetControl.NetBase.SetNodeSeedKey(key))
        {
            await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Vault.NoRestore, NetConstants.NodeSecretKeyName);
            return;
        }
    }

    private async Task TerminateAsync(UnitContext context)
    {
        this.Logger.Get<DefaultLog>().Log("Termination process initiated");

        try
        {
            await context.SendTerminateAsync(new());
        }
        catch
        {
        }
    }

    private void Terminate(bool abort)
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);

        this.Logger.Get<DefaultLog>().Log(abort ? "Aborted" : "Terminated");
        this.Logger.FlushAndTerminate().Wait(); // Write logs added after Terminate().
    }
}
