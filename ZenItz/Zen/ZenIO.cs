// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public class ZenIO
{
    public ZenIO()
    {
    }

    internal void Save(ref ulong io, ref long io2, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner, bool exclusiveSnowflake)
    {
        ZenDirectory? directory;
        if (!ZenIdentifier.IsValidIO(io) || !this.directoryGoshujin.DirectoryIdChain.TryGetValue(ZenIdentifier.IOToDirectoryId(io), out directory))
        {// Get valid directory.
            directory = new(); // tempcode
        }

        directory.Save(ref io, ref io2, memoryOwner, exclusiveSnowflake);
    }

    internal async Task<(ZenDataResult DataResult, ulong IO, long IO2)> Load(ulong io, long io2)
    {
        if (!ZenIdentifier.IsValidIO(io) || !this.directoryGoshujin.DirectoryIdChain.TryGetValue(ZenIdentifier.IOToDirectoryId(io), out var directory))
        {// Invalid io.
            return new(new(ZenResult.NoData), 0, 0);
        }

        return await directory.Load(ref io, ref io2);
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
