// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using System.Runtime.CompilerServices;
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

        var builder = new NetControl.Builder();

        // Netsphere
        var unit = builder.Build();
        await unit.Run(new NetOptions(), true);

        var netControl = unit.Context.ServiceProvider.GetRequiredService<NetControl>();
        await Test(netControl);

        await unit.Terminate();

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    private static async Task Test(NetControl netControl)
    {
        var node = await netControl.NetTerminal.UnsafeGetNetNode(new(IPAddress.Loopback, 49152));
        if (node is null)
        {
            await Console.Out.WriteLineAsync("No connection");
            return;
        }

        using (var connection = await netControl.NetTerminal.Connect(node))
        {
            if (connection is not null)
            {
                var service = connection.GetService<TestService>();

                var source = "Nupo";
                var destination = await service.DoubleString(source);
                await Console.Out.WriteLineAsync($"{source} -> {destination}");
            }
        }
    }
}
