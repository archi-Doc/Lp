// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public class Zen
{
    public const int MaxFlakeSize = 1024 * 1024 * 4; // 4MB
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentCount = 1000;
    public const long DefaultPrimarySizeLimit = 100_000_000; // 100MB
    public const string DefaultZenFile = "Zen.main";
    public const string DefaultZenBackup = "Zen.back";
    public const string DefaultSnowmanFile = "Snow.main";
    public const string DefaultSnowmanBackup = "Snow.back";

    public delegate void ObjectToMemoryOwnerDelegate(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved);

    public delegate object? MemoryOwnerToObjectDelegate(ByteArrayPool.MemoryOwner memoryOwner);

    public static void DefaultObjectToMemoryOwner(object? obj, out ByteArrayPool.MemoryOwner dataToBeMoved)
    {
        dataToBeMoved = ByteArrayPool.MemoryOwner.Empty;
    }

    public static object? DefaultMemoryOwnerToObject(ByteArrayPool.MemoryOwner memoryOwner)
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

    public async Task<ZenStartResult> TryStartZen(ZenStart param)
    {
        var result = ZenStartResult.Success;
        if (this.ZenStarted)
        {
            return ZenStartResult.AlreadyStarted;
        }

        byte[]? zenData;
        try
        {
            zenData = await File.ReadAllBytesAsync(param.ZenFile);
        }
        catch
        {
            result = ZenStartResult.ZenFileNotFound;
            goto Exit;
        }

        if (zenData.Length < 8)
        {
            result = ZenStartResult.ZenFileError;
            goto Exit;
        }

        var memory = zenData.AsMemory(8);
        if (Arc.Crypto.FarmHash.Hash64(memory.Span) != BitConverter.ToUInt64(zenData))
        {
            result = ZenStartResult.ZenFileError;
            goto Exit;
        }

        result = this.SnowmanControl.TryStart(param, memory);

Exit:
        if (param.ForceStart)
        {// Force start
        }

        return result;
    }

    public async Task StopZen(ZenStop param)
    {
        if (!this.ZenStarted)
        {
            return;
        }

        this.ZenStarted = false;
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

    internal volatile bool ZenStarted;
    internal SnowObjectGoshujin SnowFlakeGoshujin;
    internal SnowObjectGoshujin SnowFragmentGoshujin;
    private Flake.GoshujinClass flakeGoshujin = new();
}
