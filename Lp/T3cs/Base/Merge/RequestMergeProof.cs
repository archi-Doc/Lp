// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
// [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class RequestMergeProof : ProofWithPublicKey
{
    // public static readonly long DefaultValidMics = Mics.FromDays(10);

    // public static readonly int DefaultValiditySeconds = Seconds.FromDays(10);

    [Key(ProofWithPublicKey.ReservedKeyCount)]
    public Credit Credit { get; private set; }

    // [Link(Primary = true, Type = ChainType.Unordered, TargetMember = nameof(PublicKey))]
    public RequestMergeProof(Credit credit, SignaturePublicKey publicKey)
        : base(publicKey)
    {
        this.Credit = credit;
    }

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Credit;
        return true;
    }
}
