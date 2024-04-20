// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Core;
using Netsphere.Packet;

namespace Netsphere.Responder;

public class CreateRelayBlockResponder : SyncResponder<CreateRelayBlock, CreateRelayResponse>
{
    public static readonly INetResponder Instance = new CreateRelayBlockResponder();

    public override CreateRelayResponse? RespondSync(CreateRelayBlock value)
    {
        var response = new CreateRelayResponse(RelayResult.Success);
        return response;
    }
}
