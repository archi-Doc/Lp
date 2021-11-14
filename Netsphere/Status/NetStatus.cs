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

    public Information Information { get; }
}
