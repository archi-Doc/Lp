// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

internal sealed class AuthorityInterface
{
    public AuthorityInterface(AuthorityVault authorityVault, string name, byte[] encrypted)
    {
        this.authorityVault = authorityVault;
        this.Name = name;
        this.encrypted = encrypted;
    }

    public string Name { get; private set; }

    public long ExpirationMics { get; private set; }

    internal async Task<Authority?> Prepare()
    {
        if (this.authority != null)
        {
            if (this.authority.Lifetime == AuthorityLifetime.PeriodOfTime)
            {// Periof of time
                if (Mics.GetUtcNow() > this.ExpirationMics)
                {// Expired
                    this.authority = null;
                }
            }

            if (this.authority != null)
            {
                return this.authority;
            }
        }

        // Try to get AuthorityData.
        if (!PasswordEncrypt.TryDecrypt(this.encrypted, string.Empty, out var decrypted))
        {
            while (true)
            {
                var passPhrase = await this.authorityVault.UserInterfaceService.RequestPassword(Hashed.Authority.EnterPassword, this.Name).ConfigureAwait(false);
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
            this.authority = TinyhandSerializer.Deserialize<Authority>(decrypted);
        }
        catch
        {
        }

        if (this.authority != null)
        {
            if (this.authority.Lifetime == AuthorityLifetime.PeriodOfTime)
            {
                this.ExpirationMics = Mics.GetUtcNow() + this.authority.LifeMics;
            }

            return this.authority;
        }
        else
        {
            return null;
        }
    }

    private AuthorityVault authorityVault;
    private byte[] encrypted;
    private Authority? authority;
}
