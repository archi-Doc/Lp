// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    public CredentialNodes Nodes { get; private set; } = new();

    [Key(1)]
    private readonly Linkage.GoshujinClass linkages = new();

    #endregion

    public Credentials()
    {
    }

    public void Validate()
    {
        this.Nodes.Validate();
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
