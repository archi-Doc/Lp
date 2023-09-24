// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace LP.T3CS;

internal static class KeyHelper
{// KeyValue 1bit: Private, 1bit: ?, 4bits: Key class, 1bit: ?, 1bit: YTilde
    internal const KeyClass UpperKeyClass = KeyClass.Node_Encryption;

    public static ReadOnlySpan<char> PrivateKeyBrace => "!!!";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ToPublicKeyValue(byte keyValue)
        => (byte)(keyValue & ~(1 << 7));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte CreatePrivateKeyValue(KeyClass keyClass, uint yTilde)
        => (byte)(128 | (((uint)keyClass << 2) & 15) | (yTilde & 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyClass GetKeyClass(byte keyValue)
        => (KeyClass)((keyValue >> 2) & 15);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetYTilde(byte keyValue)
        => (uint)(keyValue & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrivate(byte keyValue)
        => (keyValue & 128) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPublic(byte keyValue)
        => (keyValue & 128) == 0;
}
