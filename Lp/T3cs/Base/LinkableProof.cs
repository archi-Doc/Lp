// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TestLinkageProof : LinkableProof
{
    public TestLinkageProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    public override bool Validate()
    {
        return true;
    }
}

public sealed class InvalidProof : LinkableProof
{
    public static readonly InvalidProof Instance = new();

    private InvalidProof()
        : base(default!, default)
    {
    }

    public override PermittedSigner PermittedSigner => default;

    public override bool Validate() => false;
}

public abstract partial class LinkableProof : ProofWithSigner
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ProofWithSigner.ReservedKeyCount + 1;

    #region FieldAndProperty

    [Key(ProofWithSigner.ReservedKeyCount + 0)]
    public SignaturePublicKey LinkerPublicKey { get; private set; }

    #endregion

    public LinkableProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value)
    {
        this.LinkerPublicKey = linkerPublicKey;
    }

    public override bool TryGetLinkerPublicKey([MaybeNullWhen(false)] out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }
}
