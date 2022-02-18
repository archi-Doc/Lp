// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public class ZenIO
{
    public ZenIO()
    {
    }

    public int GetFlakeId()
    {
        return 0;
    }

    public async Task<ZenDataResult> TryLoadPrimary(ZenIdentifier idSegment, Identifier identifier)
    {
        return new(ZenResult.Success);
    }

    public void Save(ref ulong snowId, ref long snowSegment, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
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
        foreach (var x in goshujin.SnowmanIdChain)
        {
            if (!x.Check())
            {
                errorDirectories ??= new();
                errorDirectories.Add(x.SnowmanDirectory);
            }
        }

        if (errorDirectories != null &&
            !await param.Query(ZenStartResult.ZenDirectoryError, errorDirectories.ToArray()))
        {
            return ZenStartResult.ZenFileError;
        }

        // if (param.DefaultFolder != null && !goshujin.SnowmanIdChain.TryGetValue(ZenDirectory.DefaultSnowmanId, out _))
        if (param.DefaultFolder != null && goshujin.SnowmanIdChain.Count == 0)
        {
            try
            {
                Directory.CreateDirectory(param.DefaultFolder);
                var defaultSnowman = new ZenDirectory(this.GetFreeSnowmanId(goshujin), param.DefaultFolder);
                goshujin.Add(defaultSnowman);
            }
            catch
            {
            }
        }

        foreach (var x in goshujin.SnowmanIdChain)
        {
            x.Start();
        }

        if (goshujin.SnowmanIdChain.Count == 0)
        {
            return ZenStartResult.NoDirectoryAvailable;
        }

        this.snowmanGoshujin = goshujin;
        return ZenStartResult.Success;
    }

    internal bool TryGetSnowman(ZenIdentifier idSegment, [MaybeNullWhen(false)] out ZenDirectory snowman)
    {
        return this.snowmanGoshujin.SnowmanIdChain.TryGetValue(idSegment.DirectoryId, out snowman);
    }

    internal byte[] Serialize()
    {
        return TinyhandSerializer.Serialize(this.snowmanGoshujin);
    }

    private uint GetFreeSnowmanId(ZenDirectory.GoshujinClass goshujin)
    {
        while(true)
        {
            var id = LP.Random.Pseudo.NextUInt32();
            if (!goshujin.SnowmanIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private ZenDirectory.GoshujinClass snowmanGoshujin = new();
}
