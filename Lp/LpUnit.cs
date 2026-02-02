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
using Lp.Logging;
using Lp.Net;
using Lp.NetServices;
using Lp.Services;
using Lp.T3cs;
using Lp.T3cs.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netsphere.Crypto;
using Netsphere.Relay;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp;

public class LpUnit
{
    #region Builder

    public class Builder : UnitBuilder<Product>
    {
        public Builder()
            : base()
        {
            this.PreConfigure(context =>
            {
                // SimpleConsole
                var simpleConsole = SimpleConsole.GetOrCreate();

                this.LoadStrings();
                this.LoadLpOptions(context);
            });

            this.Configure(context =>
            {
                // Main services
                context.AddSingleton<RobustConnection.Factory>();
                context.AddSingleton<LpUnit>();
                context.AddSingleton<LpBase>();
                context.AddSingleton<LpOptions>();
                context.AddSingleton<NetsphereLoggerOptions>();
                context.AddSingleton<LpService>();
                context.AddSingleton<LpBoardService>();
                context.Services.TryAddSingleton<SimpleConsole>(sp => SimpleConsole.GetOrCreate());
                context.AddSingleton<ConsoleUserInterfaceService>();
                context.Services.TryAddSingleton<IConsoleService>(sp => sp.GetRequiredService<ConsoleUserInterfaceService>());
                context.Services.TryAddSingleton<IUserInterfaceService>(sp => sp.GetRequiredService<ConsoleUserInterfaceService>());
                context.AddSingleton<VaultControl>();
                context.AddTransient<Vault>();
                context.AddSingleton<IStorageKey, StorageKeyVault>();
                context.AddSingleton<AuthorityControl>();
                context.AddSingleton<DomainControl>();
                context.AddSingleton<DomainServiceAgent>();

                context.AddSingleton<Credentials>();
                context.AddSingleton<Merger>();
                context.AddSingleton<RelayMerger>();
                context.AddSingleton<Linker>();
                ConfigureRelay(context);

                // RPC / Services
                context.AddSingleton<NetServices.RemoteBenchControl>();
                context.AddSingleton<NetServices.RemoteBenchHostAgent>();
                context.AddTransient<Lp.T3cs.MergerServiceAgent>();
                context.AddTransient<Lp.T3cs.MergerAdministrationAgent>();
                context.AddTransient<Lp.Net.BasalServiceAgent>();
                context.AddTransient<RelayMergerServiceAgent>();
                context.AddTransient<LpDogmaAgent>();
                // context.AddSingleton<DomainServer>();

                // RPC / Filters
                context.AddTransient<NetServices.TestOnlyFilter>();
                context.AddTransient<NetServices.MergerOrTestFilter>();

                // Machines
                context.AddSingleton<BigMachine>();
                context.AddSingleton<BigMachineBase, BigMachine>();
                context.AddTransient<Machines.TemplateMachine>();
                context.AddTransient<Machines.LogTesterMachine>();
                context.AddTransient<Machines.LpControlMachine>();
                // context.AddTransient<T3cs.Domain.DomainMachine>();
                context.AddSingleton<Machines.RelayPeerMachine>();
                context.AddSingleton<Machines.NodeControlMachine>();
                context.AddSingleton<Services.LpDogmaMachine>();

                // Subcommands
                context.AddSubcommand(typeof(Lp.Subcommands.InspectSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.OpenDataDirectorySubcommand));
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
                context.AddSubcommand(typeof(Lp.Subcommands.LpDogmaGetInformationSubcommand));

                context.AddSubcommand(typeof(Lp.Subcommands.Credential.ShowCredentialsCommand));

                context.AddSubcommand(typeof(Lp.Subcommands.LpCreateCreditSubcommand));

                context.AddSubcommand(typeof(Lp.T3cs.Domain.AssignDomainMachineSubcommand));
                context.AddSubcommand(typeof(Lp.T3cs.Domain.ShowDomainMachineSubcommand));

                // Lp.Subcommands.CrystalData.CrystalStorageSubcommand.Configure(context);
                // Lp.Subcommands.CrystalData.CrystalDataSubcommand.Configure(context);

                Lp.Subcommands.ExportSubcommand.Configure(context);
                Lp.Subcommands.FlagSubcommand.Configure(context);
                Lp.Subcommands.AuthorityCommand.Subcommand.Configure(context);
                Lp.Subcommands.VaultCommand.Subcommand.Configure(context);
                Lp.Subcommands.CommandGroup.Configure(context);
                Lp.Subcommands.MergerClient.NestedCommand.Configure(context);
                Lp.Subcommands.MergerRemote.NestedCommand.Configure(context);
                Lp.Subcommands.Relay.Subcommand.Configure(context);
                Lp.Subcommands.KeyCommand.Subcommand.Configure(context);
                Lp.Subcommands.T3cs.Subcommand.Configure(context);
            });

            this.PostConfigure(context =>
            {
                // FileLoggerOptions
                context.SetOptions(context.GetOptions<FileLoggerOptions>() with
                {
                    Path = Path.Combine(context.DataDirectory, "Logs/Log.txt"),
                    MaxLogCapacity = 20,
                });

                // NetsphereLoggerOptions
                var netsphereLoggerOptions = context.GetOptions<Lp.Logging.NetsphereLoggerOptions>();
                context.SetOptions(netsphereLoggerOptions with
                {
                    Path = Path.Combine(context.DataDirectory, "Logs/Net.txt"),
                    MaxLogCapacity = 100,
                    Formatter = netsphereLoggerOptions.Formatter with { TimestampFormat = "mm:ss.ffffff K", },
                    ClearLogsAtStartup = true,
                    MaxQueue = 100_000,
                });

                // ConsoleLoggerOptions
                var lpOptions = context.GetOptions<LpOptions>();
                var consoleLoggerOptions = context.GetOptions<ConsoleLoggerOptions>();
                context.SetOptions(consoleLoggerOptions with
                {
                    FormatterOptions = consoleLoggerOptions.FormatterOptions with
                    {
                        EnableColor = lpOptions.ColorConsole,
                    },
                });

                var netOptions = context.GetOptions<NetOptions>();

                var lpBase = context.ServiceProvider.GetRequiredService<LpBase>();
                lpBase.Initialize(context.DataDirectory, lpOptions, true, "merger");

                var netBase = context.ServiceProvider.GetRequiredService<NetBase>();
                netBase.SetOptions(lpOptions.ToNetOptions());
                netBase.AllowUnsafeConnection = true; // betacode
                netBase.NetOptions.EnableServer = true; // betacode
                netBase.DefaultAgreement = netBase.DefaultAgreement with { MaxStreamLength = 100_000_000, }; // betacode

                context.SetOptions(context.GetOptions<CrystalOptions>() with
                {
                    DefaultSaveFormat = SaveFormat.Utf8,
                    SaveInterval = TimeSpan.FromMinutes(10),
                    GlobalDirectory = new LocalDirectoryConfiguration(context.DataDirectory),
                    EnableFilerLogger = false,
                });
            });

            var crystalControlBuilder = CrystalBuilder();

            this.AddBuilder(new NetUnit.Builder());
            this.AddBuilder(crystalControlBuilder);
            this.AddBuilder(new Lp.Logging.LpLogger.Builder());
        }

        #endregion

        private static void ConfigureRelay(IUnitConfigurationContext context)
        {
            var options = context.GetOptions<LpOptions>();
            if (SignaturePublicKey.TryParse(options.CertificateRelayPublicKey, out var relayPublicKey, out _))
            {// CertificateRelayControl
                context.AddSingleton<IRelayControl, CertificateRelayControl>();
            }
        }

        private static CrystalUnit.Builder CrystalBuilder()
        {
            return new CrystalUnit.Builder()
                .ConfigureCrystal(context =>
                {
                    context.AddCrystal<LpSettings>(new()
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(LpSettings.Filename),
                        RequiredForLoading = true,
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

                    context.AddCrystal<Lp.Services.LpDogma>(new CrystalConfiguration() with
                    {
                        NumberOfFileHistories = 0,
                        FileConfiguration = new GlobalFileConfiguration(Lp.Services.LpDogma.Filename),
                    });

                    context.AddCrystal<DomainControl>(new CrystalConfiguration() with
                    {
                        NumberOfFileHistories = 2,
                        FileConfiguration = new GlobalFileConfiguration(DomainControl.Filename),
                    });

                    /*context.AddCrystal<DomainStorage>(new CrystalConfiguration() with
                    {
                        NumberOfFileHistories = 2,
                        FileConfiguration = new GlobalFileConfiguration(DomainStorage.Filename),
                    });*/
                });
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

        private void LoadLpOptions(IUnitPreConfigurationContext context)
        {
            var args = context.Arguments.RawArguments;
            LpOptions? options = null;

            if (context.Arguments.TryGetOptionValue("loadoptions", out var optionFile))
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

            // Set data directory
            var dataDirectory = options?.DataDirectory;
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = "Local";
            }

            if (!Path.IsPathRooted(dataDirectory))
            {
                dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), dataDirectory);
            }

            context.DataDirectory = dataDirectory;

            if (options is not null)
            {
                context.SetOptions(options);
            }
        }
    }

    #region Product

    public class Product : UnitProduct
    {
        public Product(UnitContext context)
            : base(context)
        {
            TinyhandSerializer.ServiceProvider = context.ServiceProvider;
        }

        public async Task RunAsync(LpOptions options)
        {
            try
            {
                // CrystalControl
                var crystalControl = this.Context.ServiceProvider.GetRequiredService<CrystalControl>();

                // Vault
                var vaultControl = this.Context.ServiceProvider.GetRequiredService<VaultControl>();
                await vaultControl.LoadAsync();
                ((StorageKeyVault)this.Context.ServiceProvider.GetRequiredService<IStorageKey>()).VaultControl = vaultControl;

                // Load
                var result = await crystalControl.PrepareAndLoad();
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

            var lpUnit = this.Context.ServiceProvider.GetRequiredService<LpUnit>();
            try
            {
                LpConstants.Initialize();

                // Start
                lpUnit.UnitLogger.Get<DefaultLog>().Log($"Lp ({Netsphere.Version.VersionHelper.VersionString})");

                // Prepare
                await lpUnit.DomainControl.Prepare(this.Context);
                await lpUnit.PrepareMaster(this.Context);
                await lpUnit.PrepareMerger(this.Context);
                await lpUnit.PrepareRelay(this.Context);
                await lpUnit.PrepareLinker(this.Context);
                await lpUnit.PreparePeer(this.Context);

                // Vault -> NodeKey
                await lpUnit.LoadKeyVault_NodeKey();

                // Create optional instances
                this.Context.CreateInstances();

                // Prepare
                await this.Context.SendPrepare();
            }
            catch
            {
                await lpUnit.Save(this.Context);
                lpUnit.Terminate(true);
                return;
            }

            try
            {// Load
                await lpUnit.LoadAsync(this.Context);
            }
            catch
            {
                await lpUnit.AbortAsync();
                lpUnit.Terminate(true);
                return;
            }

            try
            {// Start, Main loop
                await lpUnit.Start(this.Context);

                await lpUnit.MainAsync();

                await this.Context.SendStop();
                await lpUnit.TerminateAsync(this.Context);
                await lpUnit.Save(this.Context);
                lpUnit.Terminate(false);
            }
            catch
            {
                await lpUnit.TerminateAsync(this.Context);
                await lpUnit.Save(this.Context);
                lpUnit.Terminate(true);
                return;
            }
        }
    }

    #endregion

    public LpUnit(UnitContext context, UnitCore core, UnitLogger unitLogger, ILogger<LpUnit> logger, IUserInterfaceService userInterfaceService, SimpleConsole simpleConsole, LpBase lpBase, BigMachine bigMachine, NetUnit netsphere, CrystalControl crystalControl, VaultControl vault, AuthorityControl authorityControl, DomainControl domainControl, LpSettings settings, Merger merger, RelayMerger relayMerger, Linker linker, LpService lpService)
    {
        this.UnitLogger = unitLogger;
        this.logger = logger;
        this.UserInterfaceService = userInterfaceService;
        this.simpleConsole = simpleConsole;
        this.simpleConsole.Core = core;
        this.LpBase = lpBase;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.NetUnit = netsphere;
        this.CrystalControl = crystalControl;
        this.VaultControl = vault;
        this.AuthorityControl = authorityControl;
        this.DomainControl = domainControl;
        this.LpBase.Settings = settings;
        this.Merger = merger;
        this.RelayMerger = relayMerger;
        this.Linker = linker;
        this.lpService = lpService;

        if (this.LpBase.Options.TestFeatures)
        {
            NetAddress.SkipValidation = true;
            this.NetUnit.Services.Register<IRemoteBenchHost, RemoteBenchHostAgent>();
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

    public UnitLogger UnitLogger { get; }

    public UnitCore Core { get; }

    public IUserInterfaceService UserInterfaceService { get; }

    public LpBase LpBase { get; }

    public BigMachine BigMachine { get; }

    public NetUnit NetUnit { get; }

    public Merger Merger { get; }

    public RelayMerger RelayMerger { get; }

    public Linker Linker { get; }

    public CrystalControl CrystalControl { get; }

    public VaultControl VaultControl { get; }

    public AuthorityControl AuthorityControl { get; }

    public DomainControl DomainControl { get; }

    private readonly ILogger logger;
    private readonly SimpleParser subcommandParser;
    private readonly SimpleConsole simpleConsole;
    private readonly LpService lpService;

    public async Task PreparePeer(UnitContext context)
    {
        this.NetUnit.Services.Register<IBasalService, BasalServiceAgent>();

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayPeerPrivault))
        {// RelayPeerPrivault is valid
            var privault = this.LpBase.Options.RelayPeerPrivault;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    await this.UserInterfaceService.Notify(default, LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
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
                    await this.UserInterfaceService.Notify(default, LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.NewSignature();
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }
        }

        if (!string.IsNullOrEmpty(this.LpBase.Options.CreditPeer))
        {// Credit peer
            // this.BigMachine.DomainMachine.GetOrCreate(DomainMachineKind.CreditPeer, this.LpBase.Options.CreditPeer);
        }
    }

    public async Task PrepareRelay(UnitContext context)
    {
        if (context.ServiceProvider.GetService<IRelayControl>() is CertificateRelayControl certificateRelayControl)
        {
            if (SignaturePublicKey.TryParse(this.LpBase.Options.CertificateRelayPublicKey, out var relayPublicKey, out _))
            {
                certificateRelayControl.SetCertificatePublicKey(relayPublicKey);
                this.UnitLogger.Get<CertificateRelayControl>().Log($"Active: {relayPublicKey.ToString()}");
            }
        }
    }

    public async Task PrepareMaster(UnitContext context)
    {
        var key = this.LpBase.Options.MasterKey;
        if (!string.IsNullOrEmpty(key))
        {
            if (!MasterKey.TryParse(key, out var masterKey, out _))
            {
                this.logger?.TryGet(LogLevel.Error)?.Log(Hashed.Error.InvalidMasterKey);
                return;
            }

            if (string.IsNullOrEmpty(this.LpBase.Options.NodeSecretKey))
            {
                (_, var seedKey) = masterKey.CreateSeedKey(MasterKey.Kind.Node);
                this.LpBase.Options.NodeSecretKey = seedKey.UnsafeToString();
            }

            if (string.IsNullOrEmpty(this.LpBase.Options.MergerCode))
            {
                (_, var seedKey) = masterKey.CreateSeedKey(MasterKey.Kind.Merger);
                this.LpBase.Options.MergerCode = seedKey.UnsafeToString();
            }

            if (string.IsNullOrEmpty(this.LpBase.Options.RelayMergerCode))
            {
                (_, var seedKey) = masterKey.CreateSeedKey(MasterKey.Kind.RelayMerger);
                this.LpBase.Options.RelayMergerCode = seedKey.UnsafeToString();
            }

            if (string.IsNullOrEmpty(this.LpBase.Options.LinkerCode))
            {
                (_, var seedKey) = masterKey.CreateSeedKey(MasterKey.Kind.Linker);
                this.LpBase.Options.LinkerCode = seedKey.UnsafeToString();
            }
        }
    }

    public async Task PrepareMerger(UnitContext context)
    {
        var crystalControl = context.ServiceProvider.GetRequiredService<CrystalControl>();

        var code = this.LpBase.Options.MergerCode;
        if (!string.IsNullOrEmpty(code))
        {// Enable merger
            var seedKey = await this.lpService.GetSeedKeyFromCode(code);
            if (seedKey is null)
            {
                seedKey = SeedKey.New(KeyOrientation.Signature);
                this.VaultControl.Root.AddObject(code, seedKey);
            }

            context.ServiceProvider.GetRequiredService<Merger>().Initialize(crystalControl, seedKey);
            this.NetUnit.Services.Register<IMergerService, MergerServiceAgent>();
            this.NetUnit.Services.Register<LpDogmaNetService, LpDogmaAgent>();

            if (this.LpBase.RemotePublicKey.IsValid)
            {
                this.NetUnit.Services.Register<IMergerAdministration, MergerAdministrationAgent>();
            }
        }

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayMergerCode))
        {// RelayMergerCode is valid
            var privault = this.LpBase.Options.RelayMergerCode;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    await this.UserInterfaceService.Notify(default, LogLevel.Error, Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.New(KeyOrientation.Signature);
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }

            context.ServiceProvider.GetRequiredService<RelayMerger>().Initialize(crystalControl, seedKey);
            this.NetUnit.Services.Register<IRelayMergerService, RelayMergerServiceAgent>();
            this.NetUnit.Services.Register<LpDogmaNetService, LpDogmaAgent>();
        }
    }

    public async Task PrepareLinker(UnitContext context)
    {
        var crystalControl = context.ServiceProvider.GetRequiredService<CrystalControl>();
        if (!string.IsNullOrEmpty(this.LpBase.Options.LinkerCode))
        {// LinkerCode is valid
            var seedKey = await this.lpService.GetSeedKeyFromCode(this.LpBase.Options.LinkerCode).ConfigureAwait(false);
            /*if (!SeedKey.TryParse(code, out var privateKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(code, out privateKey, out _))
                {
                    await this.UserInterfaceService.Notify(LogLevel.Error, Hashed.Linker.NoPrivateKey, code);
                    privateKey = SeedKey.New(KeyOrientation.Signature);
                    this.VaultControl.Root.AddObject(code, privateKey);
                }
            }*/

            if (seedKey is not null)
            {
                context.ServiceProvider.GetRequiredService<Linker>().Initialize(crystalControl, seedKey);
            }

            // this.NetUnit.Services.Register<IMergerClient, MergerClientAgent>();
            // this.NetUnit.Services.Register<IMergerRemote, MergerRemoteAgent>();
        }
    }

    public async Task LoadAsync(UnitContext context)
    {
        await context.SendLoad();
    }

    public async Task AbortAsync()
    {
        // await this.CrystalControl.SaveAllAndTerminate();
    }

    public async Task Save(UnitContext context)
    {
        this.UnitLogger.Get<DefaultLog>().Log("SaveAsync - 0"); //
        Directory.CreateDirectory(this.LpBase.DataDirectory);

        // Vault
        this.VaultControl.Root.AddObject(NetConstants.NodeSecretKeyName, this.NetUnit.NetBase.NodeSeedKey);
        await this.VaultControl.SaveAsync();

        this.UnitLogger.Get<DefaultLog>().Log("SaveAsync - 1");
        await context.SendSave();

        this.UnitLogger.Get<DefaultLog>().Log("SaveAsync - 2");
        await this.CrystalControl.StoreAndRip();
        this.UnitLogger.Get<DefaultLog>().Log("SaveAsync - 3");
    }

    public async Task Start(UnitContext context)
    {
        await context.SendStart();

        this.BigMachine.Start(null);
        this.RunMachines(); // Start machines after context.SendStartAsync (some machines require NetTerminal).

        this.UserInterfaceService.WriteLine();
        var logger = this.UnitLogger.Get<DefaultLog>(LogLevel.Information);
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

        if (!this.LpBase.Options.ConfirmExit)
        {// No confirmation
            this.Core.Terminate(); // this.Terminate(false);
            return true;
        }

        var result = await this.UserInterfaceService.ReadYesNo(false, Hashed.Dialog.ConfirmExit);
        if (result == InputResultKind.No ||
            result == InputResultKind.Canceled)
        {
            return false;
        }

        this.Core.Terminate(); // this.Terminate(false);
        return true;
    }

    public bool Subcommand(string subcommand)
    {
        if (subcommand == SimpleParser.HelpString ||
            subcommand == "?")
        {
            this.subcommandParser.ShowCommandList();
            return true;
        }

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
        var defaultComparison = StringComparison.InvariantCultureIgnoreCase;
        var options = new ReadLineOptions()
        {
            Prompt = LpConstants.PromptString,
            MultilinePrompt = LpConstants.MultilinePromptString,
        };

        while (!this.Core.IsTerminated)
        {
            var inputResult = await this.simpleConsole.ReadLine(options);
            if (inputResult.Kind == InputResultKind.Terminated)
            {
                return;
            }
            else if (inputResult.Kind == InputResultKind.Canceled)
            {
                continue;
            }

            if (string.Equals(inputResult.Text, "exit", defaultComparison))
            {// Exit
                if (await this.TryTerminate(false))
                {// Terminate
                    return;
                }
            }
            else
            {// Subcommand
                try
                {
                    this.Subcommand(inputResult.Text);
                    continue;
                }
                catch (Exception e)
                {
                    this.UserInterfaceService.WriteLine(e.ToString());
                    break;
                }
            }
        }
    }

    private void RunMachines()
    {
        _ = this.BigMachine.NtpMachine.GetOrCreate().RunAsync();
        // _ = this.BigMachine.NetStatsMachine.GetOrCreate().RunAsync();
        _ = this.BigMachine.NodeControlMachine.GetOrCreate().RunAsync();
        this.BigMachine.LpControlMachine.GetOrCreate(); // .RunAsync();
        this.BigMachine.LpDogmaMachine.GetOrCreate(); // .RunAsync();

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayPeerPrivault))
        {
            this.BigMachine.RelayPeerMachine.GetOrCreate();
        }
    }

    private async Task LoadKeyVault_NodeKey()
    {
        if (this.NetUnit.NetBase.IsValidNodeKey)
        {
            return;
        }

        if (!this.VaultControl.Root.TryGetObject<SeedKey>(NetConstants.NodeSecretKeyName, out var key, out _))
        {// Failure
            if (!this.VaultControl.NewlyCreated)
            {
                await this.UserInterfaceService.Notify(default, LogLevel.Error, Hashed.Vault.NoData, NetConstants.NodeSecretKeyName);
            }

            return;
        }

        if (!this.NetUnit.NetBase.SetNodeSeedKey(key))
        {
            await this.UserInterfaceService.Notify(default, LogLevel.Error, Hashed.Vault.NoRestore, NetConstants.NodeSecretKeyName);
            return;
        }
    }

    private async Task TerminateAsync(UnitContext context)
    {
        this.UnitLogger.Get<DefaultLog>().Log("Termination process initiated");

        try
        {
            await context.SendTerminate();
        }
        catch
        {
        }
    }

    private void Terminate(bool abort)
    {
        this.Core.Terminate();
        this.Core.WaitForTermination(-1);

        this.UnitLogger.Get<DefaultLog>().Log(abort ? "Aborted" : "Terminated");
        this.UnitLogger.FlushAndTerminate().Wait(); // Write logs added after Terminate().
    }
}
