// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;

namespace QuickStart;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {// Console window closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        var builder = new NetControl.Builder()
            .SetupOptions<NetOptions>((context, options) =>
            {// NetsphereOptions
                options.NodeName = "Test server";
                options.Port = 49152;
                options.EnableEssential = true;
                options.EnableServer = true;
            })
            .ConfigureSerivice(context =>
            {
                context.AddService<ITestService>();
            });

        // Netsphere
        var unit = builder.Build();
        var options = unit.Context.ServiceProvider.GetRequiredService<NetOptions>();
        await Console.Out.WriteLineAsync(options.ToString());

        var netControl = unit.Context.ServiceProvider.GetRequiredService<NetControl>();
        netControl.Services.Register<ITestService>();

        await unit.Run(options, true);

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync("Server: Ctrl+C to exit");
        await Console.Out.WriteLineAsync();

        while (await ThreadCore.Root.Delay(1_000))
        {
        }

        await unit.Terminate();

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
