// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public enum StreamState
{// Server: Receiving->Received->(Sent), Client: Receiving->Received
    Receiving,
    Received,
    Sent,
}
