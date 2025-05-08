// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    public CredentialEvidence.GoshujinClass MergerCredentials { get; private set; } = new();

    [Key(1)]
    public CredentialEvidence.GoshujinClass RelayCredentials { get; private set; } = new();

    [Key(2)]
    public CredentialEvidence.GoshujinClass CreditCredentials { get; private set; } = new();

    [Key(3)]
    public CredentialEvidence.GoshujinClass LinkerCredentials { get; private set; } = new();

    #endregion

    public Credentials()
    {
        this.MergerCredentials.SyncAlias = true;
        this.RelayCredentials.SyncAlias = true;
        this.CreditCredentials.SyncAlias = true;
        this.LinkerCredentials.SyncAlias = true;
    }

    public void Validate()
    {
        this.MergerCredentials.Validate();
        this.RelayCredentials.Validate();
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
        this.Validate();
    }
}
