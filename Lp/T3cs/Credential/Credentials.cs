// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    public CredentialNodes Nodes { get; private set; } = new();

    [Key(1)]
    public CredentialLinks Links { get; private set; } = new();

    #endregion

    public Credentials()
    {
    }

    public void Validate()
    {
        this.Nodes.Validate();
        this.Links.Validate();
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
