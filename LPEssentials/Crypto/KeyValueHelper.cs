// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LP;

internal static class KeyHelper
{// // 1bit: Private, 1bit: ?, 4bits: KeyVersion, 1bit: ?, 1bit: YTilde
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
