// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Stats;

#pragma warning disable SA1401

namespace Lp;

public partial class Linker : MergerBase, IUnitPreparable, IUnitExecutable
{
    private const string NameSuffix = "_L";

    #region FieldAndProperty

    [MemberNotNullWhen(true, nameof(Configuration))]
    [MemberNotNullWhen(true, nameof(dataCrystal))]
    [MemberNotNullWhen(true, nameof(data))]
    public override bool Initialized { get; protected set; }

    public LinkerConfiguration? Configuration { get; protected set; }

    public LinkerState State { get; protected set; } = new();

    public override string GetName() => this.Configuration?.Name ?? string.Empty;

    public override CredentialState GetState() => this.State;

    private ICrystal<FullCredit.GoshujinClass>? dataCrystal;
    private FullCredit.GoshujinClass? data;

    #endregion

    public Linker(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase, NetStats netStats, DomainControl domainControl)
        : base(context, unitLogger, netBase, lpBase, netStats, domainControl)
    {
    }

    public virtual void Initialize(CrystalControl crystalControl, SeedKey seedKey)
    {
        this.Configuration = crystalControl.CreateCrystal<LinkerConfiguration>(new()
        {
            NumberOfFileHistories = 0,
            FileConfiguration = new GlobalFileConfiguration(LinkerConfiguration.Filename),
            RequiredForLoading = true,
        }).Data;

        this.dataCrystal = crystalControl.CreateCrystal<FullCredit.GoshujinClass>(new()
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

    public SeedKey SeedKey => this.seedKey;

    async Task IUnitPreparable.Prepare(UnitMessage.Prepare message)
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

    async Task IUnitExecutable.Stop(UnitMessage.Stop message)
    {
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
    }
}
