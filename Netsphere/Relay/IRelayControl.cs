// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Relay;

public interface IRelayControl
{
    int MaxSerialRelays
        => 5;

    int MaxParallelRelays
        => 0;

    bool CreateRelay(ClientConnection clientConnection)
        => false;
}

public class GabaGabaRelayControl : IRelayControl
{
    public int MaxSerialRelays
        => 5;

    public int MaxParallelRelays
        => 100;

    public bool CreateRelay(CreateRelayBlock createRelayBlock)
        => true;
}
