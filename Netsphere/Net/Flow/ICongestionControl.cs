// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

internal enum ProcessSendResult
{
    Complete,
    Remaining,
    Congestion,
}

internal interface ICongestionControl
{
    Connection Connection { get; }

    bool IsCongested { get; }

    bool Process(NetSender netSender);

    void Report();
}
