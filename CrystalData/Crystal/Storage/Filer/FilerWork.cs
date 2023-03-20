// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Filer;

internal class FilerWork : IEquatable<FilerWork>
{
    public enum WorkType
    {
        Write,
        Read,
        Delete,
    }

    public WorkType Type { get; }

    public StorageResult Result { get; internal set; }

    public string Path { get; }

    public ByteArrayPool.ReadOnlyMemoryOwner WriteData { get; }

    public int SizeToRead { get; }

    public ByteArrayPool.MemoryOwner ReadData { get; internal set; }

    public FilerWork(string path, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {// Write
        this.Type = WorkType.Write;
        this.Path = path;
        this.WriteData = dataToBeShared.IncrementAndShare();
    }

    public FilerWork(string path, int sizeToRead)
    {// Read
        this.Type = WorkType.Read;
        this.Path = path;
        this.SizeToRead = sizeToRead;
    }

    public FilerWork(string path)
    {// Delete
        this.Type = WorkType.Delete;
        this.Path = path;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Type, this.Path, this.WriteData.Memory.Length, this.SizeToRead);

    public bool Equals(FilerWork? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Type == other.Type &&
            this.Path == other.Path &&
            this.WriteData.Memory.Span.SequenceEqual(other.WriteData.Memory.Span) &&
            this.SizeToRead == other.SizeToRead;
    }
}
