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
        if (isAlternative)
        {
            return this.AlternativeNodeInformation;
        }
        else
        {
            return this.MyNodeInformation;
        }
    }

    public void SetMyNodeAddress(NodeAddress nodeAddress)
    {
        this.MyNodeInformation.SetAddress(nodeAddress.Address);
    }

    public NetBase NetBase { get; }

    public NodeInformation MyNodeInformation
    {
        get
        {
            if (this.myNodeInformation == null)
            {
                this.myNodeInformation = new(new NodeAddress(IPAddress.None, 0));
                this.myNodeInformation.PublicKey = this.NetBase.NodePublicKey;
            }

            return this.myNodeInformation;
        }
    }

    public NodeInformation AlternativeNodeInformation
    {
        get
        {
            if (this.alternativeNodeInformation == null)
            {
                this.alternativeNodeInformation = new(new NodeAddress(IPAddress.None, 0));
                this.alternativeNodeInformation.PublicKey = NodePrivateKey.AlternativePrivateKey.ToPublicKey();
            }

            return this.alternativeNodeInformation;
        }
    }

    private NodeInformation? myNodeInformation;

    private NodeInformation? alternativeNodeInformation;
}
