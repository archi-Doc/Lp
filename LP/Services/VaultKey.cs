// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class VaultKey : IDisposable
{
    public enum KeyLifetime
    {
        Application,
        PeriodOfTime,
        Session,
    }

    public enum Result
    {
        Success,
    }

    public VaultKey(Vault vault, KeyLifetime lifetime)
    {
        this.vault = vault;
        // this.Name = name;
        this.Lifetime = lifetime;
    }

    public Result TrySignData(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        this.vault.
        return Result.Success;
    }

    public void Dispose()
    {
        this.privateKey = null;
    }

    // public string Name { get; }

    public KeyLifetime Lifetime { get; }

    public long LifeMics { get; }

    public long ExpireMics { get; }

    private Result PreparePrivateKey()
    {

    }

    private Vault? vault;
    private PrivateKey? privateKey;
}
