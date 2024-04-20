﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

public class NoRelayControl : IRelayControl
{
    public static readonly IRelayControl Instance = new NoRelayControl();

    public int MaxSerialRelays
        => 0;

    public int MaxParallelRelays
        => 0;
}
