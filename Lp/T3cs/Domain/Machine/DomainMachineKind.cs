// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs.Domain;

public enum DomainMachineKind : byte
{
    Invalid,
    CreditMerger,
    CreditPeer,
    RelayMerger,
    RelayPeer,
    DataMerger,
    DataPeer,
}
