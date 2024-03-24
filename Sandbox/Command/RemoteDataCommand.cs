// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Unit;
using Netsphere;
using SimpleCommandLine;

namespace Sandbox;

[SimpleCommand("remotedata")]
public class RemoteDataCommand : ISimpleCommandAsync<RemoteDataOptions>
{
    public RemoteDataCommand(ILogger<RemoteDataCommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.netControl = netControl;
    }

    public async Task RunAsync(RemoteDataOptions options, string[] args)
    {
        var netTerminal = this.netControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var r = await NetHelper.TryGetStreamService<IRemoteData>(netTerminal, options.NetNode, options.RemotePrivateKey, 100_000_000);
        if (r.Connection is null ||
            r.Service is null)
        {
            return;
        }

        try
        {
            var remoteData = r.Service;
            var sendStream = await remoteData.Put("test.txt", 100);
            if (sendStream is null)
            {
                return;
            }

            var result = await sendStream.Send(Encoding.UTF8.GetBytes("test string"));
            var resultValue = await sendStream.CompleteSendAndReceive();

            var receiveStream = await remoteData.Get("test.txt");
            if (receiveStream is null)
            {
                return;
            }

            var buffer = new byte[100];
            var r2 = await receiveStream.Receive(buffer);
            await Console.Out.WriteLineAsync(Encoding.UTF8.GetString(buffer, 0, r2.Written));
        }
        finally
        {
            r.Connection.Dispose();
        }
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
}

public record RemoteDataOptions
{
    [SimpleOption("netnode", Description = "Node address", Required = false)]
    public string NetNode { get; init; } = string.Empty;

    [SimpleOption("nodepublickey", Description = "Node public key", Required = false)]
    public string NodePublicKey { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Remote private key", Required = false)]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
