// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Immutable credit object.
/// </summary>
[TinyhandObject]
public sealed partial class Credential : IValidatable, IEquatable<Credential>
{
    public enum Type
    {
        Credit,
        Lockout,
    }

    public Credential()
    {
    }

    [Key(0)]
    public Credit Credit { get; private set; } = default!;

    [Key(1)]
    public long CredentialMics { get; private set; }

    [Key(2)]
    public Type CredentialType { get; private set; }

    [Key(5, AddProperty = "Signatures", Condition = false)]
    [MaxLength((1 + Value.MaxMergers) * 2)]
    private byte[] signatures = default!;

    public bool Validate()
    {
        if (!this.Credit.Validate())
        {
            return false;
        }

        return true;
    }

    public bool Equals(Credential? other)
    {
        if (other == null)
        {
            return false;
        }
        else if (!this.Credit.Equals(other.Credit))
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Credit);
}
