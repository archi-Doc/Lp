// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class DomainStorage
{
    public const string Filename = "DomainStorage";

    [Key(0)]
    private ConcurrentDictionary<ulong, DomainData> domainDataDictionary = new();

    public DomainStorage()
    {
    }

    public bool TryGetDomainData(Credit credit, [MaybeNullWhen(false)] out DomainData domainData)
    {
        return this.TryGetDomainData(credit.GetXxHash3(), out domainData);
    }

    public bool TryGetDomainData(ulong domainHash, [MaybeNullWhen(false)] out DomainData domainData)
    {
        if (this.domainDataDictionary.TryGetValue(domainHash, out domainData))
        {
            return true;
        }

        domainData = default;
        return false;
    }

    internal DomainData AddDomainService(Credit domainCredit, DomainRole kind, SeedKey? domainSeedKey)
    {
        var domainHash = domainCredit.GetXxHash3();
        var serviceClass = this.domainDataDictionary.AddOrUpdate(
            domainHash,
            hash =>
            {//
                var serviceClass = new DomainData(domainCredit);
                serviceClass.Update(kind, domainSeedKey);
                return serviceClass;
            },
            (hash, original) =>
            {
                original.Update(kind, domainSeedKey);
                return original;
            });

        return serviceClass;
    }
}
