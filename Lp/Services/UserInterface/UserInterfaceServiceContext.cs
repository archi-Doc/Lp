// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;

namespace Lp.Services;

public class UserInterfaceServiceContext
{
    public bool Initialized { get; private set; }

    public IRemoteUserInterfaceReceiver? Receiver { get; private set; }

    public UserInterfaceServiceContext()
    {
    }

    public void InitializeLocal()
    {
        if (this.Initialized)
        {
            return;
        }

        this.Initialized = true;
    }

    public void InitializeRemote(IRemoteUserInterfaceReceiver receiver)
    {
        if (this.Initialized)
        {
            return;
        }

        this.Initialized = true;
        this.Receiver = receiver;
    }
}
