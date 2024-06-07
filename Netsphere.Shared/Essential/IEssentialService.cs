// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using ValueLink.Integrality;

namespace Netsphere.Interfaces;

[NetServiceInterface]
public interface IEssentialService : INetService
{
    NetTask<IntegralityResultMemory> DifferentiateEssentialNode(BytePool.RentMemory memory);
}
