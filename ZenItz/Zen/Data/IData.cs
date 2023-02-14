// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IData
{
    static abstract int StaticId { get; }

    static abstract object StaticNew();

    int Id { get; }
}
