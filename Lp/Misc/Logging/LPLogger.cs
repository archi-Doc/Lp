// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Lp.Logging;

public class LpLogger
{
    public class Builder : UnitBuilder
    {
        private static bool IsSubclassOfRawGeneric(Type? generic, Type? toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                // Loggers (ConsoleAndFileLogger, BackgroundAndFileLogger, ConsoleLogger)
                context.AddSingleton<BackgroundAndFileLogger>();
                context.AddSingleton<ConsoleAndFileLogger>();

                // Filters
                context.AddSingleton<MachineLogFilter>();
                context.AddSingleton<TemporaryMemoryLogFilter>();

                // Resolver
                context.ClearLoggerResolver();
                context.AddLoggerResolver(NetControl.LowLevelLoggerResolver<FileLogger<NetsphereLoggerOptions>>);
                context.AddLoggerResolver(context =>
                {
                    if (context.LogLevel == LogLevel.Debug)
                    {// Debug -> no output
                        // context.SetOutput<FileLogger<NetsphereLoggerOptions>>();
                        if (context.LogOutputType is null)
                        {
                            context.SetOutput<EmptyLogger>();
                        }

                        return;
                    }

                    if (IsSubclassOfRawGeneric(typeof(BigMachines.Machine<>), context.LogSourceType))
                    {// Machines
                        context.SetOutput<BackgroundAndFileLogger>();
                        context.SetFilter<MachineLogFilter>();
                        return;
                    }

                    /*if (context.LogSourceType == typeof(ClientTerminal))
                    {// ClientTerminal
                        if (context.TryGetOptions<LpOptions>(out var options) &&
                        options.NetsphereOptions.EnableLogger)
                        {
                            context.SetOutput<StreamLogger<ClientTerminalLoggerOptions>>();
                        }

                        return;
                    }
                    else if (context.LogSourceType == typeof(ServerTerminal))
                    {// ServerTerminal
                        if (context.TryGetOptions<LpOptions>(out var options) &&
                        options.NetsphereOptions.EnableLogger)
                        {
                            context.SetOutput<StreamLogger<ServerTerminalLoggerOptions>>();
                        }

                        return;
                    }
                    else if (context.LogSourceType == typeof(NetSocketObsolete))
                    {
                        context.SetOutput<EmptyLogger>();
                        return;
                    }*/

                    context.SetOutput<ConsoleAndFileLogger>();
                });
            });
        }
    }
}
