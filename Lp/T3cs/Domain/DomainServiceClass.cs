// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

internal class DomainServiceClass
{
    public enum Kind : byte
    {
        Root,
        Leaf,
        External,
    }

    private readonly Credit domainCredit;
    private SeedKey? domainSeedKey;

    public DomainServiceClass(Credit domainCredit)
    {
        this.domainCredit = domainCredit;
    }
}
