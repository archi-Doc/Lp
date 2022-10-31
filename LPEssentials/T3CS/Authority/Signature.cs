// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;

namespace LP.T3CS;

[TinyhandObject]
public readonly partial struct Signature
{
    public enum Type
    {
        Invalid,
        Affirmative,
    }

    public Signature(Type signatureType, long signedMics)
    {
        this.SignatureType = signatureType;
        this.SignedMics = signedMics;
    }

    public Signature(Type signatureType, long signedMics, byte[] sign)
    {
        this.SignatureType = signatureType;
        this.SignedMics = signedMics;

        if (sign.Length == PublicKey.PublicKeyLength)
        {
            this.sign = sign;
        }
    }

    [Key(0)]
    public readonly Type SignatureType;

    [Key(1)]
    public readonly long SignedMics;

    [Key(2)]
    [DefaultValue(null)]
    private readonly byte[]? sign;
}
