// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IItzShip<TIdentifier, TPayload> : IItzShip
    where TPayload : IItzPayload
{
    void Set(in TIdentifier id, in TPayload value);

    ItzResult Get(in TIdentifier id, out TPayload value);
}

public interface IItzShip : ILPSerializable
{
    int Count();
}
