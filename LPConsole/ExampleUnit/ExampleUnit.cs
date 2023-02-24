// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LPConsole.Example;

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
        context.CreateInstance<ExampleUnit>();
        context.AddSubcommand(typeof(ExampleSubcommand));
    }

    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {// Configuration for TestClass.
            this.Configure(context => ExampleUnit.Configure(context));
        }
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public record Param();

        public Unit(UnitContext context)
            : base(context)
        {
        }
    }

    public ExampleUnit(UnitContext context, ILogger<ExampleUnit> logger)
        : base(context)
    {
        this.logger = logger;
    }

    public void Prepare(UnitMessage.Prepare message)
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

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.logger.TryGet()?.Log("Example unit running");
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.logger.TryGet()?.Log("Example unit terminated");
    }

    private ILogger<ExampleUnit> logger;
}
