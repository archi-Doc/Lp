// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;

namespace LP;

public sealed class AuthorityInterface
{
    public AuthorityInterface(Authority authority, string name, byte[] encrypted)
    {
        this.authority = authority;
        this.Name = name;
        this.encrypted = encrypted;
    }

    public async Task<(AuthorityResult Result, byte[] Destination)> TrySignData(Credit credit, byte[] data)
    {
        var result = await this.Prepare().ConfigureAwait(false);
        if (result != AuthorityResult.Success)
        {
            return (result, Array.Empty<byte>());
        }

        // this.authorityInfo!.SignData(credit, data);

        return (AuthorityResult.Success, Array.Empty<byte>());
    }

    public AuthorityInfo? TryGetInfo()
    {
        var result = this.Prepare();
        return this.authorityInfo;
    }

    public async Task<AuthorityResult> Prepare()
    {
        if (this.authorityInfo != null)
        {
            if (this.authorityInfo.Lifetime == AuthorityLifetime.PeriodOfTime)
            {// Periof of time
                if (Mics.GetUtcNow() > this.ExpirationMics)
                {// Expired
                    this.authorityInfo = null;
                }
            }

            if (this.authorityInfo != null)
            {
                return AuthorityResult.Success;
            }
        }

        // Try to get AuthorityInfo.
        if (!PasswordEncrypt.TryDecrypt(this.encrypted, string.Empty, out var decrypted))
        {
            while (true)
            {
                var passPhrase = await this.authority.UserInterfaceService.RequestPassword(Hashed.Authority.EnterPassword, this.Name).ConfigureAwait(false);
                if (passPhrase == null)
                {
                    return AuthorityResult.Canceled;
                }

                if (PasswordEncrypt.TryDecrypt(this.encrypted, passPhrase, out decrypted))
                {
                    break;
                }
            }
        }

        // Deserialize
        try
        {
            this.authorityInfo = TinyhandSerializer.Deserialize<AuthorityInfo>(decrypted);
        }
        catch
        {
        }

        if (this.authorityInfo != null)
        {
            return AuthorityResult.Success;
        }
        else
        {
            return AuthorityResult.InvalidData;
        }
    }

    public string Name { get; private set; }

    public long ExpirationMics { get; }

    private Authority authority;
    private byte[] encrypted;
    private AuthorityInfo? authorityInfo;
}
