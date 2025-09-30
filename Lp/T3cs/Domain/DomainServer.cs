// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Lp.Net;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
[NetServiceObject]
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

    [MemberNotNullWhen(true, nameof(creditDomain), nameof(seedKey))]
    public bool IsActive => this.creditDomain is not null && this.seedKey is not null;

    private CreditDomain? creditDomain;
    private SeedKey? seedKey;
    private SignaturePublicKey publicKey;

    #endregion

    public DomainServer()
    {
    }

    public bool Initialize(CreditDomain creditDomain, SeedKey seedKey)
    {
        var publicKey = seedKey.GetSignaturePublicKey();
        if (!creditDomain.DomainOption.Credit.PrimaryMerger.Equals(ref publicKey))
        {
            return false;
        }

        if (!creditDomain.DomainOption.Credit.Equals(this.Credit))
        {
            this.Credit = creditDomain.DomainOption.Credit;
            this.Clear();
        }

        this.creditDomain = creditDomain;
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
