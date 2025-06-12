// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public sealed class EmptyProof : ContractableProofWithSigner
{
    public static readonly EmptyProof Instance = new();

    private EmptyProof()
        : base(default, default!)
    {
    }

    public override PermittedSigner PermittedSigner => default;

    public override bool Validate(ValidationOptions validationOptions) => true;

    public override SignaturePublicKey GetSignatureKey() => default;

    public override bool TryGetLinkerPublicKey(out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = default;
        return false;
    }

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = default;
        return false;
    }

    /*public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = default;
        return false;
    }*/
}
