// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;

namespace QuickStart;

public class Program
{
    public static async Task Main(string[] args)
    {
        var unit = new NetControl.Builder().Build(); // Using Builder pattern, create a NetControl unit that implements communication functionality.
        await unit.Run(new NetOptions(), true); // Execute the created unit with default options.

        var netControl = unit.Context.ServiceProvider.GetRequiredService<NetControl>(); // Get a NetControl instance.
        var node = await netControl.NetTerminal.UnsafeGetNetNode(new(IPAddress.Loopback, 49152));
        if (node is null)
        {
            await Console.Out.WriteLineAsync("No connection");
        }
        else
        {
            using (var connection = await netControl.NetTerminal.Connect(node))
            {
                if (connection is null)
                {
                    await Console.Out.WriteLineAsync("No connection");
                }
                else
                {
                    var service = connection.GetService<ITestService>();
                    var input = "Nupo";
                    var output = await service.DoubleString(input);
                    await Console.Out.WriteLineAsync($"{input} -> {output}");
                }
            }
        }

        await unit.Terminate();
    }
}
