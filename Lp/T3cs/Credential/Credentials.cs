// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    private readonly CredentialEvidence.GoshujinClass credentialEvidences = new();

    #endregion

    public Credentials()
    {
        this.credentialEvidences.SyncAlias = true;
    }

    public CredentialEvidence[] LockAndToArray()
    {
        using (this.credentialEvidences.LockObject.EnterScope())
        {
            return this.ToArray();
        }
    }

    public bool LockAndTryGet(SignaturePublicKey publicKey, [MaybeNullWhen(false)] out CredentialEvidence credentialEvidence)
    {
        using (this.credentialEvidences.LockObject.EnterScope())
        {
            credentialEvidence = this.credentialEvidences.CredentialKeyChain.FindFirst(publicKey);
            return credentialEvidence is not null;
        }
    }

    public void Validate()
    {
        using (this.credentialEvidences.LockObject.EnterScope())
        {
            TemporaryList<CredentialEvidence> toDelete = default;
            foreach (var evidence in this.credentialEvidences)
            {
                if (!evidence.Validate())
                {
                    toDelete.Add(evidence);
                }
            }

            foreach (var evidence in toDelete)
            {
                this.credentialEvidences.Remove(evidence);
            }
        }
    }

    public bool TryAdd(CredentialEvidence evidence)
    {
        if (evidence.ValidateAndVerify() != true)
        {
            return false;
        }

        using (this.lockObject.EnterScope())
        {
            return ((IIntegralityGoshujin)this).IntegrateObject(Integrality.Default, evidence) == IntegralityResult.Success;
        }
    }

    public void Validate()
    {
        this.credentialEvidences.Validate();
    }

    [TinyhandOnSerialized]
    private void OnSerialized()
    {
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized()
    {
        this.Validate();
    }
}
