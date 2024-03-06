// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Netsphere;
using Netsphere.Packet;
using Xunit;

namespace xUnitTest.NetsphereTest;

[Collection(NetFixtureCollection.Name)]
public class StreamTest
{
    private readonly int[] dataLength = [0, 1, 10, 111, 300, 1_000, 1_372, 1_373, 1_400, 3_000, 10_000, 100_000, 1_000_000, 1_500_000, 2_000_000, ];
    private readonly byte[][] dataArray;

    public StreamTest(NetFixture netFixture)
    {
        this.netFixture = netFixture;

        this.dataArray = new byte[this.dataLength.Length][];
        for (var i = 0; i < this.dataLength.Length; i++)
        {
            var r = new Xoshiro256StarStar((ulong)i);
            this.dataArray[i] = new byte[this.dataLength[i]];
            r.NextBytes(this.dataArray[i]);
        }
    }

    private readonly NetFixture netFixture;

    [Fact]
    public async Task Test1()
    {
        using (var connection = await this.netFixture.NetControl.NetTerminal.Connect(NetNode.Alternative))
        {
            connection.IsNotNull();
            if (connection is null)
            {
                return;
            }

            var service = connection.GetService<IStreamService>();
            service.IsNotNull();
            if (service is null)
            {
                return;
            }

            await this.TestPingPing(service);
        }
    }

    private async Task TestPingPing(IStreamService service)
    {
        for (var i = 0; i < this.dataLength.Length; i++)
        {
            if (this.dataArray[i].Length <= NetFixture.MaxBlockSize)
            {
                var r = await service.Pingpong(this.dataArray[i]).ResponseAsync;
                r.Value!.SequenceEqual(this.dataArray[i]).IsTrue();
            }
        }
    }
}
