﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public partial class EvolLinkage : Linkage
{
    public static bool TryCreate(ContractableEvidence evidence1, ContractableEvidence evidence2, [MaybeNullWhen(false)] out EvolLinkage linkage)
        => TryCreate(() => new EvolLinkage(), evidence1, evidence2, out linkage);

    #region Integrality

    public class Integrality : Integrality<LinkLinkage.GoshujinClass, LinkLinkage>
    {
        public static readonly Integrality Default = new()
        {
            MaxItems = 1_000,
            RemoveIfItemNotFound = false,
        };

        public override bool Validate(LinkLinkage.GoshujinClass goshujin, LinkLinkage newItem, LinkLinkage? oldItem)
        {
            if (oldItem is not null &&
                oldItem.LinkedMics >= newItem.LinkedMics)
            {
                return false;
            }

            if (!newItem.ValidateAndVerify())
            {
                return false;
            }

            return true;
        }

        /*public override int Trim(GoshujinClass goshujin, int integratedCount)
        {
            return base.Trim(goshujin, integratedCount);
        }*/
    }

    #endregion

    protected EvolLinkage()
        : base()
    {
    }

    [Link(Name = "CreditLink", Type = ChainType.Unordered)]
    public Credit Credit1 => this.Proof1.Value.Credit;

    // [Link(UnsafeTargetChain = "CreditLinkChain")]
    public Credit Credit2 => this.Proof2.Value.Credit;

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public SignaturePublicKey LinkerPublicKey => this.Proof1.LinkerPublicKey;

    public new LinkProof Proof1 => (LinkProof)base.Proof1;

    public new LinkProof Proof2 => (LinkProof)base.Proof2;
}
