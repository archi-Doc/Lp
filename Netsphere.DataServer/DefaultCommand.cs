﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Stats;
using SimpleCommandLine;

namespace RemoteDataServer;

[SimpleCommand("default", Default = true)]
public class DefaultCommand : ISimpleCommandAsync<DefaultCommandOptions>
{
    public DefaultCommand(ILogger<DefaultCommandOptions> logger, NetControl netControl, RemoteDataControl remoteDataBroker)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.remoteData = remoteDataBroker;
    }

    public async Task RunAsync(DefaultCommandOptions options, string[] args)
    {
        this.PrepareKey(options);
        // await this.PrepareNodeAddress();
        await this.PunchNode(options.PunchNode);
        this.remoteData.Initialize(options.Directory);

        await Console.Out.WriteLineAsync($"{this.netControl.NetBase.NetOptions.NodeName}");
        await Console.Out.WriteLineAsync($"Node: {this.netControl.NetStats.GetOwnNetNode().ToString()}");
        await Console.Out.WriteLineAsync($"Remote key: {this.remoteData.RemotePublicKey.ToString()}");
        await Console.Out.WriteLineAsync($"Directory: {this.remoteData.DataDirectory}");
        await Console.Out.WriteLineAsync("Ctrl+C to exit");
        await Console.Out.WriteLineAsync();

        await ThreadCore.Root.Delay(Timeout.InfiniteTimeSpan); // Wait until the server shuts down.
    }

    private void PrepareKey(DefaultCommandOptions options)
    {
        if (NodePrivateKey.TryParse(options.NodePrivateKey, out var privateKey))
        {
            this.netControl.NetBase.SetNodePrivateKey(privateKey);
            this.netControl.NetTerminal.SetNodeKey(privateKey);
        }
        else if (CryptoHelper.TryParseFromEnvironmentVariable<NodePrivateKey>(NetConstants.NodePrivateKeyName, out privateKey))
        {
            this.netControl.NetBase.SetNodePrivateKey(privateKey);
            this.netControl.NetTerminal.SetNodeKey(privateKey);
        }

        if (SignaturePublicKey.TryParse(options.RemotePublicKey, out var publicKey))
        {
            this.remoteData.RemotePublicKey = publicKey;
        }
        else if (CryptoHelper.TryParseFromEnvironmentVariable<SignaturePublicKey>(NetConstants.RemotePublicKeyName, out publicKey))
        {
            this.remoteData.RemotePublicKey = publicKey;
        }
    }

    private async Task PunchNode(string punchNode)
    {
        if (!NetAddress.TryParse(punchNode, out var node))
        {
            if (!CryptoHelper.TryParseFromEnvironmentVariable<NetAddress>("punchnode", out node))
            {
                return;
            }
        }

        var sw = Stopwatch.StartNew();

        var p = new PingPacket("PunchMe");
        var result = await this.netControl.NetTerminal.PacketTerminal.SendAndReceive<PingPacket, PingPacketResponse>(node, p);

        sw.Stop();
        this.logger.TryGet()?.Log($"Punch: {result.ToString()} {sw.ElapsedMilliseconds} ms");
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
    private readonly RemoteDataControl remoteData;
}

public record DefaultCommandOptions
{
    [SimpleOption("Directory", Description = "Directory")]
    public string Directory { get; init; } = "Data";

    [SimpleOption("PunchNode", Description = "Punch node")]
    public string PunchNode { get; init; } = string.Empty;

    [SimpleOption("NodePrivatekey", Description = "Node private key")]
    public string NodePrivateKey { get; init; } = string.Empty;

    [SimpleOption("RemotePublickey", Description = "Remote public key")]
    public string RemotePublicKey { get; set; } = string.Empty;
}
