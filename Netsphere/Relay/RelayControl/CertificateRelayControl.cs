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
        public CreateRelayResponder(CertificateRelayControl relayControl)
        {
            this.relayControl = relayControl;
        }

        public override NetResultValue<CreateRelayResponse> RespondAsync(CertificateToken<CreateRelayBlock> token)
        {
            if (!token.PublicKey.Equals(this.relayControl.CertificatePublicKey) ||
                !this.ServerConnection.ValidateAndVerifyWithSalt(token))
            {
                return new(NetResult.NotAuthorized);
            }

            var result = this.ServerConnection.NetTerminal.RelayAgent.Add(token.Target.RelayId, this.ServerConnection.DestinationNode);
            var response = new CreateRelayResponse(result);
            return new(NetResult.Success, response);
        }

        private readonly CertificateRelayControl relayControl;
    }

    public int MaxSerialRelays
        => 5;

    public int MaxParallelRelays
        => 100;

    public SignaturePublicKey CertificatePublicKey { get; private set; }

    public void ProcessRegisterResponder(ResponderControl responders)
    {
        responders.Register(new CreateRelayResponder(this));
    }

    public void SetCertificatePublicKey(SignaturePublicKey publicKey)
    {
        this.CertificatePublicKey = publicKey;
    }
}
