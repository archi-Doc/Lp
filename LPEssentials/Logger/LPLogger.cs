// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

public class LPLogger
{
    public class Builder : UnitBuilder
    {
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                // Loggers (ConsoleAndFileLogger, BackgroundAndFileLogger, ConsoleLogger)
                context.AddSingleton<BackgroundAndFileLogger>();
                context.AddSingleton<ConsoleAndFileLogger>();
                context.AddSingleton<SerilogLogger>();

                // Resolver
                context.ClearLoggerResolver();
                context.AddLoggerResolver(context =>
                {
                    if (context.LogLevel == LogLevel.Debug)
                    {
                        context.SetOutput<EmptyLogger>();
                        return;
                    }

                    context.SetOutput<ConsoleAndFileLogger>();
                });
            });
        }
    }
}
