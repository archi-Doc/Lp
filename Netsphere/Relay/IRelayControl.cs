// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
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

    void ProcessCreateRelay(TransmissionContext transmissionContext)
    {
        transmissionContext.Result = NetResult.UnknownError;
        transmissionContext.Return();
    }
}

public class GabaGabaRelayControl : IRelayControl
{
    public int MaxSerialRelays
        => 5;

    public int MaxParallelRelays
        => 100;

    public bool CreateRelay(CreateRelayBlock createRelayBlock)
        => true;

    public void ProcessCreateRelay(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<CreateRelayBlock>(transmissionContext.Owner.Memory.Span, out var block))
        {
            transmissionContext.Result = NetResult.DeserializationFailed;
            transmissionContext.Return();
            return;
        }

        transmissionContext.Return();

        _ = Task.Run(() =>
        {
            var response = new CreateRelayResponse(RelayResult.Success);
            transmissionContext.SendAndForget(response);
        });
    }
}
