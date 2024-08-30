// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = true)]
public partial class Credential : CertificateToken<Value>
{
    private const int MaxItems = 1_000;

    public class Integrality : Integrality<Credential.GoshujinClass, Credential>
    {
        public static readonly ObjectPool<Integrality> Pool = new(
            () => new()
            {
                MaxItems = Credential.MaxItems,
                RemoveIfItemNotFound = false,
            },
            NetConstants.IntegralityDefaultPoolSize);

        public override bool Validate(Credential.GoshujinClass goshujin, Credential newItem, Credential? oldItem)
        {
            if (oldItem is not null &&
                oldItem.SignedMics >= newItem.SignedMics)
            {
                return false;
            }

            if (!newItem.ValidateAndVerify())
            {
                return false;
            }

            if (newItem.PublicKey.Equals(LpConstants.LpKey))
            {// Lp key
            }
            else if (goshujin.OriginatorChain.FindFirst(newItem.PublicKey) is null)
            {// Not found
                return false;
            }

            return true;
        }
    }

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, TargetMember = "Originator")]
    public Credential()
    {
    }

    public Credential(Value target)
    {
        this.Target = target;
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
