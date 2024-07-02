// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Stats;

namespace Lp.Net;

[NetServiceObject]
internal partial class BasalServiceAgent : IBasalService
{
    public BasalServiceAgent(NetStats netStats)
    {
        this.netStats = netStats;
    }

    #region FieldAndProperty

    private readonly NetStats netStats;

    #endregion

    public async NetTask<BytePool.RentMemory> DifferentiateActiveNode(ReadOnlyMemory<byte> memory)
    {
        return this.netStats.NodeControl.DifferentiateOnlineNode(memory);
    }
}
