// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Net;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
[NetServiceObject]
public partial record class DomainServer : IDomainService
{
    public const string Filename = "DomainServer";
    public const int MaxNodeCount = 1_000; // Maximum number of nodes in the domain data.

    #region FieldAndProperty

    [Key(0)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    [Key(1)]
    public NodeProof.GoshujinClass Nodes { get; private set; } = new();

    [Key(2)]
    public CreditEvols.GoshujinClass CreditEvols { get; private set; } = new();

    [Key(3)]
    public byte[] DomainSignature { get; private set; } = [];

    [Key(4)]
    public byte[] DomainEvols { get; private set; } = [];

    public bool IsActive => this.seedKey is not null;

    private SeedKey? seedKey;
    private SignaturePublicKey publicKey;

    #endregion

    public DomainServer()
    {
    }

    public bool Initialize(Credit credit, SeedKey seedKey)
    {
        var publicKey = seedKey.GetSignaturePublicKey();
        if (!credit.PrimaryMerger.Equals(ref publicKey))
        {
            return false;
        }

        this.SetCredit(credit);
        this.seedKey = seedKey;
        this.publicKey = publicKey;

        return true;
    }

    public void SetCredit(Credit credit)
    {
        if (!credit.Equals(this.Credit))
        {
            this.Credit = credit;
            this.Clear();
        }
    }

    public void Clear()
    {
        this.Nodes.Clear();
        this.CreditEvols.Clear();
        this.DomainSignature = [];
        this.DomainEvols = [];
    }

    async NetTask<NetResult> INetServiceWithOwner.Authenticate(OwnerToken token)
    {
        if (!this.IsActive)
        {
            return NetResult.NoNetService;
        }

        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (!token.ValidateAndVerify(serverConnection))
        {
            return NetResult.NotAuthenticated;
        }

        if (token.Credit is null)
        {
            return NetResult.InvalidData;
        }

        return NetResult.Success;
    }

    async NetTask<NetResult> IDomainService.RegisterNode(NodeProof nodeProof)
    {
        if (!this.IsActive)
        {
            return NetResult.NoNetService;
        }

        if (!nodeProof.NetNode.Validate() ||
            !nodeProof.NetNode.Address.IsValidIpv4AndIpv6)
        {
            //return NetResult.NoNetwork;
        }

        if (!nodeProof.ValidateAndVerify())
        {
            return NetResult.InvalidData;
        }

        using (this.Nodes.LockObject.EnterScope())
        {
            if (this.Nodes.PublicKeyChain.FindFirst(nodeProof.PublicKey) is { } first)
            {// Found
                if (first.SignedMics >= nodeProof.SignedMics)
                {// Existing proof is newer or equal
                    return NetResult.Success;
                }
                else
                {
                    first.Goshujin = default; // Remove the existing proof.
                }
            }

            this.Nodes.Add(nodeProof);

            while (this.Nodes.Count > DomainServer.MaxNodeCount)
            {// Remove the oldest NodeProofs if the count exceeds the maximum.
                if (this.Nodes.SignedMicsChain.First is { } node)
                {
                    node.Goshujin = default;
                }
            }
        }

        return NetResult.Success;
    }

    async Task<NetResultAndValue<NetNode>> IDomainService.GetNode(SignaturePublicKey publicKey)
    {
        if (!this.IsActive)
        {
            return new(NetResult.NoNetService);
        }

        using (this.Nodes.LockObject.EnterScope())
        {
            if (this.Nodes.PublicKeyChain.TryGetValue(publicKey, out var nodeProof))
            {
                return new(NetResult.Success, nodeProof.NetNode);
            }
            else
            {
                return new(NetResult.NotFound);
            }
        }
    }
}
