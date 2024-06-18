// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using ValueLink.Integrality;

namespace Lp.Basal;

[NetServiceInterface]
public interface IBasalService : INetService
{
    NetTask<IntegralityResultMemory> DifferentiateOnlineNode(BytePool.RentMemory memory);
}
