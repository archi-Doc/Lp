// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Arc.Unit;
using Netsphere;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Sandbox;

[SimpleCommand("remotedata")]
public class RemoteDataCommand : ISimpleCommandAsync
{
    public RemoteDataCommand(ILogger<RemoteDataCommand> logger, NetControl netControl)
    {
        this.logger = logger;
        this.NetControl = netControl;
    }

    public async Task RunAsync(string[] args)
    {
        var netTerminal = this.NetControl.NetTerminal;
        var packetTerminal = netTerminal.PacketTerminal;

        var netNode = await netTerminal.UnsafeGetNetNode(NetAddress.Alternative);
        if (netNode is null)
        {
            return;
        }

        using (var connection = await netTerminal.Connect(netNode))
        {
            if (connection is null)
            {
                return;
            }

            var remoteData = connection.GetService<IRemoteData>();

            var sendStream = await remoteData.Put("test.txt", 100);
            if (sendStream is null)
            {
                return;
            }

            var result = await sendStream.SendBlock<string>("test.txt");
            if (result != NetResult.Success)
            {
                return;
            }

            result = await sendStream.Send(Encoding.UTF8.GetBytes("test string"));
            result = await sendStream.Complete();

            var receiveStream = await remoteData.Get("test.txt");
            if (receiveStream is null)
            {
                return;
            }

            var buffer = new byte[100];
            var r = await receiveStream.Receive(buffer);
        }
    }

    public NetControl NetControl { get; set; }

    private ILogger logger;
}
