// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.Machines;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class LpDogma
{
    public const string Filename = "LpDogma";

    [TinyhandObject(ImplicitKeyAsName = true)]
    public partial record class CredentialNode([property: KeyAsName(ConvertToString = true)] NetNode Node, [property: KeyAsName(ConvertToString = true)] SignaturePublicKey RemoteKey, [property: KeyAsName(ConvertToString = true)] SignaturePublicKey MergerKey);

    [KeyAsName]
    public CredentialNode[] CredentialNodes { get; set; } = [];
}
