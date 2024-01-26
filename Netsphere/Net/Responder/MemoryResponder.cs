// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Server;

namespace Netsphere.Responder;

public class MemoryResponder : SyncResponder<Memory<byte>, Memory<byte>>
{
    public static readonly INetResponder Instance = new MemoryResponder();

    public override Memory<byte> RespondSync(Memory<byte> value)
        => value;
}
