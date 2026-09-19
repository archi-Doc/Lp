// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Lp.Net;

[NetService]
public interface IBasalService : INetService
{
    Task<BytePool.RentedMemory> GetActiveNodes();

    Task<BytePool.RentedMemory> DifferentiateNodes(ReadOnlyMemory<byte> memory);

    Task<string?> GetNodeInformation();
}
