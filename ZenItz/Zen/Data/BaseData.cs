// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface BaseData : IData
{
    static int IData.StaticId => 0;

    // public int Id => IData.StaticId;

    public void Initialize(ZenOptions options, IFromDataToIO fromDataToIo)
    {
    }

    public void LoadInternal()
    {
    }

    public void SaveInternal(ulong file, bool unload)
    {
    }
}
