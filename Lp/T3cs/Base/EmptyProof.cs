// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

public sealed class EmptyProof : LinkableProof
{
    public static readonly EmptyProof Instance = new();

    private EmptyProof()
        : base(default!, default)
    {
    }

    public override PermittedSigner PermittedSigner => default;

    public override bool Validate() => false;
}
