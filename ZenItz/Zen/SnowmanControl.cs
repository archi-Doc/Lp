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

    internal ZenStartResult TryStart(ZenStart param, ReadOnlyMemory<byte> data)
    {
        Snowman.GoshujinClass? goshujin;
        try
        {
            goshujin = TinyhandSerializer.Deserialize<Snowman.GoshujinClass>(data);
            if (goshujin == null)
            {
                goshujin = new();
            }
        }
        catch
        {
            return ZenStartResult.ZenFileError;
        }

        var directoryError = false;
        foreach (var x in goshujin.SnowmanIdChain)
        {
            if (!x.Check(true))
            {
                directoryError = true;
            }
        }

        if (directoryError)
        {
            return ZenStartResult.ZenFileError;
        }

        if (param.DefaultFolder != null &&
            !goshujin.SnowmanIdChain.TryGetValue(Snowman.DefaultSnowmanId, out _))
        {
            var defaultSnowman = new Snowman(param.DefaultFolder);
            goshujin.Add(defaultSnowman);
        }

        foreach (var x in goshujin.SnowmanIdChain)
        {
            x.Start();
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
