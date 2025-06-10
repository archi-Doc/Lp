// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class LinkProof : ContractableProofWithSigner
{
    public override PermittedSigner PermittedSigner => PermittedSigner.Merger | PermittedSigner.LpKey;

    public LinkProof(SignaturePublicKey linkerPublicKey, Value value)
        : base(linkerPublicKey, value)
    {
    }
}
