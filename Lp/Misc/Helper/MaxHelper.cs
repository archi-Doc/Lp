// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Misc;

public static class MaxHelper
{
    public const byte UInt8 = 0xBD;
    public const sbyte Int8 = unchecked((sbyte)0xBD);
    public const ushort UInt16 = 0xBDBD;
    public const short Int16 = unchecked((short)0xBDBD);
    public const uint UInt32 = 0xBDBDBDBD;
    public const int Int32 = unchecked((int)0xBDBDBDBD);
    public const ulong UInt64 = 0xBDBDBDBDBDBDBDBD;
    public const long Int64 = unchecked((long)0xBDBDBDBDBDBDBDBD);

    public static readonly SignaturePublicKey SignaturePublicKey;
    public static readonly byte[] Signature;
    public static readonly Identifier Identifier;
    public static readonly SignaturePublicKey[] Merger;
    public static readonly Credit Credit;

    static MaxHelper()
    {
        SignaturePublicKey = new SignaturePublicKey(UInt64, UInt64, UInt64, UInt64);
        Signature = new byte[CryptoSign.SignatureSize];
        Signature.AsSpan().Fill(UInt8);
        Identifier = new Identifier(UInt64, UInt64, UInt64, UInt64);
        Merger = [SignaturePublicKey, SignaturePublicKey, SignaturePublicKey,];
        Credit = new Credit(Identifier, Merger);
    }
}
