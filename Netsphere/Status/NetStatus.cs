// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace LP.Net;

public class NetStatus
{
    public NetStatus(NetBase netBase)
    {
        this.NetBase = netBase;
    }

    public NodeInformation GetMyNodeInformation()
    {
        this.myNodeInformation.PublicKeyX = this.NetBase.NodePublicKey.X;
        this.myNodeInformation.PublicKeyY = this.NetBase.NodePublicKey.Y;

        return this.myNodeInformation;
    }

    public NetBase NetBase { get; }

    private NodeInformation myNodeInformation = new(new NodeAddress(IPAddress.None, 0));
}
