// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Collections;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
public partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    public CredentialClass MergerCredentials { get; private set; } = new();

    [Key(1)]
    public CredentialEvidence.GoshujinClass RelayCredentials { get; private set; } = new();

    [Key(2)]
    public CredentialClass CreditCredentials { get; private set; } = new();

    [Key(3)]
    public CredentialClass LinkerCredentials { get; private set; } = new();

    #endregion

    public Credentials()
    {
        this.RelayCredentials.SyncAlias = true;
        // CredentialEvidence.Integrality.Default.Integrate(this.RelayCredentials, broker).ConfigureAwait(false);
        // CredentialEvidence.Integrality.Default.Differentiate(this.RelayCredentials, default);
    }

    public void Validate()
    {
        this.MergerCredentials.Validate();
        this.Validate(this.RelayCredentials); // this.RelayCredentials.Validate();
        this.CreditCredentials.Validate();
        this.LinkerCredentials.Validate();
    }

    [TinyhandOnSerialized]
    private void OnSerialized()
    {
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized()
    {
        this.MergerCredentials.Validate();
        this.Validate(this.RelayCredentials); // this.RelayCredentials.Validate();
        this.CreditCredentials.Validate();
        this.LinkerCredentials.Validate();
    }

    private void Validate(CredentialEvidence.GoshujinClass goshujin)
    {
        using (goshujin.LockObject.EnterScope())
        {
            TemporaryList<CredentialEvidence> toDelete = default;
            foreach (var evidence in goshujin)
            {
                if (!evidence.Validate())
                {
                    toDelete.Add(evidence);
                }
            }

            foreach (var evidence in toDelete)
            {
                goshujin.Remove(evidence);
            }
        }
    }
}
