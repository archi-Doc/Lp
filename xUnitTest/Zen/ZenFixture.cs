// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
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
        var builder = new ZenControl.Builder()
            .Configure(context =>
            {
                // Services
            });

        var unit = builder.Build();
        var param = new ZenControl.Unit.Param();

        this.ZenControl = unit.Context.ServiceProvider.GetRequiredService<ZenControl>();
        this.ZenControl.Zen.StartForTest();
    }

    public void Dispose()
    {
    }

    public ZenControl ZenControl { get; }
}
