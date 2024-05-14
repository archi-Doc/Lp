// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

public class RelayMerger : Merger
{
    public RelayMerger(SignaturePrivateKey mergerPrivateKey, UnitContext context, UnitLogger unitLogger, LPBase lpBase, ICrystal<CreditData.GoshujinClass> crystal, MergerInformation mergerInformation, ICrystal<RelayStatus.GoshujinClass> relayStatusCrystal)
        : base(mergerPrivateKey, context, unitLogger, lpBase, crystal, mergerInformation)
    {
        this.logger = unitLogger.GetLogger<RelayMerger>();
        this.relayStatusCrystal = relayStatusCrystal;
        this.relayStatusData = this.relayStatusCrystal.Data;
    }

    private ICrystal<RelayStatus.GoshujinClass> relayStatusCrystal;
    private RelayStatus.GoshujinClass relayStatusData;

    public async NetTask<RelayStatus?> GetRelayStatus(Credit relayCredit)
    {
        return this.relayStatusData.TryGet(relayCredit);
    }
}
