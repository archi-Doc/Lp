// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class NodeProof : ProofWithPublicKey
{
    public static readonly long DefaultValidMics = Mics.FromDays(10);

    public static readonly int DefaultValiditySeconds = Seconds.FromDays(10);

    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    public NodeProof(SignaturePublicKey publicKey, NetNode netNode)
        : base(publicKey)
    {
        this.NetNode = netNode;
    }

    [Link(Type = ChainType.Ordered)]
    public long PriorityMics => this.IsAuthorized ? this.SignedMics : this.SignedMics >> 1;

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public NetNode NetNode { get; private set; }

    [Key(ProofWithPublicKey.ReservedKeyCount + 1)]
    public bool IsAuthorized { get; set; }
}
