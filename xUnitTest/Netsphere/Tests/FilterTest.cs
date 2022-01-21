// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Xunit;

namespace xUnitTest.Netsphere;

[Collection(TestServerCollection.Name)]
public class FilterTest
{
    public FilterTest(TestServer testServer)
    {
        this.TestServer = testServer;
    }

    [Fact]
    public async Task Test1()
    {
        using (var terminal = this.NetControl.Terminal.Create(NodeInformation.Alternative))
        {
            var basicService = terminal.GetService<IFilterTestService>();
            var task = await basicService.NoFilter(1).ResponseAsync;
            task.Result.Is(NetResult.Success);
            task.Value.Is(1);
        }
    }

    public TestServer TestServer { get; }

    public NetControl NetControl => this.TestServer.NetControl;
}
