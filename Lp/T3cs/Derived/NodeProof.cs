// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class NodeProof : ProofWithPublicKey
{
    public static readonly long DefaultValidMics = Mics.FromDays(10);

    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    [Link(Type = ChainType.Ordered, TargetMember = nameof(SignedMics))]
    public NodeProof(SignaturePublicKey publicKey, NetNode netNode)
        : base(publicKey)
    {
        this.NetNode = netNode;
    }

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public NetNode NetNode { get; private set; }
}
