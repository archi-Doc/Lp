// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit.Obsolete;

public class TestClass
{
    public static void SampleCode()
    {
        var builder = new Builder()
            .Configure(x => { }); // Custom configuration

        var unit = builder.Build();
        unit.RunStandalone(new());
    }

    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {// Configuration for TestClass.
            this.Configure(context =>
            {
                context.AddTransient<TestUnit>();
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

        public void RunStandalone(Param param)
        {
        }
    }

    public class TestUnit : UnitBase, IUnitPreparable
    {
        public TestUnit(UnitContext context)
            : base(context)
        {
        }

        public void Prepare(UnitMessage.Prepare message)
        {
        }
    }

    public TestClass(TestUnit testUnit)
    {
        this.Unit1 = testUnit;
    }

    public TestUnit Unit1 { get; }
}
