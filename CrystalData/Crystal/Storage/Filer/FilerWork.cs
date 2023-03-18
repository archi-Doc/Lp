// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal class FilerWork : IEquatable<FilerWork>
{
    public enum WorkType
    {
        Put,
        Get,
        Delete,
    }

    public WorkType Type { get; }

    public string Path { get; }

    public ByteArrayPool.ReadOnlyMemoryOwner DataToBePut { get; }

    public int SizeToGet { get; }

    public ByteArrayPool.MemoryOwner GotData { get; internal set; }

    public FilerWork(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {// Put
        this.Type = WorkType.Put;
        this.Path = path;
        this.DataToBePut = dataToBeShared.IncrementAndShare();
    }

    public FilerWork(string path, int sizeToGet)
    {// Get
        this.Type = WorkType.Get;
        this.Path = path;
        this.SizeToGet = sizeToGet;
    }

    public FilerWork(string path)
    {// Delete
        this.Type = WorkType.Delete;
        this.Path = path;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Type, this.Path, this.DataToBePut.Memory.Length, this.SizeToGet);

    public bool Equals(FilerWork? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Type == other.Type &&
            this.Path == other.Path &&
            this.DataToBePut.Memory.Span.SequenceEqual(other.DataToBePut.Memory.Span) &&
            this.SizeToGet == other.SizeToGet;
    }
}
