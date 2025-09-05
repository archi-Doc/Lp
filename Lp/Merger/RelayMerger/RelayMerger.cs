// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Netsphere.Stats;

namespace Lp.T3cs;

public class RelayMerger : Merger
{
    private const string NameSuffix = "_RM";

    public RelayMerger(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase, NetStats netStats, DomainControl domainControl)
        : base(context, unitLogger, netBase, lpBase, netStats, domainControl)
    {
        this.logger = unitLogger.GetLogger<RelayMerger>();
    }

    #region FieldAndProperty

    private ICrystal<RelayStatus.GoshujinClass>? relayStatusCrystal;
    private RelayStatus.GoshujinClass? relayStatusData;

    [MemberNotNullWhen(true, nameof(relayStatusCrystal))]
    [MemberNotNullWhen(true, nameof(relayStatusData))]
    public override bool Initialized { get; protected set; }

    #endregion

    public override void Initialize(Crystalizer crystalizer, SeedKey mergerSeedKey)
    {
        this.Configuration = crystalizer.CreateCrystal<MergerConfiguration>(new()
        {
            NumberOfFileHistories = 0,
            FileConfiguration = new GlobalFileConfiguration(MergerConfiguration.RelayMergerFilename),
            RequiredForLoading = true,
        }).Data;

        this.creditDataCrystal = crystalizer.CreateCrystal<FullCredit.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 3,
            FileConfiguration = new GlobalFileConfiguration("RelayMerger/Credits"),
            StorageConfiguration = new SimpleStorageConfiguration(
                new GlobalDirectoryConfiguration("RelayMerger/Storage")),
        });

        if (string.IsNullOrEmpty(this.Configuration.Name))
        {
            this.Configuration.Name = $"{this.netBase.NetOptions.NodeName}{NameSuffix}";
        }

        this.creditData = this.creditDataCrystal.Data;

        this.relayStatusCrystal = crystalizer.CreateCrystal<RelayStatus.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 0,
            FileConfiguration = new GlobalFileConfiguration("Relay/Status"),
        });

        this.relayStatusData = this.relayStatusCrystal.Data;

        this.seedKey = mergerSeedKey;
        this.PublicKey = this.seedKey.GetSignaturePublicKey();

        this.Initialized = true;
    }

    public async NetTask<RelayStatus?> GetRelayStatus(Credit relayCredit)
    {
        if (!this.Initialized)
        {
            return default;
        }

        await this.creditDataCrystal.Store();
        return this.relayStatusData.TryGet(relayCredit);
    }
}
