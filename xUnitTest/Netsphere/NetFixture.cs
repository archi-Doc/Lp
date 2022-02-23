// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Netsphere;
using Xunit;

namespace xUnitTest.Netsphere;

[CollectionDefinition(NetFixtureCollection.Name)]
public class NetFixtureCollection : ICollectionFixture<NetFixture>
{
    public const string Name = "NetFixture";
}

public class NetFixture : IDisposable
{
    public NetFixture()
    {
        // DI Container
        NetControl.Register(this.container);

        // Services
        this.container.Register<BasicServiceImpl>(Reuse.Singleton);

        // Filters
        this.container.Register<NullFilter>(Reuse.Singleton);

        this.container.ValidateAndThrow();

        var options = new LP.Options.NetsphereOptions();
        options.EnableAlternative = true;
        options.EnableTestFeatures = true;
        NetControl.QuickStart(true, () => new TestServerContext(), () => new TestCallContext(), "test", options, true);

        this.NetControl = this.container.Resolve<NetControl>();
    }

    public void Dispose()
    {
    }

    public NetControl NetControl { get; }

    private Container container = new();
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
