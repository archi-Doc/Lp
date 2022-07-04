// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Unit;
using LPEssentials.Radio;
using Microsoft.Extensions.DependencyInjection;
using static Netsphere.NetControlBuilder;
using static SimpleCommandLine.SimpleParser;

namespace Netsphere;

public class NetControlUnit : BuiltUnit
{
    public record Param(bool EnableServer, Func<ServerContext> NewServerContext, Func<CallContext> NewCallContext, string NodeName, NetsphereOptions Options, bool AllowUnsafeConnection);

    public NetControlUnit(UnitBuilderContext context)
        : base(context)
    {
    }

    public void RunStandalone(Param param)
    {
        var netBase = this.ServiceProvider.GetRequiredService<NetBase>();
        netBase.Initialize(param.EnableServer, param.NodeName, param.Options);
        netBase.AllowUnsafeConnection = param.AllowUnsafeConnection;

        var netControl = this.ServiceProvider.GetRequiredService<NetControl>();
        if (param.EnableServer)
        {
            netControl.SetupServer(param.NewServerContext, param.NewCallContext);
        }

        Logger.Configure(null);
        Radio.Send(new Message.Configure());
        Radio.SendAsync(new Message.StartAsync(ThreadCore.Root));
    }
}

public class NetControlBuilder : UnitBuilder<NetControlUnit>
{
    private static void ConfigureInternal(UnitBuilderContext context)
    {
        // Base
        context.TryAddSingleton<BigMachine<Identifier>>();

        // Main services
        context.AddSingleton<NetControl>();
        context.AddSingleton<NetBase>();
        context.AddSingleton<Terminal>();
        context.AddSingleton<EssentialNode>();
        context.AddSingleton<NetStatus>();
        context.AddTransient<Server>();
        context.AddTransient<NetService>();
        // serviceCollection.RegisterDelegate(x => new NetService(container), Reuse.Transient);

        // Machines
        context.AddTransient<LP.Machines.EssentialNetMachine>();

        // Subcommands
        context.AddCommand(typeof(LP.Subcommands.NetTestSubcommand));

        // Unit
        context.AddTransient<TestUnit>();
    }

    internal class TestUnit : IUnitConfigurable
    {
        public void Configure()
        {
        }
    }

    public NetControlBuilder()
        : base()
    {
        this.Configure(ConfigureInternal);
    }

    public record Param(bool EnableServer, Func<ServerContext> NewServerContext, Func<CallContext> NewCallContext, string NodeName, NetsphereOptions Options, bool AllowUnsafeConnection);

    /*public BuiltUnit BuildStandalone(Param param)
    {
        var built = this.Build();

        var netBase = built.ServiceProvider.GetRequiredService<NetBase>();
        netBase.Initialize(param.EnableServer, param.NodeName, param.Options);
        netBase.AllowUnsafeConnection = param.AllowUnsafeConnection;

        var netControl = built.ServiceProvider.GetRequiredService<NetControl>();
        if (param.EnableServer)
        {
            netControl.SetupServer(param.NewServerContext, param.NewCallContext);
        }

        Logger.Configure(null);
        Radio.Send(new Message.Configure());
        Radio.SendAsync(new Message.StartAsync(ThreadCore.Root));

        var action = () =>
        {
            var netBase = built.ServiceProvider.GetRequiredService<NetBase>();
            netBase.Initialize(param.EnableServer, param.NodeName, param.Options);
            netBase.AllowUnsafeConnection = param.AllowUnsafeConnection;

            var netControl = built.ServiceProvider.GetRequiredService<NetControl>();
            if (param.EnableServer)
            {
                netControl.SetupServer(param.NewServerContext, param.NewCallContext);
            }

            Logger.Configure(null);
            Radio.Send(new Message.Configure());
            Radio.SendAsync(new Message.StartAsync(ThreadCore.Root));
        }

        return built;
    }*/
}
