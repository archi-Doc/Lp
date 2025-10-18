// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true)]
public partial record class CreditIdentity : Identity
{
    #region FieldAndProperty

    [Key(Identity.ReservedKeyCount + 0)]
    [MaxLength(LpConstants.MaxMergers)]
    public partial SignaturePublicKey[] Mergers { get; init; } = [];

    #endregion

    public CreditIdentity(Identifier sourceIdentifier, SignaturePublicKey originator, SignaturePublicKey[] mergers)
        : base(sourceIdentifier, originator)
    {
        this.Mergers = mergers;
    }

    public Credit? ToCredit()
    {
        Credit.TryCreate(this, out var credit);
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
        return $"{{ SourceIdentifier = {this.SourceIdentifier.ToString(options)}, Originator = {this.Originator.ToString(options)}, Mergers={{{string.Join(", ", this.Mergers.Select(x => x.ToString(options)))}}}";
    }
}
