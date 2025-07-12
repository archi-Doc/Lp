// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Logging;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Stats;

#pragma warning disable SA1401

namespace Lp;

public abstract partial class MergerBase : UnitBase
{
    #region FieldAndProperty

    public abstract bool Initialized { get; protected set; }

    public abstract string GetName();

    public abstract CredentialState GetState();

    public SignaturePublicKey PublicKey { get; protected set; }

    protected ILogger logger;
    protected ModestLogger modestLogger;
    protected NetBase netBase;
    protected LpBase lpBase;
    protected NetStats netStats;
    protected DomainControl domainControl;
    protected SeedKey seedKey = SeedKey.Invalid;
    protected long lastRegisteredMics;

    #endregion

    public MergerBase(UnitContext context, UnitLogger unitLogger, NetBase netBase, LpBase lpBase, NetStats netStats, DomainControl domainControl)
        : base(context)
    {
        this.logger = unitLogger.GetLogger(this.GetType());
        this.modestLogger = new(this.logger);
        this.modestLogger.SetSuppressionTime(TimeSpan.FromSeconds(5));
        this.netBase = netBase;
        this.lpBase = lpBase;
        this.netStats = netStats;
        this.domainControl = domainControl;
    }

    public async Task UpdateState()
    {
        if (!this.Initialized)
        {
            return;
        }

        // Check net node
        var state = this.GetState();
        state.NetNode = this.netStats.OwnNetNode;
        state.Name = this.GetName();
        if (state.NetNode is null)
        {
            this.modestLogger.NonConsecutive(Hashed.Error.NoFixedNode, LogLevel.Error)?.Log(Hashed.Error.NoFixedNode);
            return;
        }

        // Check node type
        if (this.netStats.OwnNodeType != NodeType.Direct)
        {
            this.modestLogger.NonConsecutive(Hashed.Error.NoDirectConnection, LogLevel.Error)?.Log(Hashed.Error.NoDirectConnection);
            return;
        }

        // Active
        if (!state.IsActive)
        {
            state.IsActive = true;
            this.logger.TryGet(LogLevel.Information)?.Log("Activated");
        }

        if (state.IsActive && state.NetNode.Address.IsValidIpv4AndIpv6)
        {
            if (!MicsRange.FromPastToFastCorrected(Mics.FromDays(1)).IsWithin(this.lastRegisteredMics))
            {
                this.lastRegisteredMics = Mics.FastCorrected;

                var nodeProof = new NodeProof(this.PublicKey, state.NetNode);
                this.seedKey.TrySign(nodeProof, NodeProof.DefaultValidMics);
                var result = await this.domainControl.RegisterNodeToDomain(nodeProof).ConfigureAwait(false);

                this.logger.TryGet(LogLevel.Information)?.Log(Hashed.Merger.Registration, result);
                if (result == NetResult.Success)
                {
                    // this.logger.TryGet(LogLevel.Information)?.Log(this.State.NetNode.ToString());
                }
            }
        }
    }
}
