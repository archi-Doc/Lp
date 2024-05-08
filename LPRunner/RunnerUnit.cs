// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;

namespace Netsphere.Runner;

public class RunnerUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            // Configuration for Unit.
            this.Configure(context =>
            {
                context.AddSingleton<RunnerUnit>();
                context.AddSingleton<BigMachine>();
                context.AddSingleton<RunnerInformation>();
                context.CreateInstance<RunnerUnit>();

                // Command
                context.AddCommand(typeof(RunCommand));

                // Machines
                context.AddTransient<RunnerMachine>();

                // Net Services
                context.AddSingleton<RemoteControlAgent>();

                // Log filter
                // context.AddSingleton<ExampleLogFilter>();

                // Logger
                context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    x.SetOutput<ConsoleAndFileLogger>();
                });
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
            TinyhandSerializer.ServiceProvider = context.ServiceProvider;
        }

        public async Task RunAsync(string[] args)
        {
            // Create optional instances
            this.Context.CreateInstances();

            /*var lpBase = this.Context.ServiceProvider.GetRequiredService<LPBase>();
            var logger = this.Context.ServiceProvider.GetRequiredService<ILogger<RunnerMachine>>();
            var information = await this.LoadInformation(logger, Path.Combine(lpBase.RootDirectory, RunnerInformation.Path));
            if (information == null)
            {// Could not load RunnerInformation.
                return;
            }

            this.Context.ServiceProvider.GetRequiredService<RunnerBase>().Information = information;*/

            var unitOptions = this.Context.ServiceProvider.GetRequiredService<UnitOptions>();
            var information = this.Context.ServiceProvider.GetRequiredService<RunnerInformation>();
            if (!await information.Load(Path.Combine(unitOptions.RootDirectory, RunnerInformation.Path)))
            {
                return;
            }

            var netBase = this.Context.ServiceProvider.GetRequiredService<NetBase>();
            netBase.SetNodePrivateKey(information.NodeKey);

            var options = new NetOptions();
            options.Port = information.RunnerPort;
            options.NodeName = "Runner";
            await this.Run(options, true);

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

        /*private async Task<RunnerInformation?> LoadInformation(ILogger logger, string path)
        {
            try
            {
                var utf8 = await File.ReadAllBytesAsync(path);
                var information = TinyhandSerializer.DeserializeFromUtf8<RunnerInformation>(utf8);
                if (information != null)
                {// Success
                 // Update RunnerInformation
                    information.SetDefault();
                    var update = TinyhandSerializer.SerializeToUtf8(information);
                    if (!update.SequenceEqual(utf8))
                    {
                        await File.WriteAllBytesAsync(path, update);
                    }

                    return information;
                }
            }
            catch
            {
            }

            var newInformation = new RunnerInformation().SetDefault();
            await File.WriteAllBytesAsync(path, TinyhandSerializer.SerializeToUtf8(newInformation));

            logger.TryGet(LogLevel.Error)?.Log($"'{path}' could not be found and was created.");
            logger.TryGet(LogLevel.Error)?.Log($"Modify '{RunnerInformation.Path}', and restart LPRunner.");

            return null;
        }*/
    }

    public RunnerUnit(UnitContext context, ILogger<RunnerUnit> logger)
        : base(context)
    {
        this.logger = logger;
    }

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
    }

    async Task IUnitExecutable.StartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken)
    {
    }

    void IUnitExecutable.Stop(UnitMessage.Stop message)
    {
        throw new NotImplementedException();
    }

    async Task IUnitExecutable.TerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken)
    {
    }

    private ILogger<RunnerUnit> logger;
}
