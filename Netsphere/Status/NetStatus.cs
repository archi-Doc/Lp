// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace LP.Net;

public class NetStatus
{
    public NetStatus(Information information)
    {
        this.Information = information;
    }

    public NodeInformation GetMyNodeInformation()
    {
        this.myNodeInformation.PublicKeyX = this.Information.NodePublicKey.X;
        this.myNodeInformation.PublicKeyY = this.Information.NodePublicKey.Y;

        return this.myNodeInformation;
    }

    public Information Information { get; }

    private NodeInformation myNodeInformation = new(new NodeAddress(IPAddress.None, 0));
}
