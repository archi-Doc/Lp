// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Xunit;
using ZenItz;

namespace xUnitTest.Zen;

[CollectionDefinition(TestZenCollection.Name)]
public class TestZenCollection : ICollectionFixture<TestZen>
{
    public const string Name = "TestZen";
}

public class TestZen : IDisposable
{
    public TestZen()
    {
        // DI Container
        ZenControl.Register(this.container);

        this.container.ValidateAndThrow();

        ZenControl.QuickStart();

        this.ZenControl = this.container.Resolve<ZenControl>();
        this.ZenControl.Zen.TryStartZen(new(ForceStart: true)).Wait();
    }

    public void Dispose()
    {
    }

    public ZenControl ZenControl { get; }

    private Container container = new();
}
