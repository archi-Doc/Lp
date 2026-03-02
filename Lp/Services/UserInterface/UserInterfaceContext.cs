// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Services;

public class UserInterfaceContext
{
    public bool IsInitialized { get; private set; }

    public ServerConnection? Connection { get; set; }

    public UserInterfaceContext()
    {
    }

    public bool InitializeLocal()
    {
        if (this.IsInitialized)
        {
            return false;
        }

        this.Connection = default;
        return true;
    }

    public bool InitializeRemote(ServerConnection connection)
    {
        if (this.IsInitialized)
        {
            return false;
        }

        this.Connection = connection;
        return true;
    }
}
