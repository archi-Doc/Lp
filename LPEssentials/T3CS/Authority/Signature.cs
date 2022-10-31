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

        var span = sign.AsSpan();
        if (span.Length >= PublicKey.PublicKeyLength)
        {
            this.s0 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s1 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s2 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s3 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s4 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s5 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s6 = BitConverter.ToUInt64(span);
            span = span.Slice(sizeof(ulong));
            this.s7 = BitConverter.ToUInt64(span);
        }
    }

    [Key(0)]
    public readonly Type SignatureType;

    [Key(1)]
    public readonly long SignedMics;

    // [Key(2)]
    // [DefaultValue(null)]
    // private readonly byte[]? sign;

    [Key(2)]
    [DefaultValue(0)]
    private readonly ulong s0;

    [Key(3)]
    [DefaultValue(0)]
    private readonly ulong s1;

    [Key(4)]
    [DefaultValue(0)]
    private readonly ulong s2;

    [Key(5)]
    [DefaultValue(0)]
    private readonly ulong s3;

    [Key(6)]
    [DefaultValue(0)]
    private readonly ulong s4;

    [Key(7)]
    [DefaultValue(0)]
    private readonly ulong s5;

    [Key(8)]
    [DefaultValue(0)]
    private readonly ulong s6;

    [Key(9)]
    [DefaultValue(0)]
    private readonly ulong s7;
}
