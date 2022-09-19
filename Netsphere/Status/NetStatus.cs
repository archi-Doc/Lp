// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace Netsphere;

public class NetStatus
{
    public NetStatus(NetBase netBase)
    {
        this.NetBase = netBase;
    }

    public NodeInformation GetMyNodeInformation(bool isAlternative)
    {
        NodeInformation? nodeInformation;
        if (isAlternative)
        {
            if (this.alternativeNodeInformation == null)
            {
                this.alternativeNodeInformation = new(new NodeAddress(IPAddress.None, 0));
                this.alternativeNodeInformation.PublicKeyX = NodeKey.AlternativePrivateKey.X;
                this.alternativeNodeInformation.PublicKeyY = NodeKey.AlternativePrivateKey.Y;
            }

            nodeInformation = this.alternativeNodeInformation;
        }
        else
        {
            if (this.myNodeInformation == null)
            {
                this.myNodeInformation = new(new NodeAddress(IPAddress.None, 0));
                this.myNodeInformation.PublicKeyX = this.NetBase.NodePublicKey.X;
                this.myNodeInformation.PublicKeyY = this.NetBase.NodePublicKey.Y;
            }

            nodeInformation = this.myNodeInformation;
        }

        return nodeInformation;
    }

    public NetBase NetBase { get; }

    private NodeInformation? myNodeInformation;

    private NodeInformation? alternativeNodeInformation;
}
