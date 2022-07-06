// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Unit.Sample;

public class TestClass
{
    public static void SampleCode()
    {
        var builder = new TestClass.Builder()
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
    {// Unit class for customizing behavior.
        public record Param();

        public Unit(UnitParameter parameter)
            : base(parameter)
        {
        }

        public void RunStandalone(Param param)
        {
        }
    }

    public class TestUnit : UnitBase
    {
        public TestUnit(UnitParameter parameter)
            : base(parameter)
        {
        }

        public void Configure()
        {
        }
    }

    public TestClass(TestUnit testUnit)
    {
        this.Unit1 = testUnit;
    }

    public TestUnit Unit1 { get; }
}
