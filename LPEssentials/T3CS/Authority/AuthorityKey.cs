// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public struct AuthorityKey : IDisposable
{
    public AuthorityKey()
    {
    }

    public void Dispose()
    {
        this.privateKey = null;
    }

    private PrivateKey? privateKey;
}
