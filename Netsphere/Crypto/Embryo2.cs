// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Arc;

[StructLayout(LayoutKind.Explicit)]
public readonly struct Embryo2
{
    public const int Length = 64;

    #region FieldAndProperty

    [FieldOffset(0)]
    private readonly ulong x0; // Salt
    [FieldOffset(8)]
    private readonly ulong x1; // ConnectionId
    [FieldOffset(16)]
    private readonly ulong x2; // Nonce
    [FieldOffset(24)]
    private readonly ulong x3; // Reserved
    [FieldOffset(32)]
    private readonly ulong x4; // Shared key
    [FieldOffset(40)]
    private readonly ulong x5; // Shared key
    [FieldOffset(48)]
    private readonly ulong x6; // Shared key
    [FieldOffset(56)]
    private readonly ulong x7; // Shared key

    #endregion

    public ulong Salt => this.x0;

    public ulong ConnectionId => this.x1;

    public ulong Nonce => this.x2;

    [UnscopedRef]
    public ReadOnlySpan<byte> Key => this.Span.Slice(32, Aegis256.KeySize);

    [UnscopedRef]
    public Span<byte> Span
        => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));
}
