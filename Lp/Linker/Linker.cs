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

    public Linker(UnitContext context, LogUnit logUnit, NetBase netBase, LpBase lpBase, NetStats netStats, DomainControl domainControl)
        : base(context, logUnit, netBase, lpBase, netStats, domainControl)
    {
    }

    public virtual void Initialize(CrystalControl crystalControl, SeedKey seedKey)
    {
        this.Configuration = crystalControl.CreateCrystal<LinkerConfiguration>(new()
        {
            NumberOfHistoryFiles = 0,
            FileConfiguration = new GlobalFileConfiguration(LinkerConfiguration.Filename),
            RequiredForLoading = true,
        }).Data;

        this.dataCrystal = crystalControl.CreateCrystal<FullCredit.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfHistoryFiles = 3,
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

    async Task IUnitPreparable.PrepareAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
        if (!this.Initialized)
        {
            return;
        }

        this.logger.GetWriter()?.Write($"{this.Configuration.Name}: {this.PublicKey.ToString()}");
    }

    async Task IUnitExecutable.StartAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
    }

    async Task IUnitExecutable.StopAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
    }

    async Task IUnitExecutable.TerminateAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
    }
}
