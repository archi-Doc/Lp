// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Core;
using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Responder;

namespace Netsphere.Relay;

public class CertificateRelayControl : IRelayControl
{
    private class CreateRelayResponder : AsyncResponder<CertificateToken<CreateRelayBlock>, CreateRelayResponse>
    {
        public static readonly INetResponder Instance = new CreateRelayResponder();

        public override CreateRelayResponse? RespondAsync(CertificateToken<CreateRelayBlock> token)
        {
            if (!this.ServerConnection.ValidateAndVerifyWithSalt(token))
            {
                return null;
            }

            var result = this.ServerConnection.NetTerminal.RelayAgent.Add(token.Target.RelayId, this.ServerConnection.DestinationNode);
            var response = new CreateRelayResponse(result);
            return response;
        }
    }

    public int MaxSerialRelays
        => 5;

    public int MaxParallelRelays
        => 100;

    public void ProcessRegisterResponder(ResponderControl responders)
    {
        responders.Register(CreateRelayResponder.Instance);
    }

    /*public void ProcessCreateRelay(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<CertificateToken<CreateRelayBlock>>(transmissionContext.Owner.Memory.Span, out var token))
        {
            transmissionContext.Result = NetResult.DeserializationFailed;
            transmissionContext.Return();
            return;
        }

        transmissionContext.Return();

        _ = Task.Run(() =>
        {
            if (!transmissionContext.ServerConnection.ValidateAndVerifyWithSalt(token))
            {
                transmissionContext.Result = NetResult.NotAuthorized;
                return;
            }

            var response = new CreateRelayResponse(RelayResult.Success);
            transmissionContext.SendAndForget(response);
        });
    }*/
}
