// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Interfaces;
using Netsphere.Stats;
using ValueLink.Integrality;

namespace Lp.Basal;

[NetServiceObject]
internal partial class BasalServiceAgent : INodeControlService
{
    public BasalServiceAgent(NetStats netStats)
    {
        this.netStats = netStats;
    }

    #region FieldAndProperty

    private readonly NetStats netStats;

    #endregion

    public async NetTask<IntegralityResultMemory> DifferentiateActiveNode(BytePool.RentMemory memory)
    {
        try
        {
            return this.netStats.NodeControl.DifferentiateOnlineNode(memory);
        }
        finally
        {
            memory.Return();
        }
    }
}
