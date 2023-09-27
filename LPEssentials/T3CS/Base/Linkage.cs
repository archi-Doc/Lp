// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject]
public sealed partial class Linkage : IValidatable, IEquatable<Linkage>
{
    public Linkage()
    {
    }

    [Key(0)]
    public EngageProof Proof { get; private set; }

    [Key(7, AddProperty = "Signatures", Level = 0)]
    [MaxLength(Credit.MaxMergers)]
    private byte[] signatures = default!;

    public bool Equals(Linkage? other)
    {
        throw new NotImplementedException();
    }

    public bool Validate()
    {
        throw new NotImplementedException();
    }
}
