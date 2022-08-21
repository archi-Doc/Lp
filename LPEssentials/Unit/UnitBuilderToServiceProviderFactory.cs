// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Creates an <see cref="IServiceProviderFactory{UnitBuilder}"/> instance from <see cref="UnitBuilder"/> instance.
/// </summary>
public class UnitBuilderToServiceProviderFactory : IServiceProviderFactory<UnitBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilderToServiceProviderFactory"/> class.
    /// </summary>
    /// <param name="builder">The underlying <see cref="UnitBuilder"/> instance that creates <see cref="UnitBuilderToServiceProviderFactory"/>.</param>
    public UnitBuilderToServiceProviderFactory(UnitBuilder builder)
    {
        this.builder = builder;
    }

    /// <inheritdoc/>
    public UnitBuilder CreateBuilder(IServiceCollection services)
    {
        this.builder ??= new UnitBuilder();
        this.builder.Configure(context =>
        {
            foreach (var x in services)
            {
                context.Services.Add(x);
            }
        });

        return this.builder;
    }

    /// <inheritdoc/>
    public IServiceProvider CreateServiceProvider(UnitBuilder builder)
    {
        var unit = builder.Build();
        return unit.Context.ServiceProvider;
    }

    private UnitBuilder? builder;
}
