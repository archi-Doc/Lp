// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Stats;

#pragma warning disable SA1401

namespace Lp;

public partial class Linker : UnitBase, IUnitPreparable, IUnitExecutable
{
    private const string NameSuffix = "_L";

    #region FieldAndProperty

    [MemberNotNullWhen(true, nameof(Configuration))]
    [MemberNotNullWhen(true, nameof(dataCrystal))]
    [MemberNotNullWhen(true, nameof(data))]
    public virtual bool Initialized { get; protected set; }

    public SignaturePublicKey PublicKey { get; protected set; }

    public LinkerConfiguration? Configuration { get; protected set; }

    public LinkerState State { get; protected set; } = new();

    private readonly ILogger logger;
    private readonly ModestLogger modestLogger;
    private readonly NetBase netBase;
    private readonly LpBase lpBase;
    private readonly NetStats netStats;
    private ICrystal<FullCredit.GoshujinClass>? dataCrystal;
    private FullCredit.GoshujinClass? data;
    private SeedKey? seedKey;

    #endregion

    public Linker(UnitContext context, UnitLogger unitLogger, NetBase netBase, NetStats netStats, LpBase lpBase)
        : base(context)
    {
        this.logger = unitLogger.GetLogger<Linker>();
        this.modestLogger = new(this.logger);
        this.netBase = netBase;
        this.lpBase = lpBase;
        this.netStats = netStats;
    }

    public virtual void Initialize(Crystalizer crystalizer, SeedKey seedKey)
    {
        this.Configuration = crystalizer.CreateCrystal<LinkerConfiguration>(new()
        {
            NumberOfFileHistories = 0,
            FileConfiguration = new GlobalFileConfiguration(LinkerConfiguration.Filename),
            RequiredForLoading = true,
        }).Data;

        this.dataCrystal = crystalizer.CreateCrystal<FullCredit.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 3,
            FileConfiguration = new GlobalFileConfiguration("Linker/Data"),
            StorageConfiguration = new SimpleStorageConfiguration(
                new GlobalDirectoryConfiguration("Linker/Storage")),
        });

        if (string.IsNullOrEmpty(this.Configuration.Name))
        {
            this.Configuration.Name = $"{this.netBase.NetOptions.NodeName}{NameSuffix}";
        }

        this.data = this.dataCrystal.Data;
        this.seedKey = seedKey;
        this.PublicKey = this.seedKey.GetSignaturePublicKey();

        this.Initialized = true;
    }

    public bool TrySign(Proof proof, long validMics)
        => this.seedKey is null ? false : this.seedKey.TrySign(proof, validMics);

    public void UpdateState()
    {
        if (!this.Initialized)
        {
            return;
        }

        // Check net node
        this.State.NetNode = this.netStats.OwnNetNode;
        this.State.Name = this.Configuration.Name;
        if (this.State.NetNode is null)
        {
            this.modestLogger.NonConsecutive(Hashed.Error.NoFixedNode, LogLevel.Error)?.Log(Hashed.Error.NoFixedNode);
            return;
        }

        // Check node type
        if (this.netStats.OwnNodeType != NodeType.Direct)
        {
            this.modestLogger.NonConsecutive(Hashed.Error.NoDirectConnection, LogLevel.Error)?.Log(Hashed.Error.NoDirectConnection);
            return;
        }

        // Active
        if (!this.State.IsActive)
        {
            this.State.IsActive = true;
            this.logger.TryGet(LogLevel.Information)?.Log("Activated");
        }
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        if (!this.Initialized)
        {
            return;
        }

        this.logger.TryGet()?.Log($"{this.Configuration.Name}: {this.PublicKey.ToString()}");
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
    }

    void IUnitExecutable.Stop(UnitMessage.Stop message)
    {
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
    }
}
