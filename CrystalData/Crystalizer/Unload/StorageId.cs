// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace CrystalData;

[TinyhandObject]
public readonly partial struct StorageId : IEquatable<StorageId>, IComparable<StorageId>
{// StorageId: JournalPosition 8 bytes, Hash 7 bytes, SubId 1 byte.
    public const string Extension = "storage";
    public const int Length = 16; // 8 + 8
    public const ulong HashMask = 0xFFFF_FFFF_FFFF_FF00;
    public const ulong SubIdMask = 0x0000_0000_0000_00FF;
    public static readonly StorageId Invalid = default;
    public static readonly StorageId Empty = new(1, 0);
    public static readonly int LengthInBase32;

    static StorageId()
    {
        LengthInBase32 = Base32Sort.GetEncodedLength(Length);
    }

    public StorageId()
    {
    }

    public StorageId(ulong journalPosition, ulong hashAndSubId)
    {
        this.JournalPosition = journalPosition;
        this.hashAndSubId = hashAndSubId;
    }

    public StorageId(ulong journalPosition, ulong hash, byte subId)
    {
        this.JournalPosition = journalPosition;
        this.hashAndSubId = (hash & HashMask) | subId;
    }

    public static bool TryParse(string base32, out StorageId storageId)
    {
        var byteArray = Base32Sort.Default.FromStringToByteArray(base32);
        return TryParse(byteArray, out storageId);
    }

    public static bool TryParse(ReadOnlySpan<byte> span, out StorageId storageId)
    {
        if (span.Length >= Length)
        {
            try
            {
                var journalPosition = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt64(span));
                span = span.Slice(sizeof(ulong));
                var hash = BitConverter.ToUInt64(span);
                storageId = new(journalPosition, hash);
                return true;
            }
            catch
            {
            }
        }

        storageId = default;
        return false;
    }

    [Key(0)]
    public readonly ulong JournalPosition;

    [Key(1)]
    private readonly ulong hashAndSubId;

    public byte SubId => (byte)(this.hashAndSubId & SubIdMask);

    public bool IsValid => this.JournalPosition != 0;

    public bool HashEquals(ulong hash)
        => (hash & HashMask) == (this.hashAndSubId & HashMask);

    public byte[] ToByteArray()
    {
        var byteArray = new byte[Length];
        this.WriteSpan(byteArray.AsSpan());

        return byteArray;
    }

    public string ToBase32()
    {
        Span<byte> span = stackalloc byte[Length];
        this.WriteSpan(span);

        return Base32Sort.Default.FromByteArrayToString(span);
    }

    public override string ToString()
        => $"Position: {this.JournalPosition}, HashAndSubId: {this.hashAndSubId}";

    public bool Equals(StorageId other)
        => this.JournalPosition == other.JournalPosition &&
        this.hashAndSubId == other.hashAndSubId;

    public static bool operator >(StorageId w1, StorageId w2)
        => w1.CompareTo(w2) > 0;

    public static bool operator <(StorageId w1, StorageId w2)
        => w1.CompareTo(w2) < 0;

    public int CompareTo(StorageId other)
    {
        if (this.JournalPosition < other.JournalPosition)
        {
            return -1;
        }
        else if (this.JournalPosition > other.JournalPosition)
        {
            return 1;
        }

        if (this.hashAndSubId < other.hashAndSubId)
        {
            return -1;
        }
        else if (this.hashAndSubId > other.hashAndSubId)
        {
            return 1;
        }

        return 0;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.JournalPosition, this.hashAndSubId);

    private static void WriteBigEndian(ulong value, Span<byte> span)
    {
        unchecked
        {
            // Write to highest index first so the JIT skips bounds checks on subsequent writes.
            span[7] = (byte)value;
            span[6] = (byte)(value >> 8);
            span[5] = (byte)(value >> 16);
            span[4] = (byte)(value >> 24);
            span[3] = (byte)(value >> 32);
            span[2] = (byte)(value >> 40);
            span[1] = (byte)(value >> 48);
            span[0] = (byte)(value >> 56);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteSpan(Span<byte> span)
    {
        WriteBigEndian(this.JournalPosition, span);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.hashAndSubId);
    }
}
