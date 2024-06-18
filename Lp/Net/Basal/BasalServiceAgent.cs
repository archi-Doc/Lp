// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using ValueLink.Integrality;

namespace Lp.Basal;

[NetServiceObject]
internal partial class BasalServiceAgent : IBasalService
{
    public NetTask<IntegralityResultMemory> DifferentiateOnlineNode(BytePool.RentMemory memory)
    {
        throw new NotImplementedException();
    }
}
