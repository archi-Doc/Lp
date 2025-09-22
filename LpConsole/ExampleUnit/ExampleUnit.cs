// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LpConsole.Example;

public class ExampleUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public static void SampleCode()
    {
        var builder = new Builder()
            .Configure(x => { }); // Custom configuration

        var unit = builder.Build();
    }

    public static void Configure(IUnitConfigurationContext context)
    {
        context.AddSingleton<ExampleUnit>();
        context.RegisterInstanceCreation<ExampleUnit>();
        context.AddSubcommand(typeof(ExampleSubcommand));
    }

    public class Builder : UnitBuilder<Product>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {// Configuration for TestClass.
            this.Configure(context => ExampleUnit.Configure(context));
        }
    }

    public class Product : UnitProduct
    {// Unit class for customizing behaviors.
        public record Param();

        public Product(UnitContext context)
            : base(context)
        {
        }
    }

    public ExampleUnit(UnitContext context, ILogger<ExampleUnit> logger)
        : base(context)
    {
        this.logger = logger;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
        // Load strings
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            HashedString.LoadAssembly(null, asm, "ExampleUnit.strings-en.tinyhand");
            // HashedString.LoadAssembly("ja", asm, "ExampleUnit.strings-ja.tinyhand");
        }
        catch
        {
        }

        this.logger.TryGet()?.Log("Example unit prepared");
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
        this.logger.TryGet()?.Log("Example unit started");
    }

    void IUnitExecutable.Stop(UnitMessage.Stop message)
    {
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
        this.logger.TryGet()?.Log("Example unit terminated");
    }

    private ILogger<ExampleUnit> logger;
}
