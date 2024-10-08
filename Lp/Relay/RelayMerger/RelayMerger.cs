// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public class RelayMerger : Merger
{
    public RelayMerger(UnitContext context, UnitLogger unitLogger, LpBase lpBase)
        : base(context, unitLogger, lpBase)
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

    public override void Initialize(Crystalizer crystalizer, SignaturePrivateKey mergerPrivateKey)
    {
        this.Information = crystalizer.CreateCrystal<MergerConfiguration>(new()
        {
            NumberOfFileHistories = 3,
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

        this.creditData = this.creditDataCrystal.Data;

        this.relayStatusCrystal = crystalizer.CreateCrystal<RelayStatus.GoshujinClass>(new()
        {
            SaveFormat = SaveFormat.Binary,
            NumberOfFileHistories = 0,
            FileConfiguration = new GlobalFileConfiguration("Relay/Status"),
        });

        this.relayStatusData = this.relayStatusCrystal.Data;

        this.mergerPrivateKey = mergerPrivateKey;
        this.MergerPublicKey = this.mergerPrivateKey.ToPublicKey();

        this.Initialized = true;
    }

    public async NetTask<RelayStatus?> GetRelayStatus(Credit relayCredit)
    {
        if (!this.Initialized)
        {
            return default;
        }

        await this.creditDataCrystal.Save();
        return this.relayStatusData.TryGet(relayCredit);
    }
}
