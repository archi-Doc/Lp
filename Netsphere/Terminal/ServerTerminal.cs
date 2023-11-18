// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Netsphere;

public class ServerTerminal : NetTerminalObsolete
{
    public const ushort DefaultReceiverNumber = 4;
    public const ushort MaxReceiverNumber = 16;

    internal ServerTerminal(Terminal terminal, NetEndPoint endPdoint, NetNode node, ulong gene)
        : base(terminal, endPdoint, node, gene)
    {// NodeInformation: Managed
    }

    public ushort ReceiverNumber { get; private set; } = DefaultReceiverNumber;

    private ConcurrentQueue<ServerOperation> receiverQueue = new();

    internal void SetReceiverNumber(ushort receiverNumber = DefaultReceiverNumber)
    {
        if (receiverNumber > MaxReceiverNumber)
        {
            receiverNumber = MaxReceiverNumber;
        }
        else if (receiverNumber == 0)
        {
            receiverNumber = 1;
        }

        this.ReceiverNumber = receiverNumber;
        this.EnsureReceiver();
    }

    internal void EnsureReceiver()
    {// Checked
        while (this.receiverQueue.Count < this.ReceiverNumber)
        {
            this.receiverQueue.Enqueue(new ServerOperation(this));
        }
    }

    internal async Task<(ServerOperation? Operation, NetReceivedData Received)> ReceiveAsync()
    {// Checked
        this.EnsureReceiver();
        if (!this.receiverQueue.TryDequeue(out var operation))
        {
            return (default, new(NetResult.NoReceiver));
        }

        var received = await operation.ReceiveAsync().ConfigureAwait(false);
        if (received.Result == NetResult.Success)
        {// Success
            return (operation, received);
        }
        else
        {// Timeout, Error
            return (default, received);
        }
    }
}
