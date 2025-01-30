// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Lp.Net;

[NetServiceInterface]
public interface IBasalService : INetService
{
    NetTask<BytePool.RentMemory> GetActiveNodes();

    NetTask<BytePool.RentMemory> DifferentiateCredential(ReadOnlyMemory<byte> memory);
}
