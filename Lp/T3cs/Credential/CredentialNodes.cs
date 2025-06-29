// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialNodes
{
    [Key(0)]
    private CredentialEvidence.GoshujinClass goshujin = new();

    public CredentialNodes()
    {
        this.goshujin.SyncAlias = true;
    }

    public bool CheckAuthorization(Credit credit)
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            foreach (var x in credit.Mergers)
            {
                if (!this.CheckAuthorizationInternal(x))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool CheckAuthorization(SignaturePublicKey publicKey)
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            if (!this.CheckAuthorizationInternal(publicKey))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryGet(SignaturePublicKey key, [MaybeNullWhen(false)] out CredentialEvidence credentialEvidence)
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            credentialEvidence = this.goshujin.CredentialKeyChain.FindFirst(key);
            return credentialEvidence is not null;
        }
    }

    public bool TryAdd(CredentialEvidence evidence)
    {
        if (evidence.ValidateAndVerify(default) != true)
        {
            return false;
        }

        using (this.goshujin.LockObject.EnterScope())
        {
            return ((IIntegralityGoshujin)this.goshujin).IntegrateObject(CredentialEvidence.Integrality.Default, evidence) == IntegralityResult.Success;
        }
    }

    public BytePool.RentMemory Differentiate(ReadOnlyMemory<byte> memory)
        => CredentialEvidence.Integrality.Default.Differentiate(this.goshujin, memory);

    public Task<IntegralityResultAndCount> Integrate(IntegralityBrokerDelegate broker)
        => CredentialEvidence.Integrality.Default.Integrate(this.goshujin, broker);

    public CredentialEvidence[] ToArray()
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            return this.goshujin.ToArray();
        }
    }

    internal void Validate()
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            TemporaryList<CredentialEvidence> toDelete = default;
            foreach (var evidence in this.goshujin)
            {
                if (!evidence.Validate(default))
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

    private bool CheckAuthorizationInternal(SignaturePublicKey publicKey)
    {
        if (!publicKey.Validate())
        {
            return false;
        }

        var credentialEvidence = this.goshujin.CredentialKeyChain.FindFirst(publicKey);
        if (credentialEvidence is null)
        {
            return false;
        }

        return true; //return credentialEvidence.IsAuthorized;
    }
}
