// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Core;
using Netsphere.Crypto;
using Netsphere.Responder;

namespace Netsphere.Relay;

public class CertificateRelayControl : IRelayControl
{
    public static readonly IRelayControl Instance = new CertificateRelayControl();

    private class CreateRelayResponder : AsyncResponder<CertificateToken<CreateRelayBlock>, CreateRelayResponse>
    {
        public CreateRelayResponder(CertificateRelayControl relayControl)
        {
            this.relayControl = relayControl;
        }

        public override NetResultValue<CreateRelayResponse> RespondAsync(CertificateToken<CreateRelayBlock> token)
        {
            if (this.ServerConnection.NetTerminal.RelayControl is not CertificateRelayControl ||
                !token.PublicKey.Equals(this.relayControl.CertificatePublicKey) ||
                !token.ValidateAndVerifyWithSalt(this.ServerConnection.Salt))
            {
                return new(NetResult.NotAuthenticated);
            }

            var relayAgent = this.ServerConnection.NetTerminal.RelayAgent;
            var result = relayAgent.Add(this.ServerConnection, token.Target, out var relayId, out var outerRelayId);
            var relayPoint = this.relayControl.DefaultMaxRelayPoint;
            var response = new CreateRelayResponse(result, relayId, outerRelayId, relayPoint);
            relayAgent.AddRelayPoint(relayId, relayPoint);

            return new(NetResult.Success, response);
        }

        private readonly CertificateRelayControl relayControl;
    }

    public int MaxSerialRelays
        => 5;

    public int MaxParallelRelays
        => 100;

    public long DefaultRelayRetensionMics
        => Mics.FromMinutes(5);

    public long DefaultMaxRelayPoint
        => 100_000;

    public long DefaultRestrictedIntervalMics
        => 20_000;

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
