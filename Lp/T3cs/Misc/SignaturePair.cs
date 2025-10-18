// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.T3cs;

public readonly struct SignaturePair
{
    public readonly int Index;

    public readonly byte[]? Signature;

    public SignaturePair(int index, byte[]? signature)
    {
        this.Index = index;
        this.Signature = signature;
    }

    [MemberNotNullWhen(true, nameof(Signature))]
    public bool IsValid => this.Signature is not null;
}
