// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;

namespace QuickStart;

public class Program
{
    public static async Task Main(string[] args)
    {
        var unit = new NetControl.Builder().Build(); // Create a NetControl unit that implements communication functionality.
        await unit.Run(new NetOptions(), true); // Execute the created unit with default options.

        var netControl = unit.Context.ServiceProvider.GetRequiredService<NetControl>(); // Get a NetControl instance.
        using (var connection = await netControl.NetTerminal.UnsafeConnect(new(IPAddress.Loopback, 49152)))
        {// Connect to the server's address (loopback address).
         // All communication in Netsphere is encrypted, and connecting by specifying only the address is not recommended due to the risk of man-in-the-middle attacks.
            if (connection is null)
            {
                await Console.Out.WriteLineAsync("No connection");
            }
            else
            {
                var service = connection.GetService<ITestService>(); // Retrieve an instance of the target service.
                var input = "Nupo";
                var output = await service.DoubleString(input); // Arguments are sent to the server through the Tinyhand serializer, processed, and the results are received.
                await Console.Out.WriteLineAsync($"{input} -> {output}");
            }
        }

        await unit.Terminate(); // Perform the termination process for the unit.
    }
}
