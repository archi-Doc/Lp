// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    public CredentialClass RelayCredentials { get; private set; } = new();

    [Key(2)]
    public CredentialClass CreditCredentials { get; private set; } = new();

    #endregion

    public Credentials()
    {
    }

    public void Validate()
    {
        this.MergerCredentials.Validate();
        this.RelayCredentials.Validate();
        this.CreditCredentials.Validate();
    }

    [TinyhandOnSerialized]
    private void OnSerialized()
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
