﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public Task<BytePool.RentMemory> GetActiveNodes()
        => Task.FromResult(this.netStats.NodeControl.GetActiveNodes());

    public Task<BytePool.RentMemory> DifferentiateMergerCredential(ReadOnlyMemory<byte> memory)
        => Task.FromResult(this.credentials.MergerCredentials.Differentiate(memory));

    public async Task<string?> GetNodeInformation()
        => this.lpBase.NodeName;
}
