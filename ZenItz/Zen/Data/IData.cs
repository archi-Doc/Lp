// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IData
{
    static abstract int StaticId { get; }

    static abstract bool PreloadData { get; }

    int Id { get; }
}
