// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs.Domain;

public static class DomainMachineHelper
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static bool TryParseDomainMachineKind(ReadOnlySpan<char> source, out DomainMachineKind kind)
    {
        if (source.Equals("CreditMerger", IgnoreCase))
        {
            kind = DomainMachineKind.CreditMerger;
            return true;
        }
        else if (source.Equals("CreditPeer", IgnoreCase))
        {
            kind = DomainMachineKind.CreditPeer;
            return true;
        }
        else if (source.Equals("RelayMerger", IgnoreCase))
        {
            kind = DomainMachineKind.RelayMerger;
            return true;
        }
        else if (source.Equals("RelayPeer", IgnoreCase))
        {
            kind = DomainMachineKind.RelayPeer;
            return true;
        }
        else if (source.Equals("DataMerger", IgnoreCase))
        {
            kind = DomainMachineKind.DataMerger;
            return true;
        }
        else if (source.Equals("DataPeer", IgnoreCase))
        {
            kind = DomainMachineKind.DataPeer;
            return true;
        }

        kind = DomainMachineKind.Invalid;
        return false;
    }
}
