// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Netsphere;
using Netsphere.Crypto;
using Netsphere.Packet;
using Netsphere.Relay;
using Xunit;

namespace xUnitTest.NetsphereTest;

[Collection(NetFixtureCollection.Name)]
public class RelayTest
{
    public RelayTest(NetFixture netFixture)
    {
        this.NetFixture = netFixture;
    }

    [Fact]
    public async Task Test1()
    {
        var xo = new Xoshiro256StarStar(123);
        this.NetControl.Responders.Register(Netsphere.Responder.MemoryResponder.Instance);

        var netTerminal = this.NetControl.NetTerminal;
        var privateKey = SignaturePrivateKey.Create();
        if (netTerminal.RelayControl is CertificateRelayControl rc)
        {
            rc.SetCertificatePublicKey(privateKey.ToPublicKey());
        }

        var netNode = (await netTerminal.UnsafeGetNetNode(NetAddress.Alternative))!;
        netNode.IsNotNull();

        using (var clientConnection = (await netTerminal.ConnectForRelay(netNode, 0))!)
        {
            clientConnection.IsNotNull();

            var block = new CreateRelayBlock();
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, privateKey);
            var r = await clientConnection.SendAndReceive<CertificateToken<CreateRelayBlock>, CreateRelayResponse>(token);
            r.IsSuccess.IsTrue();
            r.Value.IsNotNull();

            var result = netTerminal.RelayCircuit.AddRelay(r.Value!.RelayId, clientConnection, true);
        }

        using (var clientConnection = (await netTerminal.ConnectForRelay(netNode, 1))!)
        {
            clientConnection.IsNotNull();

            var block = new CreateRelayBlock();
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, privateKey);
            var r = await clientConnection.SendAndReceive<CertificateToken<CreateRelayBlock>, CreateRelayResponse>(token);
            r.IsSuccess.IsTrue();
            r.Value.IsNotNull();

            var result = netTerminal.RelayCircuit.AddRelay(r.Value!.RelayId, clientConnection, true);

            var setRelayPacket = new SetRelayPacket();
            setRelayPacket.OuterEndPoint = new(r.Value.RelayId, clientConnection.DestinationEndpoint.EndPoint);
            await netTerminal.PacketTerminal.SendAndReceive<SetRelayPacket, SetRelayResponse>(NetAddress.Relay, setRelayPacket, -1);
        }

        using (var connection = (await this.NetControl.NetTerminal.Connect(NetNode.Alternative, Connection.ConnectMode.ReuseIfAvailable, 2))!)
        {
            connection.IsNotNull();
            var basicService = connection.GetService<IBasicService>();
            var task = await basicService.SendInt(1).ResponseAsync;
            task.Result.Is(NetResult.Success);

            var task2 = await basicService.IncrementInt(2).ResponseAsync;
            task2.Result.Is(NetResult.Success);
            task2.Value.Is(3);

            task2 = await basicService.SumInt(3, 4).ResponseAsync;
            task2.Result.Is(NetResult.Success);
            task2.Value.Is(7);

            for (var i = 3_000; i < 10_000; i += 1_000) //0
            {
                var array = new byte[i];
                xo.NextBytes(array);
                var memory = await connection.SendAndReceive<Memory<byte>, Memory<byte>>(array.AsMemory());
                memory.Value.Span.SequenceEqual(array).IsTrue();
            }

            var r = await basicService.TestResult().ResponseAsync;
            r.Result.Is(NetResult.InvalidOperation);

            var r2 = await basicService.TestResult2().ResponseAsync;
            r2.Result.Is(NetResult.StreamLengthLimit);
            r2.Value.Is(NetResult.StreamLengthLimit);
        }

        var st = await netTerminal.RelayCircuit.UnsafeDetailedToString();

        netTerminal.RelayCircuit.Clear();
    }

    public NetFixture NetFixture { get; }

    public NetControl NetControl => this.NetFixture.NetControl;
}
