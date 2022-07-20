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
                // context.ClearLogger();

                // context.AddConsoleLogger();

                context.AddLoggerResolver(x =>
                {
                    if (x.LogSourceType == typeof(object))
                    {
                        return new();
                    }

                    return default; // ILogDestination, ILogFilter
                });
            });
        }
    }
}
