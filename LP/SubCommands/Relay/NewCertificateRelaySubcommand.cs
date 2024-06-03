// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere.Crypto;
using Netsphere.Relay;
using SimpleCommandLine;

namespace LP.Subcommands.Relay;

[SimpleCommand("new-certificate-relay", Description = "")]
public class NewCertificateRelaySubcommand : ISimpleCommandAsync<NewCertificateRelayOptions>
{
    public NewCertificateRelaySubcommand(ILogger<NewCertificateRelaySubcommand> logger, IUserInterfaceService userInterfaceService, NetTerminal netTerminal, AuthorityVault authorityVault)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netTerminal = netTerminal;
        this.authorityVault = authorityVault;
    }

    public async Task RunAsync(NewCertificateRelayOptions options, string[] args)
    {
        this.logger.TryGet()?.Log("New certificate relay");

        if (await this.authorityVault.GetAuthority(options.Authority) is not { } authority)
        {
            return;
        }

        if (!NetNode.TryParseNetNode(this.logger, options.RelayNode, out var netNode))
        {
            return;
        }

        using (var clientConnection = await this.netTerminal.ConnectForRelay(netNode, 0))
        {
            if (clientConnection is null)
            {
                return;
            }

            var block = new CreateRelayBlock(true);
            var token = new CertificateToken<CreateRelayBlock>(block);
            authority.SignWithSalt(token, clientConnection.Salt);
            var r = await clientConnection.SendAndReceive<CertificateToken<CreateRelayBlock>, CreateRelayResponse>(token).ConfigureAwait(false);
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

            var result = this.netTerminal.RelayCircuit.AddRelay(r.Value.RelayId, clientConnection, true);
            Console.WriteLine($"AddRelay: {result.ToString()}");
            Console.WriteLine(this.netTerminal.RelayCircuit.NumberOfRelays);

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
    private readonly AuthorityVault authorityVault;
}

public record NewCertificateRelayOptions
{
    [SimpleOption("authority", Required = true, Description = "Authority")]
    public string Authority { get; init; } = string.Empty;

    [SimpleOption("relaynode", Required = true, Description = "Relay node")]
    public string RelayNode { get; init; } = string.Empty;
}
