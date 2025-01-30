// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Relay;
using SimpleCommandLine;

namespace Lp.Subcommands.Relay;

[SimpleCommand("add-certificate-relay")]
public class AddCertificateRelaySubcommand : ISimpleCommandAsync<AddCertificateRelayOptions>
{
    public AddCertificateRelaySubcommand(ILogger<AddCertificateRelaySubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal, AuthorityControl authorityControl, VaultControl vaultControl, LpService lpService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
        this.authorityControl = authorityControl;
        this.vaultControl = vaultControl;
        this.lpService = lpService;
    }

    public async Task RunAsync(AddCertificateRelayOptions options, string[] args)
    {
        RelayCircuit relayCircuit = options.Incoming switch
        {
            true => this.netTerminal.IncomingCircuit,
            false => this.netTerminal.OutgoingCircuit,
        };

        this.userInterfaceService.WriteLine($"Add {relayCircuit.KindText} relay");

        // Code
        var seedKey = await this.lpService.GetSeedKey(this.logger, options.Code);
        if (seedKey is null)
        {
            return;
        }

        if (!NetNode.TryParseNetNode(this.logger, options.RelayNode, out var netNode))
        {
            return;
        }

        using (var relayConnection = await this.netTerminal.ConnectForRelay(netNode, true, 0))
        {
            if (relayConnection is null)
            {
                return;
            }

            var block = new AssignRelayBlock(true);
            var token = new CertificateToken<AssignRelayBlock>(block);
            seedKey.SignWithSalt(token, relayConnection.EmbryoSalt);
            var r = await relayConnection.SendAndReceive<CertificateToken<AssignRelayBlock>, AssignRelayResponse>(token).ConfigureAwait(false);
            if (r.IsFailure || r.Value is null)
            {
                Console.WriteLine(r.Result.ToString());
                return;
            }
            else if (r.Value.Result != RelayResult.Success)
            {
                Console.WriteLine(r.Result.ToString());
                return;
            }

            var result = await relayCircuit.AddRelay(block, r.Value, relayConnection);
            Console.WriteLine($"AddRelay: {result.ToString()}");
            Console.WriteLine(relayCircuit.NumberOfRelays);

            var outerAddress = new NetAddress(r.Value.OuterRelayId, netNode.Address);
            Console.WriteLine($"Outer address: {outerAddress.ToString()}");

            /*var outerEndpoint = new NetEndpoint(r.Value.RelayId, clientConnection.DestinationEndpoint.EndPoint);
            var packet = RelayOperatioPacket.SetOuterEndPoint(outerEndpoint);
            var result2 = await this.netTerminal.PacketTerminal.SendAndReceive<RelayOperatioPacket, RelayOperatioResponse>(NetAddress.Relay, packet, -1);
            Console.WriteLine($"CreateOuterEndPoint: {result2.Result.ToString()}");
            Console.WriteLine($"OuterEndPoint: {outerEndpoint.ToString()}");*/
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetTerminal netTerminal;
    private readonly AuthorityControl authorityControl;
    private readonly VaultControl vaultControl;
    private readonly LpService lpService;
}

public record AddCertificateRelayOptions
{
    [SimpleOption("Code", Required = true, Description = "Code")]
    public string Code { get; init; } = string.Empty;

    [SimpleOption("RelayNode", Required = true, Description = "Relay node")]
    public string RelayNode { get; init; } = string.Empty;

    [SimpleOption("Incoming", Required = false, Description = "")]
    public bool Incoming { get; init; } = false;
}
