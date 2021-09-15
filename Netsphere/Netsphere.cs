// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace LP.Net;

public class Netsphere
{
    public Netsphere()
    {
    }

    public MyStatus MyStatus { get; } = new();
}
