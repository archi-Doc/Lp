// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

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
using Lp.Subcommands;
using Lp.T3cs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netsphere.Crypto;
using Netsphere.Relay;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp;

public class LpUnit
{
    public static readonly Type[] RemoteSubcommands = [
        typeof(FreezeSubcommand),
        typeof(InspectSubcommand),
        typeof(BenchmarkSubcommand),
        typeof(ShowOwnNetNodeSubcommand),
        typeof(ShowNodeControlStateSubcommand),
        typeof(TestSubcommand),
        typeof(Lp.Subcommands.OperateCredit.OperateCreditCommand),
    ];

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
                context.AddSingleton<CreditService>();
                context.AddSingleton<ExecutionStack>();//

                // Console services
                context.Services.TryAddSingleton<SimpleConsole>(sp => SimpleConsole.GetOrCreate());
                context.AddSingleton<ConsoleUserInterfaceService>();
                context.Services.AddScoped<UserInterfaceServiceContext>();
                context.Services.TryAddScoped<IUserInterfaceService>(sp =>
                {
                    var context = sp.GetService<UserInterfaceServiceContext>();
                    var console = sp.GetRequiredService<ConsoleUserInterfaceService>();
                    if (context?.Receiver is { } receiver)
                    {
                        return new RemoteUserInterfaceService(receiver, console);
                    }
                    else
                    {
                        return console;
                    }
                });
                context.Services.TryAddScoped<IConsoleService>(sp => sp.GetRequiredService<IUserInterfaceService>());

                context.Services.TryAddSingleton<SimpleParser>(sp => sp.GetRequiredService<LpUnit>().subcommandParser);
                context.AddSingleton<VaultControl>();
                context.AddTransient<Vault>();
                context.AddSingleton<IStorageKey, StorageKeyVault>();
                context.AddSingleton<AuthorityControl>();
                context.AddSingleton<DomainControl>();
                context.AddSingleton<DomainRadiant>();
                context.AddSingleton<DomainServiceAgent>();
                context.AddSingleton<RemoteBenchControl>();

                context.AddSingleton<Credentials>();
                context.AddSingleton<Merger>();
                context.AddSingleton<RelayMerger>();
                context.AddSingleton<Linker>();
                ConfigureRelay(context);

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
                context.AddSingleton<T3cs.Domain.DomainMachine>();
                context.AddSingleton<Machines.RelayPeerMachine>();
                context.AddSingleton<Machines.NodeControlMachine>();
                context.AddSingleton<Services.LpDogmaMachine>();

                // Subcommands
                context.AddSubcommand(typeof(Lp.Subcommands.TemplateSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.FreezeSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.InspectSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.OpenDataDirectorySubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.TestSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.MicsSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.GCSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.PingSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.RestartRemoteContainerSubcommand));
                context.AddSubcommand(typeof(Lp.Subcommands.RemoteSubcommand));
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
                context.AddSubcommand(typeof(Lp.Subcommands.OperateCredit.OperateCreditCommand));

                context.AddSubcommand(typeof(Lp.Subcommands.Credential.ShowCredentialsCommand));

                context.AddSubcommand(typeof(Lp.Subcommands.LpCreateCreditSubcommand));

                context.AddSubcommand(typeof(Lp.T3cs.Domain.AddDomainSubcommand));
                context.AddSubcommand(typeof(Lp.T3cs.Domain.RemoveDomainSubcommand));
                context.AddSubcommand(typeof(Lp.T3cs.Domain.ListDomainSubcommand));
                context.AddSubcommand(typeof(Lp.T3cs.Domain.ShowDomainMachineSubcommand));

                // Lp.Subcommands.CrystalData.CrystalStorageSubcommand.Configure(context);
                // Lp.Subcommands.CrystalData.CrystalDataSubcommand.Configure(context);

                Lp.Subcommands.ExportSubcommand.Configure(context);
                // Lp.Subcommands.FlagSubcommand.Configure(context);
                Lp.Subcommands.AuthorityCommand.Subcommand.Configure(context);
                Lp.Subcommands.VaultCommand.Subcommand.Configure(context);
                Lp.Subcommands.CommandGroup.Configure(context);
                Lp.Subcommands.MergerClient.NestedCommand.Configure(context);
                Lp.Subcommands.MergerRemote.NestedCommand.Configure(context);
                Lp.Subcommands.Relay.Subcommand.Configure(context);
                Lp.Subcommands.KeyCommand.Subcommand.Configure(context);
                Lp.Subcommands.OperateCredit.NestedCommand.Configure(context);
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
                    var defaultStorage = new SimpleStorageConfiguration(new GlobalDirectoryConfiguration("Storage"))
                    {
                        // NumberOfHistoryFiles = 0,
                    };

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

                    context.AddCrystal<CreditPoint.GoshujinClass>(new CrystalConfiguration() with
                    {
                        SaveFormat = SaveFormat.Binary,
                        NumberOfFileHistories = 3,
                        FileConfiguration = new GlobalFileConfiguration("Credits"),
                        StorageConfiguration = defaultStorage,
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

        public async Task Run(LpOptions options)
        {
            try
            {
                // This part is a bit complicated: VaultControl, ConsoleUserInterfaceService, and CrystalData end up with a circular dependency, so loading of LpSettings is deferred.

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

                this.Context.ServiceProvider.GetRequiredService<ConsoleUserInterfaceService>().Load(crystalControl);
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
                lpUnit.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write($"Lp ({Arc.VersionHelper.VersionString})");

                // Prepare
                await lpUnit.PrepareMaster(this.Context);
                await lpUnit.PrepareMerger(this.Context);
                await lpUnit.PrepareRelay(this.Context);
                await lpUnit.PrepareLinker(this.Context);
                await lpUnit.PreparePeer(this.Context);
                await lpUnit.DomainControl.Prepare(this.Context); // Since the Merger must be prepared first, process DomainControl last.

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

                await lpUnit.Main(this.Context);

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

    public LpUnit(UnitContext context, UnitCore core, ExecutionStack executionStack, LogUnit logUnit, ILogger<LpUnit> logger, IUserInterfaceService userInterfaceService, SimpleConsole simpleConsole, LpBase lpBase, BigMachine bigMachine, NetUnit netsphere, CrystalControl crystalControl, VaultControl vault, AuthorityControl authorityControl, DomainControl domainControl, LpSettings settings, Merger merger, RelayMerger relayMerger, Linker linker, LpService lpService)
    {
        this.ExecutionStack = executionStack;
        this.LogUnit = logUnit;
        this.logger = logger;
        this.UserInterfaceService = userInterfaceService;
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

        this.simpleConsole = simpleConsole;
        this.simpleConsole.Core = core;
        this.simpleConsole.KeyInputHook = (ref keyInfo) =>
        {
            if (keyInfo.Modifiers == ConsoleModifiers.Control)
            {
                if (keyInfo.Key == ConsoleKey.Q)
                {// Ctrl+Q
                    if (this.ExecutionStack.CancelTop())
                    {
                        this.UserInterfaceService.WriteLineError("Canceled");
                    }

                    return KeyInputHookResult.Handled;
                }
            }

            return KeyInputHookResult.NotHandled;
        };

        if (this.LpBase.Options.TestFeatures)
        {
            NetAddress.SkipValidation = true;
            this.NetUnit.Services.EnableNetService<IRemoteBenchHost>();
        }

        if (this.LpBase.RemotePublicKey.IsValid)
        {
            this.NetUnit.Services.EnableNetService<IRemoteUserInterfaceSender>();
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

    public LogUnit LogUnit { get; }

    public UnitCore Core { get; }

    public ExecutionStack ExecutionStack { get; }

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
        this.NetUnit.Services.EnableNetService<IBasalService>();

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayPeerPrivault))
        {// RelayPeerPrivault is valid
            var privault = this.LpBase.Options.RelayPeerPrivault;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    this.UserInterfaceService.WriteLineError(Hashed.Merger.NoPrivateKey, privault);
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
                    this.UserInterfaceService.WriteLineError(Hashed.Merger.NoPrivateKey, privault);
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
                this.LogUnit.RootLogService.GetWriter<CertificateRelayControl>()?.Write($"Active: {relayPublicKey.ToString()}");
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
                this.logger?.GetWriter(LogLevel.Error)?.Write(Hashed.Error.InvalidMasterKey);
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
            this.NetUnit.Services.EnableNetService<IMergerService>();
            this.NetUnit.Services.EnableNetService<LpDogmaNetService>();

            if (this.LpBase.RemotePublicKey.IsValid)
            {
                this.NetUnit.Services.EnableNetService<IMergerAdministration>();
            }
        }

        if (!string.IsNullOrEmpty(this.LpBase.Options.RelayMergerCode))
        {// RelayMergerCode is valid
            var privault = this.LpBase.Options.RelayMergerCode;
            if (!SeedKey.TryParse(privault, out var seedKey))
            {// 1st: Tries to parse as SignaturePrivateKey, 2nd : Tries to get from Vault.
                if (!this.VaultControl.Root.TryGetObject<SeedKey>(privault, out seedKey, out _))
                {
                    this.UserInterfaceService.WriteLineError(Hashed.Merger.NoPrivateKey, privault);
                    seedKey = SeedKey.New(KeyOrientation.Signature);
                    this.VaultControl.Root.AddObject(privault, seedKey);
                }
            }

            context.ServiceProvider.GetRequiredService<RelayMerger>().Initialize(crystalControl, seedKey);
            this.NetUnit.Services.EnableNetService<IRelayMergerService>();
            this.NetUnit.Services.EnableNetService<LpDogmaNetService>();
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
        this.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write("SaveAsync - 0");
        Directory.CreateDirectory(this.LpBase.DataDirectory);

        // Vault
        this.VaultControl.Root.AddObject(NetConstants.NodeSecretKeyName, this.NetUnit.NetBase.NodeSeedKey);
        await this.VaultControl.SaveAsync();

        this.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write("SaveAsync - 1");
        await context.SendSave();

        this.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write("SaveAsync - 2");
        await this.CrystalControl.StoreAndRip();
        this.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write("SaveAsync - 3");
    }

    public async Task Start(UnitContext context)
    {
        await context.SendStart();

        context.ServiceProvider.GetRequiredService<ClockHand>().Start();
        this.BigMachine.Start(null);
        this.RunMachines(); // Start machines after context.SendStartAsync (some machines require NetTerminal).

        this.UserInterfaceService.WriteLine();
        var logger = this.LogUnit.RootLogService.GetWriter<DefaultLog>(LogLevel.Information);
        this.LogInformation(logger);

        logger?.Write("Press Ctrl+C to exit, Ctrl+Q to cancel the task");
        logger?.Write("Running");
    }

    public void LogInformation(LogWriter? logWriter)
    {
        logWriter?.Write($"Utc: {Mics.GetUtcNow().MicsToDateTimeString()}");
        this.LpBase.LogInformation(logWriter);
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

    public Task Subcommand(string subcommand, CancellationToken cancellationToken)
    {
        if (subcommand == SimpleParser.HelpString)
        {
            this.subcommandParser.ShowHelp();
            return Task.CompletedTask;
        }
        else if (subcommand == "h")
        {
            this.subcommandParser.ShowCommandList();
            return Task.CompletedTask;
        }

        if (!this.subcommandParser.Parse(subcommand))
        {
            if (this.subcommandParser.HelpCommand != string.Empty)
            {
                this.subcommandParser.ShowHelp();
                return Task.CompletedTask;
            }
            else
            {
                this.UserInterfaceService.WriteLine("Invalid subcommand.");
                return Task.CompletedTask;
            }
        }

        return this.subcommandParser.Execute(cancellationToken);
        // return Task.Run(() => this.subcommandParser.Execute(cancellationToken));
    }

    private async Task Main(UnitContext context)
    {
        var defaultComparison = StringComparison.InvariantCultureIgnoreCase;
        var options = new ReadLineOptions()
        {
            Prompt = LpConstants.PromptString,
            MultilineDelimiter = LpConstants.MultilineIndeitifierString,
            MultilinePrompt = LpConstants.MultilinePromptString,
            KeyInputHook = (ref keyInfo) =>
            {
                if (keyInfo.Modifiers == ConsoleModifiers.Control &&
                keyInfo.Key == ConsoleKey.C)
                {// Ctrl+C
                    _ = this.TryTerminate();
                    return KeyInputHookResult.Handled;
                }

                return KeyInputHookResult.NotHandled;
            },
        };

        while (!this.Core.IsTerminated)
        {
            var inputResult = await this.simpleConsole.ReadLine(options).ConfigureAwait(false);
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
                using (var scope = this.ExecutionStack.Push())
                {
                    try
                    {
                        await this.Subcommand(inputResult.Text, scope.CancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        this.UserInterfaceService.WriteLine(e.ToString());
                    }
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
                this.UserInterfaceService.WriteLineError(Hashed.Vault.NoData, NetConstants.NodeSecretKeyName);
            }

            return;
        }

        if (!this.NetUnit.NetBase.SetNodeSeedKey(key))
        {
            this.UserInterfaceService.WriteLineError(Hashed.Vault.NoRestore, NetConstants.NodeSecretKeyName);
            return;
        }
    }

    private async Task TerminateAsync(UnitContext context)
    {
        this.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write("Termination process initiated");

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

        this.LogUnit.RootLogService.GetWriter<DefaultLog>()?.Write(abort ? "Aborted" : "Terminated");
        this.LogUnit.FlushAndTerminate().Wait(); // Write logs added after Terminate().
    }
}
