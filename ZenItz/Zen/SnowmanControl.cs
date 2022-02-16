// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public class SnowmanControl
{
    public SnowmanControl()
    {
    }

    public int GetFlakeId()
    {
        return 0;
    }

    public async Task<ZenDataResult> TryLoadPrimary(SnowFlakeIdSegment idSegment, Identifier identifier)
    {
        return new(ZenResult.Success);
    }

    public void Save(ref ulong snowId, ref long snowSegment, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
    }

    internal async Task<ZenStartResult> TryStart(ZenStartParam param, ReadOnlyMemory<byte>? data)
    {
        Snowman.GoshujinClass? goshujin = null;

        if (data != null)
        {
            try
            {
                goshujin = TinyhandSerializer.Deserialize<Snowman.GoshujinClass>(data.Value);
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

        if (param.DefaultFolder != null &&
            !goshujin.SnowmanIdChain.TryGetValue(Snowman.DefaultSnowmanId, out _))
        {
            try
            {
                Directory.CreateDirectory(param.DefaultFolder);
                var defaultSnowman = new Snowman(param.DefaultFolder);
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

    internal bool TryGetSnowman(SnowFlakeIdSegment idSegment, [MaybeNullWhen(false)] out Snowman snowman)
    {
        return this.snowmanGoshujin.SnowmanIdChain.TryGetValue(idSegment.SnowmanId, out snowman);
    }

    private Snowman.GoshujinClass snowmanGoshujin = new();
}
