// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

public readonly struct Waypoint : IEquatable<Waypoint>, IComparable<Waypoint>
{
    public const string Extension = "waypoint";
    public const int Length = 24; // 8 + 4 + 4 + 8
    public static readonly Waypoint Invalid = default;
    public static readonly Waypoint Empty = new(1, 0, 0, 0);

    public Waypoint(ulong journalPosition, uint currentPlane, uint nextPlane, ulong hash)
    {
        this.JournalPosition = journalPosition;
        this.CurrentPlane = currentPlane;
        this.NextPlane = nextPlane;
        this.Hash = hash;
    }

    public static bool TryParse(string base64Url, out Waypoint waypoint)
    {
        var byteArray = Base64.Url.FromStringToByteArray(base64Url);
        return TryParse(byteArray, out waypoint);
    }

    public static bool TryParse(ReadOnlySpan<byte> span, out Waypoint waypoint)
    {
        if (span.Length >= Length)
        {
            try
            {
                var journalPosition = BitConverter.ToUInt64(span);
                span = span.Slice(sizeof(ulong));
                var currentPlane = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                var nextPlane = BitConverter.ToUInt32(span);
                span = span.Slice(sizeof(uint));
                var hash = BitConverter.ToUInt64(span);

                waypoint = new(journalPosition, currentPlane, nextPlane, hash);
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
    public readonly uint CurrentPlane;
    public readonly uint NextPlane; // Où allons-nous
    public readonly ulong Hash;

    public bool IsValid => this.JournalPosition != 0;

    public byte[] ToByteArray()
    {
        var byteArray = new byte[Length];
        this.WriteSpan(byteArray.AsSpan());

        return byteArray;
    }

    public string ToBase64Url()
    {
        Span<byte> span = stackalloc byte[Length];
        this.WriteSpan(span);

        return Base64.Url.FromByteArrayToString(span);
    }

    public bool Equals(Waypoint other)
        => this.JournalPosition == other.JournalPosition &&
        this.CurrentPlane == other.CurrentPlane &&
        this.NextPlane == other.NextPlane &&
        this.Hash == other.Hash;

    public int CompareTo(Waypoint other)
    {
        if (this.JournalPosition < other.JournalPosition)
        {
            return -1;
        }
        else if (this.JournalPosition > other.JournalPosition)
        {
            return 1;
        }

        if (this.CurrentPlane < other.CurrentPlane)
        {
            return -1;
        }
        else if (this.CurrentPlane > other.CurrentPlane)
        {
            return 1;
        }

        if (this.NextPlane < other.NextPlane)
        {
            return -1;
        }
        else if (this.NextPlane > other.NextPlane)
        {
            return 1;
        }

        if (this.Hash < other.Hash)
        {
            return -1;
        }
        else if (this.Hash > other.Hash)
        {
            return 1;
        }

        return 0;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.JournalPosition, this.CurrentPlane, this.NextPlane, this.Hash);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteSpan(Span<byte> span)
    {
        BitConverter.TryWriteBytes(span, this.JournalPosition);
        span = span.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(span, this.CurrentPlane);
        span = span.Slice(sizeof(uint));
        BitConverter.TryWriteBytes(span, this.NextPlane);
        span = span.Slice(sizeof(uint));
        BitConverter.TryWriteBytes(span, this.Hash);
    }
}
