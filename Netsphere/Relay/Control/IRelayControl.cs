// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Responder;

namespace Netsphere.Relay;

public interface IRelayControl
{
    int MaxSerialRelays { get; }

    int MaxParallelRelays { get; }

    long DefaultRelayRetensionMics { get; }

    long DefaultMaxRelayPoint { get; }

    long DefaultRestrictedIntervalMics { get; }

    void ProcessRegisterResponder(ResponderControl responders)
    {
    }
}
