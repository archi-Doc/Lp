// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs.Domain;

public static class DomainMachineHelper
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static bool TryParseDomainMachineKind(ReadOnlySpan<char> source, out DomainMachineKind kind)
    {
        if (source.Equals("CreditMerger", IgnoreCase) || source.Equals("cm", IgnoreCase))
        {
            kind = DomainMachineKind.CreditMerger;
            return true;
        }
        else if (source.Equals("CreditPeer", IgnoreCase) || source.Equals("cp", IgnoreCase))
        {
            kind = DomainMachineKind.CreditPeer;
            return true;
        }
        else if (source.Equals("RelayMerger", IgnoreCase) || source.Equals("rm", IgnoreCase))
        {
            kind = DomainMachineKind.RelayMerger;
            return true;
        }
        else if (source.Equals("RelayPeer", IgnoreCase) || source.Equals("rp", IgnoreCase))
        {
            kind = DomainMachineKind.RelayPeer;
            return true;
        }
        else if (source.Equals("DataMerger", IgnoreCase) || source.Equals("dm", IgnoreCase))
        {
            kind = DomainMachineKind.DataMerger;
            return true;
        }
        else if (source.Equals("DataPeer", IgnoreCase) || source.Equals("dp", IgnoreCase))
        {
            kind = DomainMachineKind.DataPeer;
            return true;
        }

        kind = DomainMachineKind.Invalid;
        return false;
    }
}
