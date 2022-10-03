// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class VaultKey : IDisposable
{
    public enum Lifetime
    {
        Singleton,
        Scoped,
        PeriodOfTime,
    }

    public VaultKey()
    {
    }

    public void Dispose()
    {
        this.privateKey = null;
    }

    private PrivateKey? privateKey;
}
