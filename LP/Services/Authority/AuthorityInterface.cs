// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;

namespace LP.T3CS;

internal sealed class AuthorityInterface
{
    public AuthorityInterface(Authority authority, string name, byte[] encrypted)
    {
        this.authority = authority;
        this.Name = name;
        this.encrypted = encrypted;
    }

    public string Name { get; private set; }

    public long ExpirationMics { get; private set; }

    internal async Task<AuthorityKey?> Prepare()
    {
        if (this.authorityKey != null)
        {
            if (this.authorityKey.Lifetime == AuthorityLifetime.PeriodOfTime)
            {// Periof of time
                if (Mics.GetUtcNow() > this.ExpirationMics)
                {// Expired
                    this.authorityKey = null;
                }
            }

            if (this.authorityKey != null)
            {
                return this.authorityKey;
            }
        }

        // Try to get AuthorityData.
        if (!PasswordEncrypt.TryDecrypt(this.encrypted, string.Empty, out var decrypted))
        {
            while (true)
            {
                var passPhrase = await this.authority.UserInterfaceService.RequestPassword(Hashed.Authority.EnterPassword, this.Name).ConfigureAwait(false);
                if (passPhrase == null)
                {// Canceled
                    return null;
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
            this.authorityKey = TinyhandSerializer.Deserialize<AuthorityKey>(decrypted);
        }
        catch
        {
        }

        if (this.authorityKey != null)
        {
            if (this.authorityKey.Lifetime == AuthorityLifetime.PeriodOfTime)
            {
                this.ExpirationMics = Mics.GetUtcNow() + this.authorityKey.LifeMics;
            }

            return this.authorityKey;
        }
        else
        {
            return null;
        }
    }

    private Authority authority;
    private byte[] encrypted;
    private AuthorityKey? authorityKey;
}
