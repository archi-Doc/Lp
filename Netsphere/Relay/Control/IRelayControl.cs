// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Responder;

namespace Netsphere.Relay;

public interface IRelayControl
{
    int MaxSerialRelays { get; }

    int MaxParallelRelays { get; }

    long RelayDurationMics { get; }

    void ProcessRegisterResponder(ResponderControl responders)
    {
    }
}
