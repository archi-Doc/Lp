// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Server;

namespace Netsphere.Responder;

public class TestStreamResponder : INetResponder
{
    public static readonly INetResponder Instance = new TestStreamResponder();

    public ulong DataId
        => 123456789;

    public bool Respond(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<int>(transmissionContext.Owner.Memory.Span, out var size))
        {
            transmissionContext.Return();
            return false;
        }

        transmissionContext.

        return true;
    }
}
