// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Journal;

public readonly struct Waypoint : IEquatable<Waypoint>
{
    public const string Extension = "waypoint";
    public const int Length = 20; // 8 + 4 + 8

    public Waypoint(ulong journalPosition, uint journalToken, ulong hash)
    {
        this.JournalPosition = journalPosition;
        this.JournalToken = journalToken;
        this.Hash = hash;
    }

    public readonly ulong JournalPosition;
    public readonly uint JournalToken;
    public readonly ulong Hash;

    public byte[] ToByteArray()
    {
        var byteArray = new byte[Length];
        var span = byteArray.AsSpan();
        BitConverter.TryWriteBytes(span, this.JournalPosition);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.JournalToken);
        span = span.Slice(sizeof(uint));
        BitConverter.TryWriteBytes(span, this.Hash);

        return span.ToArray();
    }

    public bool Equals(Waypoint other)
        => this.JournalPosition == other.JournalPosition &&
        this.JournalToken == other.JournalToken &&
        this.Hash == other.Hash;

    public override int GetHashCode()
        => HashCode.Combine(this.JournalPosition, this.JournalToken, this.Hash);
}
