// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    internal async Task<AuthoritySeed?> Prepare()
    {
        if (this.authoritySeed != null)
        {
            if (this.authoritySeed.Lifetime == AuthorityLifetime.PeriodOfTime)
            {// Periof of time
                if (Mics.GetUtcNow() > this.ExpirationMics)
                {// Expired
                    this.authoritySeed = null;
                }
            }

            if (this.authoritySeed != null)
            {
                return this.authoritySeed;
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
            this.authoritySeed = TinyhandSerializer.Deserialize<AuthoritySeed>(decrypted);
        }
        catch
        {
        }

        if (this.authoritySeed != null)
        {
            if (this.authoritySeed.Lifetime == AuthorityLifetime.PeriodOfTime)
            {
                this.ExpirationMics = Mics.GetUtcNow() + this.authoritySeed.LifeMics;
            }

            return this.authoritySeed;
        }
        else
        {
            return null;
        }
    }

    private Authority authority;
    private byte[] encrypted;
    private AuthoritySeed? authoritySeed;
}
