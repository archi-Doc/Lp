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

    public long ExpireMics { get; }

    private AuthorityKeyResult PreparePrivateKey()
    {

    }

    private Authority? authority;
    private PrivateKey? privateKey;
}
