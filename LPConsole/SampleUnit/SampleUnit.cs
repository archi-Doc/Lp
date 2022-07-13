// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Tinyhand;

namespace LPConsole.Sample;

public class SampleUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public static void SampleCode()
    {
        var builder = new Builder()
            .Configure(x => { }); // Custom configuration

        var unit = builder.Build();
    }

    public static void Configure(UnitBuilderContext context)
    {
        context.AddSingleton<SampleUnit>();
        context.CreateInstance<SampleUnit>();
        context.AddSubcommand(typeof(SampleSubcommand));
    }

    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {// Configuration for TestClass.
            this.Configure(context => SampleUnit.Configure(context));
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

    public SampleUnit(UnitContext context)
        : base(context)
    {
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        // Load strings
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        try
        {
            HashedString.LoadAssembly(null, asm, "SampleUnit.strings-en.tinyhand");
            // HashedString.LoadAssembly("ja", asm, "SampleUnit.strings-en.tinyhand");
        }
        catch
        {
        }

        Logger.Default.Information("Sample unit prepared.");
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        Logger.Default.Information("Sample unit running.");
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        Logger.Default.Information("Sample unit terminated.");
    }
}
