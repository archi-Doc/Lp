// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netsphere.Relay;

public static class RelayHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelayId SpanToRelayId(ReadOnlySpan<byte> span)
        => MemoryMarshal.Read<RelayId>(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateNonce(uint salt4, ulong salt8, ulong secret, Span<byte> nonce)
    {
        Debug.Assert(nonce.Length == 32);

        MemoryMarshal.Write(nonce, salt4);
        nonce = nonce.Slice(sizeof(uint));
        MemoryMarshal.Write(nonce, salt8);
        nonce = nonce.Slice(sizeof(ulong));
        MemoryMarshal.Write(nonce, secret);
        nonce = nonce.Slice(sizeof(ulong));
        MemoryMarshal.Write(nonce, salt4);
        nonce = nonce.Slice(sizeof(uint));
        MemoryMarshal.Write(nonce, salt8);
    }
}
