// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class LinkProof : ProofWithValue
{
    [Key(ProofWithValue.ReservedKeyCount)]
    public SignaturePublicKey LinkerPublicKey { get; private set; }

    public LinkProof(SignaturePublicKey linkerPublicKey, Value value)
    {
        this.LinkerPublicKey = linkerPublicKey;
        this.Value = value;
    }

    public override bool TryGetLinkerPublicKey([MaybeNullWhen(false)] out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}
