// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Unit;
using LP.NetServices;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("remotebench")]
public class RemoteBenchSubcommand : ISimpleCommandAsync<RemoteBenchOptions>
{
    public RemoteBenchSubcommand(ILogger<RemoteBenchSubcommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.netControl = netControl;
    }

    public async Task RunAsync(RemoteBenchOptions options, string[] args)
    {
        /*if (!NetNode.TryParse(options.Node, out var node))
        {// NetNode.TryParseNetNode(this.logger, options.Node, out var node)
            return;
        }*/

        if (!NetAddress.TryParse(this.logger, options.Node, out var address))
        {
            return;
        }

        var node = await this.netControl.NetTerminal.UnsafeGetNetNodeAsync(address);
        if (node is null)
        {
            return;
        }

        await Console.Out.WriteLineAsync("Wait about 3 seconds for the execution environment to stabilize.");
        try
        {
            await Task.Delay(3_000, ThreadCore.Root.CancellationToken);
        }
        catch
        {
            return;
        }

        // await this.TestPingpong(node);

        using (var connection = await this.netControl.NetTerminal.TryConnect(node))
        {
            if (connection is null)
            {
                return;
            }

            var privateKey = SignaturePrivateKey.Create();
            var agreement = connection.Agreement with { AllowBidirectionalConnection = true, MinimumConnectionRetentionSeconds = 300, };
            var token = new CertificateToken<ConnectionAgreement>(agreement);
            connection.SignWithSalt(token, privateKey);
            connection.ValidateAndVerifyWithSalt(token);

            var r = await connection.UpdateAgreement(token);

            var serverConnection = connection.PrepareBidirectional();
            var service = connection.GetService<IRemoteBenchHost>();
            if (await service.Register(token) == NetResult.Success)
            {
                this.logger.TryGet()?.Log($"Register: Success");
            }
            else
            {
                serverConnection.Close();
                this.logger.TryGet()?.Log($"Register: Failure");
                return;
            }

            var r2 = await service.OpenBidirectional(token);

            // connection.RequestAgreement();
            // connection.CreateBidirectionalService<IRemoteBenchHost, IRemoteBenchRunner>();
            // connection.InvokeBidirectional(Tinyhand.TinyhandHelper.GetFullNameId<IBenchmarkService>());
        }

        /*while (true)
        {
            this.logger.TryGet()?.Log($"Waiting...");
            if (await this.remoteBenchBroker.Wait() == false)
            {
                Console.WriteLine($"Exit");
                break;
            }

            this.logger.TryGet()?.Log($"Benchmark {node.ToString()}, Total/Concurrent: {this.remoteBenchBroker.Total}/{this.remoteBenchBroker.Concurrent}");
            await this.remoteBenchBroker.Process(netControl.NetTerminal, node);
        }*/
    }

    private async Task TestPingpong(NetNode node)
    {
        const int N = 100;

        using (var connection = await this.netControl.NetTerminal.TryConnect(node))
        {
            if (connection is null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            var service = connection.GetService<IRemoteBenchHost>();
            for (var i = 0; i < N; i++)
            {
                await service.Pingpong([0, 1, 2,]);
            }

            sw.Stop();

            this.logger.TryGet()?.Log($"Pingpong x {N} {sw.ElapsedMilliseconds} ms");
        }
    }

    private NetControl netControl { get; set; }
    private ILogger logger;
}

public record RemoteBenchOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
