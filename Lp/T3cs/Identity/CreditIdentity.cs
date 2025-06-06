// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class CreditIdentity : Identity
{
    [Key(Identity.ReservedKeyCount + 0)]
    [MaxLength(LpConstants.MaxMergers)]
    public partial SignaturePublicKey[] Mergers { get; init; } = [];

    public CreditIdentity(Identifier sourceIdentifier, SignaturePublicKey originator, SignaturePublicKey[] mergers)
        : base(sourceIdentifier, originator)
    {
        this.Mergers = mergers;
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        if (this.Mergers.Length == 0 ||
            this.Mergers.Length > LpConstants.MaxMergers)
        {
            return false;
        }

        foreach (var x in this.Mergers)
        {
            if (!x.Validate())
            {
                return false;
            }
        }

        return true;
    }
}
