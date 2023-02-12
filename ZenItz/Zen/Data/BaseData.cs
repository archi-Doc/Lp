// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class BaseData : IBaseData
{
    public static int StaticId => 0;

    public static bool PreloadData => true;

    public int Id => StaticId;

    public void Initialize()
    {
    }

    public void LoadInternal()
    {
    }

    public void SaveInternal(ulong file, bool unload)
    {
    }
}
