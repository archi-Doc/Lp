// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class AuthorityKey : IDisposable
{
    public enum KeyLifetime
    {
        Application,
        PeriodOfTime,
        Session,
    }

    public AuthorityKey(Authority authority, KeyLifetime lifetime)
    {
        this.authority = authority;
        // this.Name = name;
        this.Lifetime = lifetime;
    }

    public AuthorityKeyResult TrySignData(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        return AuthorityKeyResult.Success;
    }

    public void Dispose()
    {
        this.privateKey = null;
    }

    // public string Name { get; }

    public KeyLifetime Lifetime { get; }

    public long LifeMics { get; }

    public long ExpirationMics { get; }

    private AuthorityKeyResult PreparePrivateKey()
    {
        if (this.privateKey != null)
        {
            if (this.Lifetime == KeyLifetime.PeriodOfTime)
            {// Periof of time
                if (Mics.GetUtcNow() > this.ExpirationMics)
                {// Expired
                    this.privateKey = null;
                }
            }

            if (this.privateKey != null)
            {
                return AuthorityKeyResult.Success;
            }
        }

        // Try to get private key.

    }

    private Authority? authority;
    private PrivateKey? privateKey;
}
