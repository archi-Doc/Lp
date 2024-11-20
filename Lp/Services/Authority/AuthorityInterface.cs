// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

internal sealed class AuthorityInterface
{
    public AuthorityInterface(AuthorityControl authorityControl, string name, byte[] encrypted)
    {
        this.authorityControl = authorityControl;
        this.Name = name;
        this.encrypted = encrypted;
    }

    public string Name { get; private set; }

    public long ExpirationMics { get; private set; }

    internal async Task<Authority?> Prepare()
    {
        if (this.authority is not null)
        {
            if (this.authority.Lifetime == AuthorityLifecycle.Duration)
            {// Periof of time
                if (Mics.GetUtcNow() > this.ExpirationMics)
                {// Expired
                    this.authority = null;
                }
            }

            if (this.authority is not null)
            {
                return this.authority;
            }
        }

        // Try to get AuthorityData.
        if (!PasswordEncryption.TryDecrypt(this.encrypted, string.Empty, out var decrypted))
        {
            while (true)
            {
                var passPhrase = await this.authorityControl.UserInterfaceService.RequestPassword(Hashed.Authority.EnterPassword, this.Name).ConfigureAwait(false);
                if (passPhrase == null)
                {// Canceled
                    return null;
                }

                if (PasswordEncryption.TryDecrypt(this.encrypted, passPhrase, out decrypted))
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
            if (this.authority.Lifetime == AuthorityLifecycle.Duration)
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

    private AuthorityControl authorityControl;
    private byte[] encrypted;
    private Authority? authority;
}
