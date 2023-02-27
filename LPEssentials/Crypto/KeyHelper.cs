﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace LP;

internal static class KeyHelper
{// KeyValue 1bit: Private, 1bit: ?, 4bits: KeyVersion, 1bit: ?, 1bit: YTilde
    public static ReadOnlySpan<char> PrivateKeyBrace => "!!!";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte CheckPublicKeyValue(byte keyValue)
        => (byte)(keyValue & ~(1 << 7));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToPublicKeyValue(uint keyVersion, uint yTilde)
        => (byte)(((keyVersion << 2) & 15) | (yTilde & 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToPrivateKeyValue(uint keyVersion, uint yTilde)
        => (byte)((1 << 7) | ((keyVersion << 2) & 15) | (yTilde & 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToKeyVersion(byte keyValue)
        => (uint)((keyValue >> 2) & 15);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToYTilde(byte keyValue)
        => (uint)(keyValue & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrivate(byte keyValue)
        => (keyValue >> 7) != 0;
}
