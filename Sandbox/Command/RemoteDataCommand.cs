// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Unit;
using Netsphere;
using SimpleCommandLine;

namespace Sandbox;

[SimpleCommand("remotedata")]
public class RemoteDataCommand : ISimpleCommandAsync<RemoteDataOptions>
{
    public RemoteDataCommand(FileLogger<FileLoggerOptions> fileLogger, ILogger<RemoteDataCommand> logger, NetControl netControl)
    {
        this.fileLogger = fileLogger;
        this.logger = logger;
        this.netControl = netControl;
    }

    public async Task RunAsync(RemoteDataOptions options, string[] args)
    {
        var netTerminal = this.netControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var r = await NetHelper.TryGetStreamService<Netsphere.RemoteData.IRemoteData>(netTerminal, options.NetNode, options.RemotePrivateKey, 100_000_000);
        if (r.Connection is null ||
            r.Service is null)
        {
            return;
        }

        try
        {
            var remoteData = r.Service;
            /*var data = Encoding.UTF8.GetBytes("test string");
            var sendStream = await remoteData.Put("test.txt", data.Length);
            if (sendStream is null)
            {
                return;
            }

            var result = await sendStream.Send(data);
            var resultValue = await sendStream.CompleteSendAndReceive();

            var receiveStream = await remoteData.Get("test.txt");
            if (receiveStream is null)
            {
                return;
            }

            var buffer = new byte[100];
            var r2 = await receiveStream.Receive(buffer);
            await Console.Out.WriteLineAsync(Encoding.UTF8.GetString(buffer, 0, r2.Written));*/

            await this.fileLogger.Flush(false);
            var path = this.fileLogger.GetCurrentPath();

            try
            {
                using var fileStream = File.OpenRead(path);
                var sendStream = await remoteData.Put("test2.txt", fileStream.Length);
                if (sendStream is not null)
                {
                    var r3 = await NetHelper.StreamToSendStream(fileStream, sendStream);
                }
            }
            catch
            {
            }

            try
            {
                using var fileStream = File.OpenWrite("test2.txt");
                var receiveStream = await remoteData.Get("test2.txt");
                if (receiveStream is not null)
                {
                    var r3 = await NetHelper.ReceiveStreamToStream(receiveStream, fileStream);
                }
            }
            catch
            {
            }
        }
        finally
        {
            r.Connection.Dispose();
        }
    }

    private readonly FileLogger<FileLoggerOptions> fileLogger;
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
