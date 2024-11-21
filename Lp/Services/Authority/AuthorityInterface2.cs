// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;

namespace Lp.T3cs;

internal sealed class AuthorityInterface2
{
    public AuthorityInterface2(AuthorityControl2 authorityControl, string name, Vault vault, Authority2 authority, Credit credit)
    {
        this.authorityControl = authorityControl;
        this.Name = name;
        this.Vault = vault;
        this.Authority = authority;
        this.Credit = credit;
    }

    #region FieldAndProperty

    private readonly AuthorityControl2 authorityControl;

    public string Name { get; }

    public Vault Vault { get; }

    public Authority2 Authority { get; }

    public Credit Credit { get; }

    #endregion
}
