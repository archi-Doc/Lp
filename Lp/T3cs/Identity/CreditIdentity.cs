// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(DualKey = false)]
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

    public Credit ToCredit()
    {
        var credit = new Credit(this.GetIdentifier(), this.Mergers);
        return credit;
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

    public override string ToString()
        => this.ToString(null);

    public string ToString(IConversionOptions? options)
    {
        return $"CreditIdentity: {this.SourceIdentifier.ToString(options)}, Originator: {this.Originator.ToString(options)}, Mergers: [{string.Join(", ", this.Mergers.Select(x => x.ToString(options)))}]";
    }
}
