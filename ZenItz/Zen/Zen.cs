// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen
{
    public const int MaxFlakeSize = 1024 * 1024 * 4; // 4MB
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentCount = 1000;
    public const long DefaultPrimarySizeLimit = 100_000_000; // 100MB

    public delegate void ObjectToMemoryOwnerDelegate(object? obj, out ByteArrayPool.MemoryOwner memoryOwner);

    public delegate object? MemoryOwnerToObjectDelegate(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner);

    public static void DefaultObjectToMemoryOwner(object? obj, out ByteArrayPool.MemoryOwner memoryOwner)
    {
        memoryOwner = ByteArrayPool.MemoryOwner.Empty;
    }

    public static object? DefaultMemoryOwnerToObject(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        return null;
    }

    public Zen()
    {
        this.SnowmanControl = new();
        this.FlakePool = new ByteArrayPool(MaxFlakeSize, (int)(DefaultPrimarySizeLimit / MaxFlakeSize));
        this.FragmentPool = new ByteArrayPool(MaxFragmentSize, 0);
        this.SnowFlakeGoshujin = new(this, this.FlakePool);
        this.SnowFragmentGoshujin = new(this, this.FragmentPool);
    }

    public void SetDelegate(ObjectToMemoryOwnerDelegate objectToMemoryOwner, MemoryOwnerToObjectDelegate memoryOwnerToObject)
    {
        this.ObjectToMemoryOwner = objectToMemoryOwner;
        this.MemoryOwnerToObject = memoryOwnerToObject;
    }

    public Flake CreateOrGet(Identifier id)
    {
        Flake? flake;
        lock (this.flakeGoshujin)
        {
            if (!this.flakeGoshujin.IdChain.TryGetValue(id, out flake))
            {
                flake = new Flake(this, id);
                this.flakeGoshujin.Add(flake);
            }
            else
            {
                flake.CreateInternal(this.flakeGoshujin);
            }
        }

        return flake;
    }

    public Flake? TryGet(Identifier id)
    {
        Flake? flake;
        lock (this.flakeGoshujin)
        {
            this.flakeGoshujin.IdChain.TryGetValue(id, out flake);
            return flake;
        }
    }

    public bool TryRemove(Identifier id)
    {
        lock (this.flakeGoshujin)
        {
            if (this.flakeGoshujin.IdChain.TryGetValue(id, out var flake))
            {
                return flake.RemoveInternal();
            }
        }

        return false;
    }

    public SnowmanControl SnowmanControl { get; }

    public ByteArrayPool FlakePool { get; }

    public ByteArrayPool FragmentPool { get; }

    public ObjectToMemoryOwnerDelegate ObjectToMemoryOwner { get; private set; } = DefaultObjectToMemoryOwner;

    public MemoryOwnerToObjectDelegate MemoryOwnerToObject { get; private set; } = DefaultMemoryOwnerToObject;

    internal SnowObjectGoshujin SnowFlakeGoshujin;
    internal SnowObjectGoshujin SnowFragmentGoshujin;
    private Flake.GoshujinClass flakeGoshujin = new();
}
