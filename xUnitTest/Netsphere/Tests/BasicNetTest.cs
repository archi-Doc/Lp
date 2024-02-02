// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Netsphere;
using Netsphere.Packet;
using Xunit;

namespace xUnitTest.NetsphereTest;

[Collection(NetFixtureCollection.Name)]
public class BasicNetTest
{
    public BasicNetTest(NetFixture netFixture)
    {
        this.NetFixture = netFixture;
    }

    [Fact]
    public async Task Test1()
    {
        this.NetControl.ResponderControl.Register(Netsphere.Responder.MemoryResponder.Instance);

        var p = new PacketPing("test56789");
        var result = await this.NetControl.NetTerminal.PacketTerminal.SendAndReceive<PacketPing, PacketPingResponse>(NetAddress.Alternative, p);
        result.Result.Is(NetResult.Success);

        using (var connection = await this.NetControl.NetTerminal.TryConnect(NetNode.Alternative))
        {
            if (connection is null)
            {
                return;
            }

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
                RandomVault.Pseudo.NextBytes(array);
                var memory = await connection.SendAndReceive<Memory<byte>, Memory<byte>>(array.AsMemory());
                memory.Value.Span.SequenceEqual(array).IsTrue();
            }
        }
    }

    public NetFixture NetFixture { get; }

    public NetControl NetControl => this.NetFixture.NetControl;
}
