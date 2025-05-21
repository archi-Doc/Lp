// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialLinks
{
    [Key(0)]
    private LinkLinkage.GoshujinClass goshujin = new();

    public CredentialLinks()
    {
    }

    public void Validate()
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            TemporaryList<LinkLinkage> toDelete = default;
            foreach (var evidence in this.goshujin)
            {
                if (!evidence.Validate())
                {
                    toDelete.Add(evidence);
                }
            }

            foreach (var evidence in toDelete)
            {
                this.goshujin.Remove(evidence);
            }
        }
    }

    public bool TryGet(SignaturePublicKey linkerPublicKey, [MaybeNullWhen(false)] out LinkLinkage linkage)
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            linkage = this.goshujin.LinkerPublicKeyChain.FindFirst(linkerPublicKey);
            return linkage is not null;
        }
    }

    public bool TryAdd(LinkLinkage linkage)
    {
        if (!linkage.ValidateAndVerify())
        {
            return false;
        }

        using (this.goshujin.LockObject.EnterScope())
        {
            return ((IIntegralityGoshujin)this.goshujin).IntegrateObject(LinkLinkage.Integrality.Default, linkage) == IntegralityResult.Success;
        }
    }

    public BytePool.RentMemory Differentiate(ReadOnlyMemory<byte> memory)
        => LinkLinkage.Integrality.Default.Differentiate(this.goshujin, memory);

    public Task<IntegralityResultAndCount> Integrate(IntegralityBrokerDelegate broker)
        => LinkLinkage.Integrality.Default.Integrate(this.goshujin, broker);

    public LinkLinkage[] ToArray()
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            return this.goshujin.ToArray();
        }
    }
}
