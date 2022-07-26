// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Xunit;

namespace xUnitTest.NetsphereTest;

[CollectionDefinition(NetFixtureCollection.Name)]
public class NetFixtureCollection : ICollectionFixture<NetFixture>
{
    public const string Name = "NetFixture";
}

public class NetFixture : IDisposable
{
    public const int MaximumResponseTime = 2000;

    public NetFixture()
    {
        var builder = new NetControl.Builder()
            .Configure(context =>
            {
                // NetService
                context.AddSingleton<BasicServiceImpl>();

                // ServiceFilter
                context.AddSingleton<NullFilter>();
            });

        var options = new LP.Data.NetsphereOptions();
        options.EnableAlternative = true;
        options.EnableTestFeatures = true;
        options.EnableLogger = false;

        var unit = builder.Build();
        var param = new NetControl.Unit.Param(true, () => new TestServerContext(), () => new TestCallContext(), "test", options, true);
        unit.RunStandalone(param).Wait();

        this.NetControl = unit.Context.ServiceProvider.GetRequiredService<NetControl>();
    }

    public void Dispose()
    {
    }

    public NetControl NetControl { get; }
}

public class TestServerContext : ServerContext
{
}

public class TestCallContext : CallContext<TestServerContext>
{
    public static new TestCallContext Current => (TestCallContext)CallContext.Current;

    public TestCallContext()
    {
    }
}
