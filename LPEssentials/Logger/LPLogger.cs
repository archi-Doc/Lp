// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace LP;

public class LPLogger
{
    public class Builder : UnitBuilder
    {
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                context.ClearLoggerResolver();
                context.AddLoggerResolver(context =>
                {
                    if (context.LogSourceType == typeof(object))
                    {
                        context.SetOutput<ConsoleLogger>();
                        return;
                    }

                    context.SetOutput<ConsoleLogger>();
                });
            });
        }
    }
}
