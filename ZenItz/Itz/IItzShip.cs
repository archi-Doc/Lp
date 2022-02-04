// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IItzShip<TPayload>
    where TPayload : IItzPayload
{
    void Set(Identifier primaryId, Identifier? secondaryId, ref TPayload value);

    ItzResult Get(Identifier primaryId, Identifier? secondaryId, out TPayload value);

    byte[] Serialize();

    bool Deserialize(ReadOnlyMemory<byte> memory);
}
