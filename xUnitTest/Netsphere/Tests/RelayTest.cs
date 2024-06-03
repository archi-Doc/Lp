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

        this.dataArray = new byte[this.dataLength.Length][];
        for (var i = 0; i < this.dataLength.Length; i++)
        {
            var r = new Xoshiro256StarStar((ulong)this.dataLength[i]);
            this.dataArray[i] = new byte[this.dataLength[i]];
            r.NextBytes(this.dataArray[i]);
        }
    }

    private readonly int[] dataLength = [0, 1, 10, 111, 300, 1_000, 1_372, 1_373, 1_400, 3_000, 10_000, 100_000, 1_000_000, 1_500_000, 2_000_000,];
    private readonly byte[][] dataArray;

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

        using (var clientConnection = (await netTerminal.ConnectForOutgoingRelay(netNode, 0))!)
        {
            clientConnection.IsNotNull();

            var block = new CreateRelayBlock();
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, privateKey);
            var r = await clientConnection.SendAndReceive<CertificateToken<CreateRelayBlock>, CreateRelayResponse>(token);
            r.IsSuccess.IsTrue();
            r.Value.IsNotNull();

            var result = netTerminal.OutgoingCircuit.AddRelay(r.Value!.RelayId, clientConnection, true);
        }

        using (var clientConnection = (await netTerminal.ConnectForOutgoingRelay(netNode, 1))!)
        {
            clientConnection.IsNotNull();

            var block = new CreateRelayBlock();
            var token = new CertificateToken<CreateRelayBlock>(block);
            clientConnection.SignWithSalt(token, privateKey);
            var r = await clientConnection.SendAndReceive<CertificateToken<CreateRelayBlock>, CreateRelayResponse>(token);
            r.IsSuccess.IsTrue();
            r.Value.IsNotNull();

            var result = netTerminal.OutgoingCircuit.AddRelay(r.Value!.RelayId, clientConnection, true);

            var setRelayPacket = new RelayOperatioPacket();
            setRelayPacket.OuterEndPoint = new(r.Value.RelayId, clientConnection.DestinationEndpoint.EndPoint);
            await netTerminal.PacketTerminal.SendAndReceive<RelayOperatioPacket, RelayOperatioResponse>(NetAddress.Relay, setRelayPacket, -1);
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

            for (var i = 0; i < 10_000; i += 1_000)
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

        var st = await netTerminal.OutgoingCircuit.UnsafeDetailedToString();

        using (var connection = (await this.NetControl.NetTerminal.Connect(NetNode.Alternative, Connection.ConnectMode.NoReuse, 2))!)
        {
            var service = connection.GetService<IStreamService>();
            service.IsNotNull();
            if (service is null)
            {
                return;
            }

            await this.TestPutAndGetHash(service);
        }

        netTerminal.OutgoingCircuit.Clear();
    }

    public NetFixture NetFixture { get; }

    public NetControl NetControl => this.NetFixture.NetControl;

    private async Task TestPutAndGetHash(IStreamService service)
    {
        var buffer = new byte[12_345];
        for (var i = 1; i < this.dataLength.Length; i++)
        {
            var stream = await service.PutAndGetHash(this.dataLength[i]);
            stream.IsNotNull();
            if (stream is null)
            {
                break;
            }

            var memory = this.dataArray[i].AsMemory();
            while (!memory.IsEmpty)
            {
                var length = Math.Min(buffer.Length, memory.Length);
                memory.Slice(0, length).CopyTo(buffer);
                memory = memory.Slice(length);

                var r = await stream.Send(buffer.AsMemory(0, length));
                r.Is(NetResult.Success);
            }

            var r2 = await stream.CompleteSendAndReceive();
            r2.Value.Is(FarmHash.Hash64(this.dataArray[i]));
        }
    }
}
