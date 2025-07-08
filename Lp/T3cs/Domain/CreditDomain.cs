// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Net;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class CreditDomain
{
    #region FieldAndProperty

    [Key(0)]
    public DomainOption DomainOption { get; init; }

    private SeedKey? seedKey;
    private DomainServer? domainData;

    #endregion

    public CreditDomain(DomainOption domainOption)
    {
        this.DomainOption = domainOption;
    }

    public bool Initialize(SeedKey seedKey, DomainServer domainData)
    {
        if (!this.DomainOption.Credit.PrimaryMerger.Equals(seedKey.GetSignaturePublicKey()))
        {
            return false;
        }

        domainData.SetCredit(this.DomainOption.Credit);
        this.seedKey = seedKey;
        this.domainData = domainData;

        return true;
    }
}
