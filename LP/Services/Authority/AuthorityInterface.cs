// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;

namespace LP;

public readonly struct AuthorityInterface
{
    internal AuthorityInterface(AuthorityKey authorityKey, long salt)
    {
        this.authorityKey = authorityKey;
        this.Salt = salt;
    }

    // public AuthorityInterface WithSalt(long salt) => new AuthorityInterface(this.authorityKey, salt);

    public Task<(AuthorityResult Result, byte[] Signature)> SignData(Credit credit, byte[] data)
        => this.authorityKey.SignData(credit, data);

    public Task<AuthorityResult> VerifyData(Credit credit, byte[] data, byte[] signature)
        => this.authorityKey.VerifyData(credit, data, signature);

    public Task<(AuthorityResult Result, AuthorityData? AuthorityData)> GetInfo()
        => this.authorityKey.GetInfo();

    public readonly long Salt;
    private readonly AuthorityKey authorityKey;
}
