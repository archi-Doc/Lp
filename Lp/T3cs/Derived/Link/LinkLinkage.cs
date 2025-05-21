// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable, Integrality = true)]
public partial class LinkLinkage : Linkage
{
    public static bool TryCreate(LinkableEvidence evidence1, LinkableEvidence evidence2, [MaybeNullWhen(false)] out LinkLinkage linkage)
        => TryCreate(() => new LinkLinkage(), evidence1, evidence2, out linkage);

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

    protected LinkLinkage()
        : base()
    {
    }

    [Link(Name = "CreditLink", Type = ChainType.Unordered, AddValue = false)]
    public Credit Credit1 => this.LinkProof1.Value.Credit;

    // [Link(UnsafeTargetChain = "CreditLinkChain", AddValue = false)]
    public Credit Credit2 => this.LinkProof2.Value.Credit;

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public SignaturePublicKey LinkerPublicKey => this.LinkProof1.LinkerPublicKey;

    public LinkProof LinkProof1 => (LinkProof)this.BaseProof1;

    public LinkProof LinkProof2 => (LinkProof)this.BaseProof2;
}

/*[TinyhandObject]
public partial class LinkEvidence : Evidence
{
    [Key(Evidence.ReservedKeyCount)]
    public LinkProof LinkProof { get; private set; }

    public override Proof BaseProof => this.LinkProof;

    public LinkEvidence(LinkProof linkProof)
    {
        this.LinkProof = linkProof;
    }
}*/
