// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public class ZenIO
{
    public ZenIO()
    {
    }

    internal void Save(ref ulong io, ref long io2, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        if (!ZenIdentifier.IsValidIO(io))
        {// Get valid directory id.

        }

        var zenIdentifier = new ZenIdentifier(io, io2);
        if (!zenIdentifier.IsValid)
        {

        }
    }

    internal async Task<ZenDataResult> Load(ZenIdentifier zenIdentifier)
    {
        if (!this.directoryGoshujin.DirectoryIdChain.TryGetValue(zenIdentifier.DirectoryId, out var directory))
        {
            return new(ZenResult.NoData);
        }

        return new ZenDataResult(ZenResult.Success, ByteArrayPool.ReadOnlyMemoryOwner.Empty);
    }

    internal async Task<ZenStartResult> TryStart(ZenStartParam param, ReadOnlyMemory<byte>? data)
    {
        ZenDirectory.GoshujinClass? goshujin = null;

        if (data != null)
        {
            try
            {
                goshujin = TinyhandSerializer.Deserialize<ZenDirectory.GoshujinClass>(data.Value);
            }
            catch
            {
                if (!await param.Query(ZenStartResult.ZenFileError))
                {
                    return ZenStartResult.ZenFileError;
                }
            }
        }

        goshujin ??= new();
        List<string>? errorDirectories = null;
        foreach (var x in goshujin.DirectoryIdChain)
        {
            if (!x.Check())
            {
                errorDirectories ??= new();
                errorDirectories.Add(x.DirectoryPath);
            }
        }

        if (errorDirectories != null &&
            !await param.Query(ZenStartResult.ZenDirectoryError, errorDirectories.ToArray()))
        {
            return ZenStartResult.ZenFileError;
        }

        // if (param.DefaultFolder != null && !goshujin.DirectoryIdChain.TryGetValue(ZenDirectory.DefaultDirectoryId, out _))
        if (param.DefaultFolder != null && goshujin.DirectoryIdChain.Count == 0)
        {
            try
            {
                Directory.CreateDirectory(param.DefaultFolder);
                var defaultDirectory = new ZenDirectory(this.GetFreeDirectoryId(goshujin), param.DefaultFolder);
                goshujin.Add(defaultDirectory);
            }
            catch
            {
            }
        }

        foreach (var x in goshujin.DirectoryIdChain)
        {
            x.Start();
        }

        if (goshujin.DirectoryIdChain.Count == 0)
        {
            return ZenStartResult.NoDirectoryAvailable;
        }

        this.directoryGoshujin = goshujin;
        return ZenStartResult.Success;
    }

    internal byte[] Serialize()
    {
        return TinyhandSerializer.Serialize(this.directoryGoshujin);
    }

    private uint GetFreeDirectoryId(ZenDirectory.GoshujinClass goshujin)
    {
        while(true)
        {
            var id = LP.Random.Pseudo.NextUInt32();
            if (id != 0 && !goshujin.DirectoryIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private ZenDirectory.GoshujinClass directoryGoshujin = new();
}
