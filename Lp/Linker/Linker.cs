// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.T3cs;
using Netsphere.Crypto;

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

    public SignaturePublicKey LinkerPublicKey { get; protected set; }

    public LinkerConfiguration? Configuration { get; protected set; }

    private readonly ILogger logger;
    private readonly NetBase netBase;
    private readonly LpBase lpBase;
    private ICrystal<FullCredit.GoshujinClass>? dataCrystal;
    private FullCredit.GoshujinClass? data;
    private SeedKey? linkerSeedKey;

    #endregion

    public Linker(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase)
        : base(context)
    {
        this.logger = unitLogger.GetLogger<Linker>();
        this.netBase = netBase;
        this.lpBase = lpBase;
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

        if (string.IsNullOrEmpty(this.Configuration.LinkerName))
        {
            this.Configuration.LinkerName = $"{this.netBase.NetOptions.NodeName}{NameSuffix}";
        }

        this.data = this.dataCrystal.Data;
        this.linkerSeedKey = seedKey;
        this.LinkerPublicKey = this.linkerSeedKey.GetSignaturePublicKey();

        this.Initialized = true;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        if (!this.Initialized)
        {
            return;
        }

        this.logger.TryGet()?.Log($"{this.Configuration.LinkerName}: {this.LinkerPublicKey.ToString()}");
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
