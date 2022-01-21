// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Netsphere;
using Xunit;

namespace xUnitTest.Netsphere;

[Collection(TestServerCollection.Name)]
public class NodeTest
{
    [Fact]
    public async Task Test1()
    {
        var netControl = TestServer.Container.Resolve<NetControl>();
        using (var terminal = netControl.Terminal.Create(NodeInformation.Alternative))
        {
            // terminal.SetMaximumResponseTime(3000);
            var basicService = terminal.GetService<IBasicService>();
            var task = await basicService.SendInt(1).ResponseAsync;
            task.Result.Is(NetResult.Success);

            var task2 = await basicService.IncrementInt(2).ResponseAsync;
            task2.Result.Is(NetResult.Success);
            task2.Value.Is(3);

            task2 = await basicService.SumInt(3, 4).ResponseAsync;
            task2.Result.Is(NetResult.Success);
            task2.Value.Is(7);
        }
    }
}
