// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace LP;

internal static class KeyHelper
{// KeyValue 1bit: Private, 1bit: true:Encryption false:Verification, 4bits: KeyVersion, 1bit: ?, 1bit: YTilde
    public static ReadOnlySpan<char> PrivateKeyBrace => "!!!";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToPublicKeyValue(byte keyValue)
        => (byte)(keyValue & ~(1 << 7));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToPrivateKeyValue(uint encryption, uint keyVersion, uint yTilde)
        => (byte)(128 | (encryption & 1) << 6 | ((keyVersion << 2) & 15) | (yTilde & 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetKeyVersion(byte keyValue)
        => (uint)((keyValue >> 2) & 15);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetYTilde(byte keyValue)
        => (uint)(keyValue & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrivate(byte keyValue)
        => (keyValue & 128) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPublic(byte keyValue)
        => (keyValue & 128) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEncryption(byte keyValue)
        => (keyValue & 64) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVerification(byte keyValue)
        => (keyValue & 64) == 0;
}
