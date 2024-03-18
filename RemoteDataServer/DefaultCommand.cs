// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Stats;
using SimpleCommandLine;

namespace RemoteDataServer;

[SimpleCommand("default", Default = true)]
public class DefaultCommand : ISimpleCommandAsync<DefaultCommandOptions>
{
    public DefaultCommand(ILogger<DefaultCommandOptions> logger, NetControl netControl, RemoteData remoteDataBroker)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.remoteData = remoteDataBroker;
    }

    public async Task RunAsync(DefaultCommandOptions options, string[] args)
    {
        this.PrepareKey(options);
        // await this.PrepareNodeAddress();
        this.remoteData.Initialize(options.Directory);

        await Console.Out.WriteLineAsync($"{this.netControl.NetBase.NetOptions.NodeName}");
        await Console.Out.WriteLineAsync($"Node: {this.netControl.NetStats.GetMyNetNode().ToString()}");
        await Console.Out.WriteLineAsync($"Remote key: {this.remoteData.RemotePublicKey.ToString()}");
        await Console.Out.WriteLineAsync($"Directory: {this.remoteData.Directory}");
        await Console.Out.WriteLineAsync("Ctrl+C to exit");
        await Console.Out.WriteLineAsync();

        await ThreadCore.Root.Delay(Timeout.InfiniteTimeSpan); // Wait until the server shuts down.
    }

    private void PrepareKey(DefaultCommandOptions options)
    {
        if (NodePrivateKey.TryParse(options.NodePrivateKey, out var privateKey))
        {
            this.netControl.NetBase.SetNodePrivateKey(privateKey);
        }
        else if (CryptoHelper.TryParseFromEnvironmentVariable<NodePrivateKey>(NetConstants.NodePrivateKeyName, out privateKey))
        {
            this.netControl.NetBase.SetNodePrivateKey(privateKey);
        }

        options.RemotePublicKey = "(BL2vjUcmAUU7U64PQV2yzwAHnAqVi7-9vaiD2HPw4fi-)"; // Test
        if (SignaturePublicKey.TryParse(options.RemotePublicKey, out var publicKey))
        {
            this.remoteData.RemotePublicKey = publicKey;
        }
        else if (CryptoHelper.TryParseFromEnvironmentVariable<SignaturePublicKey>(NetConstants.RemotePublicKeyName, out publicKey))
        {
            this.remoteData.RemotePublicKey = publicKey;
        }
    }

    private async Task PrepareNodeAddress()
    {
        var tasks = new List<Task<AddressQueryResult>>();
        tasks.Add(NetStatsHelper.GetIcanhazipIPv4());
        tasks.Add(NetStatsHelper.GetIcanhazipIPv6());

        var results = await Task.WhenAll(tasks);
        foreach (var x in results)
        {
            this.netControl.NetStats.ReportAddress(x);
        }
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
    private readonly RemoteData remoteData;
}

public record DefaultCommandOptions
{
    [SimpleOption("directory", Description = "Directory")]
    public string Directory { get; init; } = "Data";

    [SimpleOption("nodeprivatekey", Description = "Node private key")]
    public string NodePrivateKey { get; init; } = string.Empty;

    [SimpleOption("remotepublickey", Description = "Remote public key")]
    public string RemotePublicKey { get; set; } = string.Empty;
}
