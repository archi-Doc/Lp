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
    public partial record class Evol(
        Point LpPoint,
        Point DestinationPoint,
        SignaturePublicKey Originator,
        SignaturePublicKey Merger,
        SignaturePublicKey Linker)
    {
        // public long UpdatedMics { get; set; }

        public bool Validate()
        {
            if (this.LpPoint < 0 || this.LpPoint > LpConstants.MaxPoint)
            {
                return false;
            }

            if (this.DestinationPoint < 0 || this.DestinationPoint > LpConstants.MaxPoint)
            {
                return false;
            }

            if (!this.Originator.IsValid || !this.Merger.IsValid || !this.Linker.IsValid)
            {
                return false;
            }

            return true;
        }
    }

    /*[TinyhandObject(ImplicitKeyAsName = true)]
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
    }*/

    public Evol[] Evols { get; set; } = [];

    /*public CreditLink[] CreditNet { get; set; } = [];

    public Credential[] Mergers { get; set; } = [];

    public Credential[] Linkers { get; set; } = [];

    public Link[] Links { get; set; } = [];*/
}
