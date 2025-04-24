// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    public partial record class CredentialNode(
        [property: KeyAsName(ConvertToString = true)] NetNode NetNode,
        [property: KeyAsName(ConvertToString = true)] SignaturePublicKey MergerKey);

    [KeyAsName]
    public CredentialNode[] CredentialNodes { get; set; } = [];
}
