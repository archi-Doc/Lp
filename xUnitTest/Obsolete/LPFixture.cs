// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Netsphere;
using Xunit;
using xUnitTest.Netsphere;
using ZenItz;

namespace xUnitTest.Obsolete;

[CollectionDefinition(LPCollection.Name)]
public class LPCollection : ICollectionFixture<LPFixture>
{
    public const string Name = "LPCollection";
}

public class LPFixture : IDisposable
{
    public LPFixture()
    {
        // DI Container
        NetControl.Register(this.container);
        ZenControl.Register(this.container);

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

        this.ZenControl = this.container.Resolve<ZenControl>();
        this.ZenControl.Zen.TryStartZen(new(ForceStart: true)).Wait();
    }

    public void Dispose()
    {
    }

    public NetControl NetControl { get; }

    public ZenControl ZenControl { get; }

    private Container container = new();
}

/*public class TestServerContext : ServerContext
{
}

public class TestCallContext : CallContext<TestServerContext>
{
    public static new TestCallContext Current => (TestCallContext)CallContext.Current;

    public TestCallContext()
    {
    }
}
*/
