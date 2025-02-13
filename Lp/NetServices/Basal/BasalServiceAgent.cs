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
    public BasalServiceAgent(LpBase lpBase, NetStats netStats, LpStats lpStats, Credentials credentials)
    {
        this.lpBase = lpBase;
        this.netStats = netStats;
        this.lpStats = lpStats;
        this.credentials = credentials;
    }

    #region FieldAndProperty

    private readonly LpBase lpBase;
    private readonly NetStats netStats;
    private readonly LpStats lpStats;
    private readonly Credentials credentials;

    #endregion

    public async NetTask<BytePool.RentMemory> GetActiveNodes()
    {
        return this.netStats.NodeControl.GetActiveNodes();
    }

    public NetTask<BytePool.RentMemory> DifferentiateMergerCredential(ReadOnlyMemory<byte> memory)
        => this.credentials.MergerCredentials.Differentiate(memory);

    public async NetTask<string?> GetNodeInformation()
    {
        return this.lpBase.NodeName;
    }
}
