// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Netsphere;
using Xunit;

namespace xUnitTest.Netsphere;

[CollectionDefinition(TestServerCollection.Name)]
public class TestServerCollection : ICollectionFixture<TestServer>
{
    public const string Name = "TestServer";
}

public class TestServer : IDisposable
{
    public static Container Container { get; } = new();

    public TestServer()
    {
        // DI Container
        NetControl.Register(Container);

        // Services
        Container.Register<BasicServiceImpl>(Reuse.Singleton);

        // Filters
        Container.Register<NullFilter>(Reuse.Singleton);

        Container.ValidateAndThrow();

        var options = new LP.Options.NetsphereOptions();
        options.EnableAlternative = true;
        options.EnableTestFeatures = true;
        NetControl.QuickStart(true, () => new TestServerContext(), () => new TestCallContext(), "test", options, true);
    }

    public void Dispose()
    {
    }
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
