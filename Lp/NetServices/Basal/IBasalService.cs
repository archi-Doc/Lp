// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Lp.Net;

[NetServiceInterface]
public interface IBasalService : INetService
{
    Task<BytePool.RentMemory> GetActiveNodes();

    Task<BytePool.RentMemory> DifferentiateMergerCredential(ReadOnlyMemory<byte> memory);

    Task<string?> GetNodeInformation();
}
