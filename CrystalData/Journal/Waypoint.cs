// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Journal;

public readonly struct Waypoint : IEquatable<Waypoint>
{
    public const string Extension = "waypoint";
    public const int Length = 20; // 8 + 4 + 8
    public static readonly Waypoint Invalid = default;
    public static readonly Waypoint Empty = new(1, 0, 0);

    public Waypoint(ulong journalPosition, uint journalToken, ulong hash)
    {
        this.JournalPosition = journalPosition;
        this.JournalToken = journalToken;
        this.Hash = hash;
    }

    public static bool TryParse(ReadOnlySpan<byte> span, out Waypoint waypoint)
    {
        if (span.Length >= Length)
        {
            try
            {
                var journalPosition = BitConverter.ToUInt64(span);
                span = span.Slice(sizeof(ulong));
                var journalToken = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                var hash = BitConverter.ToUInt64(span);

                waypoint = new(journalPosition, journalToken, hash);
                return true;
            }
            catch
            {
            }
        }

        waypoint = default;
        return false;
    }

    public readonly ulong JournalPosition;
    public readonly uint JournalToken;
    // public readonly uint PreviousToken;
    // public readonly uint CurrentToken;
    // public readonly uint NextToken; // Où allons-nous
    public readonly ulong Hash;

    public bool IsValid => this.JournalPosition != 0;

    public byte[] ToByteArray()
    {
        var byteArray = new byte[Length];
        var span = byteArray.AsSpan();
        BitConverter.TryWriteBytes(span, this.JournalPosition);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.JournalToken);
        span = span.Slice(sizeof(uint));
        BitConverter.TryWriteBytes(span, this.Hash);

        return byteArray;
    }

    public bool Equals(Waypoint other)
        => this.JournalPosition == other.JournalPosition &&
        this.JournalToken == other.JournalToken &&
        this.Hash == other.Hash;

    public override int GetHashCode()
        => HashCode.Combine(this.JournalPosition, this.JournalToken, this.Hash);
}
