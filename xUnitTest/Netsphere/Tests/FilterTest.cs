// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Netsphere;
using Xunit;

namespace xUnitTest.Netsphere;

[Collection(TestServerCollection.Name)]
public class FilterTest
{
    [Fact]
    public async Task Test1()
    {
        var netControl = TestServer.Container.Resolve<NetControl>();
        using (var terminal = netControl.Terminal.Create(NodeInformation.Alternative))
        {
            var basicService = terminal.GetService<IFilterTestService>();
            var task = await basicService.NoFilter(1).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(1);
        }
    }
}
