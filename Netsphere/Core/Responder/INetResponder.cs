// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Net;

public interface INetResponder
{
    ulong DataId { get; }

    bool Respond(TransmissionContext transmissionContext);
}
