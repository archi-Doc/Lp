// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace LP.T3CS;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class Credential : CertificateToken<Value>
{
    public class Integrality : Integrality<Credential.GoshujinClass, Credential>
    {
        public static readonly ObjectPool<Integrality> Pool = new(
            () => new()
            {
                MaxItems = 1000,
                RemoveIfItemNotFound = false,
            },
            4);

        public override bool Validate(Credential newItem, Credential? oldItem)
        {
            return base.Validate(newItem, oldItem);
        }
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Originator")]
    public Credential()
    {
    }

    public SignaturePublicKey Originator => this.Target.Credit.Originator;
}

/*[TinyhandObject]
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

    [Key(5, AddProperty = "Signatures", Level = 0)]
    [MaxLength((1 + Credit.MaxMergers) * 2)]
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
}*/
