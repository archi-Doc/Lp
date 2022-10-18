// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using LP;
using LP.Data;
using LP.NetServices;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using SimpleCommandLine;

namespace LPRunner;

public class ConsoleUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            // Configuration for Unit.
            this.Configure(context =>
            {
                context.AddSingleton<ConsoleUnit>();
                context.CreateInstance<ConsoleUnit>();
                context.AddSingleton<Runner>();

                // Command
                context.AddCommand(typeof(ConsoleCommand));

                // Machines
                context.AddTransient<RunnerMachine>();

                // Net Services
                context.AddSingleton<IRemoteControlService, RemoteControlService>();

                // Log filter
                // context.AddSingleton<ExampleLogFilter>();

                // Logger
                context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    x.SetOutput<ConsoleAndFileLogger>();
                });
            });

            this.SetupOptions<LPBase>((context, lpBase) =>
            {// LPBase
                lpBase.Initialize(new LPOptions(), true, "relay");
            });

            this.SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/Log.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 2;
            });

            this.SetupOptions<ConsoleLoggerOptions>((context, options) =>
            {// ConsoleLoggerOptions
                options.Formatter.EnableColor = true;
            });

            this.AddBuilder(new NetControl.Builder());
        }
    }

    public class Unit : NetControl.Unit
    {// Unit class for customizing behaviors.
        public Unit(UnitContext context)
            : base(context)
        {
        }

        public async Task RunAsync(string[] args)
        {
            // Create optional instances
            this.Context.CreateInstances();

            var options = new LP.Data.NetsphereOptions();
            var param = new NetControl.Unit.Param(true, () => new ServerContext(), () => new CallContext(), "runner", options, true);
            await this.RunStandalone(param);

            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = this.Context.ServiceProvider,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true,
            };

            // Main
            // await SimpleParser.ParseAndRunAsync(this.Context.Commands, "example -string test", parserOptions);
            await SimpleParser.ParseAndRunAsync(this.Context.Commands, args, parserOptions);

            await this.Context.SendTerminateAsync(new());
        }
    }

    private class ExampleLogFilter : ILogFilter
    {
        public ExampleLogFilter(ConsoleUnit consoleUnit)
        {
            this.consoleUnit = consoleUnit;
        }

        public ILog? Filter(LogFilterParameter param)
        {// Log source/Event id/LogLevel -> Filter() -> ILog
            if (param.LogSourceType == typeof(ConsoleCommand))
            {
                // return null; // No log
                if (param.LogLevel == LogLevel.Error)
                {
                    return param.Context.TryGet<ConsoleAndFileLogger>(LogLevel.Fatal); // Error -> Fatal
                }
                else if (param.LogLevel == LogLevel.Fatal)
                {
                    return param.Context.TryGet<ConsoleAndFileLogger>(LogLevel.Error); // Fatal -> Error
                }
            }

            return param.OriginalLogger;
        }

        private ConsoleUnit consoleUnit;
    }

    public ConsoleUnit(UnitContext context, ILogger<ConsoleUnit> logger)
        : base(context)
    {
        this.logger = logger;
    }

    public void Prepare(UnitMessage.Prepare message)
    {
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
    }

    private ILogger<ConsoleUnit> logger;
}
