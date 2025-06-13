// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

/// <summary>
/// LpDogma defines the initial provisional state of the Lp network.
/// </summary>
[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class LpDogma
{
    public const string Filename = "LpDogma";

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial record class CreditLink(
        Value Value1,
        Value Value2,
        SignaturePublicKey LinkerPublicKey)
    {
        public long UpdatedMics { get; set; }
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial record class Credential(
        SignaturePublicKey PublicKey,
        NetNode NetNode,
        Point Point)
    {
        public long UpdatedMics { get; set; }
    }

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial record class Link(
        SignaturePublicKey LinkerPublicKey,
        Credit Credit1,
        Credit Credit2,
        Point Point)
    {
        public long UpdatedMics { get; set; }
    }

    [KeyAsName]
    public CreditLink[] CreditNet { get; set; } = [];

    [KeyAsName]
    public Credential[] Mergers { get; set; } = [];

    [KeyAsName]
    public Credential[] Linkers { get; set; } = [];

    [KeyAsName]
    public Link[] Links { get; set; } = [];
}
