// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TestLinkageProof : ContractableProofWithSigner
{
    public TestLinkageProof(SignaturePublicKey linkerPublicKey, Value value)
        : base(linkerPublicKey, value)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    public override bool Validate(ValidationOptions validationOptions)
    {
        return true;
    }
}
