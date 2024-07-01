// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Netsphere.Interfaces;

[NetServiceInterface]
public interface INodeControlService : INetService
{
    NetTask<BytePool.RentMemory> DifferentiateActiveNode(ReadOnlyMemory<byte> memory);
}
