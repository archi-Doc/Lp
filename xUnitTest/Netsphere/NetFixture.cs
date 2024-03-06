// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
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
            })
            .ConfigureSerivice(context =>
            {
                context.AddService<IBasicService>();
                context.AddService<IFilterTestService>();
            });

        var options = new NetOptions();
        options.EnableAlternative = true;
        options.EnableEssential = true;
        options.EnableServer = true;
        options.NodeName = "Test";

        this.unit = builder.Build();
        this.unit.Run(options, true).Wait();

        this.NetControl = this.unit.Context.ServiceProvider.GetRequiredService<NetControl>();
    }

    public void Dispose()
    {
        this.unit.Context.SendTerminateAsync(new()).Wait();
    }

    public NetControl NetControl { get; }

    private NetControl.Unit unit;
}
