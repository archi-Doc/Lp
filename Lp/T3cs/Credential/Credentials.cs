// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Arc.Collections;
using ValueLink.Integrality;
using static Netsphere.Misc.TimeCorrection;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class CredentialClass
{
    [Key(0)]
    private CredentialEvidence.GoshujinClass goshujin = new();

    public CredentialClass()
    {
    }

    public void Validate()
    {
        using (this.goshujin.LockObject.EnterScope())
        {
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
        }
    }

    public Task<IntegralityResult> Integrate(IntegralityBrokerDelegate broker)
        => CredentialEvidence.Integrality.Default.Integrate(this.goshujin, broker);

    public void Add(CredentialEvidence evidence)
    {
        using (this.goshujin.LockObject.EnterScope())
        {
            var g = (IIntegralityGoshujin)this.goshujin;
            g.IntegrateObject(CredentialEvidence.Integrality.Default, evidence);
        }
    }
}

[TinyhandObject]
public partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    public CredentialClass MergerCredentials { get; private set; } = new();

    [Key(1)]
    public CredentialClass RelayCredentials { get; private set; } = new();

    [Key(2)]
    public CredentialClass CreditCredentials { get; private set; } = new();

    #endregion

    public Credentials()
    {
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized()
    {
        this.MergerCredentials.Validate();
        this.RelayCredentials.Validate();
        this.CreditCredentials.Validate();
    }
}
