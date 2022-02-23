// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using DryIoc;
using Xunit;
using ZenItz;

namespace xUnitTest.ZenTest;

[CollectionDefinition(ZenFixtureCollection.Name)]
public class ZenFixtureCollection : ICollectionFixture<ZenFixture>
{
    public const string Name = "ZenFixture";
}

public class ZenFixture : IDisposable
{
    public ZenFixture()
    {
        // DI Container
        ZenControl.Register(this.container, null, false);

        this.container.ValidateAndThrow();

        this.ZenControl = this.container.Resolve<ZenControl>();
        this.ZenControl.Zen.StartZenForTest();
    }

    public void Dispose()
    {
    }

    public ZenControl ZenControl { get; }

    private Container container = new();
}
