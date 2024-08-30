// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Lp.Data;
using Lp.T3cs;
using Netsphere.Stats;
using ValueLink.Integrality;

namespace Lp.Net;

[NetServiceObject]
internal partial class BasalServiceAgent : IBasalService
{
    public BasalServiceAgent(NetStats netStats, LpStats lpStats)
    {
        this.netStats = netStats;
        this.lpStats = lpStats;
    }

    #region FieldAndProperty

    private readonly NetStats netStats;
    private readonly LpStats lpStats;

    #endregion

    public async NetTask<BytePool.RentMemory> DifferentiateActiveNode(ReadOnlyMemory<byte> memory)
    {
        return this.netStats.NodeControl.DifferentiateActiveNode(memory);
    }

    public async NetTask<BytePool.RentMemory> DifferentiateCredential(ReadOnlyMemory<byte> memory)
    {
        var integrality = Credential.Integrality.Pool.Get();
        var result = ((IIntegralityObject)this.lpStats.Credentials).Differentiate(integrality, memory);
        Credential.Integrality.Pool.Return(integrality);
        return result;
    }
}
