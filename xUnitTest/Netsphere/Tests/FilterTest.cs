// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Xunit;

namespace xUnitTest.Netsphere;

[Collection(NetFixtureCollection.Name)]
public class FilterTest
{
    public FilterTest(NetFixture netFixture)
    {
        this.NetFixture = netFixture;
    }

    [Fact]
    public async Task Test1()
    {
        using (var terminal = this.NetControl.Terminal.Create(NodeInformation.Alternative))
        {
            var service = terminal.GetService<IFilterTestService>();
            var task = await service.NoFilter(1).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(1);

            task = await service.Increment(2).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(3);

            task = await service.Multiply2(3).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(6);

            task = await service.Multiply3(3).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(9);

            task = await service.IncrementAndMultiply2(4).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(10);

            task = await service.Multiply2AndIncrement(5).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(11);
        }
    }

    public NetFixture NetFixture { get; }

    public NetControl NetControl => this.NetFixture.NetControl;
}
