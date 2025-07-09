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

    #endregion

    public CreditDomain(DomainOption domainOption)
    {
        this.DomainOption = domainOption;
    }
}
