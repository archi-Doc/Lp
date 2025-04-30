// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Arc.Collections;
using Microsoft.VisualBasic;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialClass
{
    [Key(0)]
    private CredentialEvidence.GoshujinClass immutableGoshujin = new();

    private Lock lockObject = new();
    private CredentialEvidence.GoshujinClass? goshujin;

    public CredentialClass()
    {
    }

    public BytePool.RentMemory Differentiate(ReadOnlyMemory<byte> memory)
        => CredentialEvidence.Integrality.Default.Differentiate(this.immutableGoshujin, memory);

    public CredentialEvidence[] ToArray()
    {
        return this.immutableGoshujin.ToArray();
    }

    public void Validate()
    {
        using (this.lockObject.EnterScope())
        {
            this.PrepareGoshujin();

            TemporaryList<CredentialEvidence> toDelete = default;
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

            if (toDelete.Count > 0)
            {
                this.UpdateGoshujin();
            }
        }
    }

    public async Task<IntegralityResultAndCount> Integrate(IntegralityBrokerDelegate broker)
    {
        using (this.lockObject.EnterScope())
        {
            this.PrepareGoshujin();
        }

        var result = await CredentialEvidence.Integrality.Default.Integrate(this.goshujin, broker).ConfigureAwait(false);
        if (result.IsModified)
        {
            using (this.lockObject.EnterScope())
            {
                this.UpdateGoshujin();
            }
        }

        return result;
    }

    public bool TryAdd(CredentialEvidence evidence)
    {
        if (evidence.ValidateAndVerify() != true)
        {
            return false;
        }

        using (this.lockObject.EnterScope())
        {
            this.PrepareGoshujin();
            var result = ((IIntegralityGoshujin)this.goshujin).IntegrateObject(CredentialEvidence.Integrality.Default, evidence);
            if (result == IntegralityResult.Success)
            {
                this.UpdateGoshujin();
            }
        }

        return true;
    }

    public bool TryGet(SignaturePublicKey key, [MaybeNullWhen(false)] out CredentialEvidence credentialEvidence)
    {
        credentialEvidence = this.immutableGoshujin.CredentialKeyChain.FindFirst(key);
        return credentialEvidence is not null;
    }

    [MemberNotNull(nameof(goshujin))]
    private void PrepareGoshujin()
    {// using (this.lockObject.EnterScope())
        if (this.goshujin is not null)
        {
            return;
        }

        this.goshujin ??= TinyhandSerializer.CloneObject(this.immutableGoshujin);
    }

    private void UpdateGoshujin()
    {// using (this.lockObject.EnterScope())
        var newGoshujin = TinyhandSerializer.CloneObject(this.goshujin);
        if (newGoshujin is not null)
        {
            this.immutableGoshujin = newGoshujin;
        }
    }
}
