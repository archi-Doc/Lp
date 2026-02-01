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
        var builder = new LpUnit.Builder()
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

        this.product = builder.Build();
    }

    public void Dispose()
    {
        this.product.Context.SendTerminate().Wait();
    }

    public IServiceProvider ServiceProvider => this.product.Context.ServiceProvider;

    private LpUnit.Product product;
}
