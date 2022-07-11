// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace LP.Custom;

public class CustomUnit : UnitBase, IUnitPreparable
{
    public static void SampleCode()
    {
        var builder = new CustomUnit.Builder()
            .Configure(x => { }); // Custom configuration

        var unit = builder.Build();
    }

    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {// Configuration for TestClass.
            this.Configure(context =>
            {
                context.AddSingleton<CustomUnit>();
                context.CreateInstance<CustomUnit>();
            });
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

    public CustomUnit(UnitContext context)
        : base(context)
    {
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        Logger.Default.Information("Custom unit prepared.");
    }
}
