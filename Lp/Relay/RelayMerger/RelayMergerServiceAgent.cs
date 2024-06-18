// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

[NetServiceObject]
internal class RelayMergerServiceAgent : MergerServiceAgent, IRelayMergerService
{
    public RelayMergerServiceAgent(RelayMerger merger)
        : base(merger)
    {
        this.relayMerger = merger;
    }

    private readonly RelayMerger relayMerger;

    NetTask<RelayStatus?> IRelayMergerService.GetRelayStatus(LP.T3CS.Credit relayCredit)
        => this.relayMerger.GetRelayStatus(relayCredit);
}
