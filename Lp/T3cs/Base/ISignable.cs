// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public readonly struct SignaturePair
{
    public readonly int SignatureIndex;

    public readonly byte[]? Signature;

    public SignaturePair(int signatureIndex, byte[]? signature)
    {
        this.SignatureIndex = signatureIndex;
        this.Signature = signature;
    }

    [MemberNotNullWhen(true, nameof(Signature))]
    public bool IsValid => this.Signature is not null;
}

public interface ISignable
{
    bool PrepareForSigning(ref SignaturePublicKey publicKey, long validMics);

    bool SetSignature(int signatureIndex, byte[] signature);
}
