// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit.Sample;

public class TestClass : UnitBase
{
    public TestClass(BuiltUnit controlUnit)
        : base(controlUnit)
    {
    }

    public void Configure()
    {
    }
}

public class TestCode
{
    public void Test()
    {
        var builder = new TemplateClass.Builder()
            .Configure(x => { });

        var unit = builder.Build();
        unit.RunStandalone(new());

        var built = (BuiltUnit)unit;
        built.Run();
    }
}

public class TemplateClass
{
    public class Builder : UnitBuilder<Unit>
    {
        public Builder()
            : base()
        {// Configuration
            this.Configure(a => { });
            // this.Configure(ConfigureInternal);
        }
    }

    public class Unit : BuiltUnit
    {
        public record Param();

        public Unit(UnitBuilderContext context)
            : base(context)
        {
        }

        public void RunStandalone(Param param)
        {
        }
    }
}
