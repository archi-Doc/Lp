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
    public BasalServiceAgent(LpBase lpBase, NetStats netStats, LpStats lpStats)
    {
        this.lpBase = lpBase;
        this.netStats = netStats;
        this.lpStats = lpStats;
    }

    #region FieldAndProperty

    private readonly LpBase lpBase;
    private readonly NetStats netStats;
    private readonly LpStats lpStats;

    #endregion

    public async NetTask<BytePool.RentMemory> GetActiveNodes()
    {
        return this.netStats.NodeControl.GetActiveNodes();
    }

    public async NetTask<BytePool.RentMemory> DifferentiateCredential(ReadOnlyMemory<byte> memory)
    {
        return CredentialProof.Integrality.Default.Differentiate(this.lpStats.Credentials, memory);
    }

    public async NetTask<string?> GetNodeInformation()
    {
        return this.lpBase.NodeName;
    }
}
