// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Obsolete;

public interface IItzShip<TPayload> : IItzShip
    where TPayload : IItzPayload
{
    void Set(in Identifier primaryId, in Identifier secondaryId, in TPayload value);

    ItzResult Get(in Identifier primaryId, in Identifier secondaryId, out TPayload value);
}

public interface IItzShip : ILPSerializable
{
    int Count();
}
