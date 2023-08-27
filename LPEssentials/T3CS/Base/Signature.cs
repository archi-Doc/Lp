// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;

namespace LP.T3CS;

[TinyhandObject]
public readonly partial struct Signature
{
    public enum Type
    {
        Invalid,
        Attest,
    }

    public Signature(PublicKey publicKey, Type signatureType, long signedMics)
    {
        this.PublicKey = publicKey;
        this.SignatureType = signatureType;
        this.SignedMics = signedMics;
        this.Sign = null;
    }

    public Signature(PublicKey publicKey, Type signatureType, long signedMics, byte[] sign)
    {
        this.PublicKey = publicKey;
        this.SignatureType = signatureType;
        this.SignedMics = signedMics;

        if (sign.Length == PublicKey.PublicKeyLength)
        {
            this.Sign = sign;
        }
        else
        {
            this.Sign = null;
        }
    }

    [Key(0)]
    public readonly PublicKey PublicKey;

    [Key(1)]
    public readonly Type SignatureType;

    [Key(2)]
    public readonly long SignedMics;

    [Key(3)]
    public readonly long ExpirationMics;

    [Key(4, Condition = false)]
    [DefaultValue(null)]
    public readonly byte[]? Sign;
}
