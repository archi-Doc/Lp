// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lp.Net;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class DomainServer
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

    [MemberNotNullWhen(true, nameof(CreditDomain), nameof(seedKey))]
    public bool IsActive => this.CreditDomain is not null && this.seedKey is not null;

    [IgnoreMember]
    public CreditDomain? CreditDomain { get; private set; }

    private SeedKey? seedKey;
    private SignaturePublicKey publicKey;

    #endregion

    public DomainServer()
    {
    }

    public bool Initialize(CreditDomain creditDomain, SeedKey seedKey)
    {
        var publicKey = seedKey.GetSignaturePublicKey();
        if (!creditDomain.DomainIdentifier.Credit.PrimaryMerger.Equals(ref publicKey))
        {
            return false;
        }

        if (!creditDomain.DomainIdentifier.Credit.Equals(this.Credit))
        {
            this.Credit = creditDomain.DomainIdentifier.Credit;
            this.Clear();
        }

        this.CreditDomain = creditDomain;
        this.seedKey = seedKey;
        this.publicKey = publicKey;

        return true;
    }

    public void Clear()
    {
        this.Nodes.Clear();
        this.CreditEvols.Clear();
        this.DomainSignature = [];
        this.DomainEvols = [];
    }
}
