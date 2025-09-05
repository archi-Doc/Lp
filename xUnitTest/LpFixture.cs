// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp;
using Xunit;

namespace xUnitTest.Lp;

[CollectionDefinition(Name)]
public class LpFixtureCollection : ICollectionFixture<LpFixture>
{
    public const string Name = "LpFixture";
}

public class LpFixture : IDisposable
{
    public LpFixture()
    {
        var builder = new Control.Builder()
            .PreConfigure(context =>
            {
            })
            .Configure(context =>
            {
                // Subcommand

                // NetService

                // ServiceFilter

                // Unit

                // Looger resolver
                context.AddLoggerResolver(context =>
                {
                });
            });
        // .ConfigureBuilder(new LpConsole.Example.ExampleUnit.Builder()); // Alternative

        this.unit = builder.Build();
    }

    public void Dispose()
    {
        this.unit.Context.SendTerminateAsync(new()).Wait();
    }

    public IServiceProvider ServiceProvider => this.unit.Context.ServiceProvider;

    private Control.Unit unit;
}
