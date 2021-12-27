// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class FlowControl
{
    public static readonly long MicsPerRound = Mics.FromMilliseconds(2);
    public static readonly double MicsPerRoundRev = 1d / Mics.FromMilliseconds(2);
    public static readonly double InitialSendCapacityPerRound = 20;
    public static readonly long InitialWindowMics = Mics.FromMilliseconds(300);

    public FlowControl(NetTerminal netTerminal)
    {
        this.NetTerminal = netTerminal;

        this.sendCapacityAccumulated = 0;
        this.sendCapacityPerRound = InitialSendCapacityPerRound;
        this.windowMics = InitialWindowMics;
    }

    public NetTerminal NetTerminal { get; }

    internal void Update(long currentMics)
    {// lock (NetTerminal.SyncObject)
        if (currentMics <= this.lastMics)
        {
            return;
        }
        else if (this.lastMics == 0)
        {
            this.lastMics = currentMics - MicsPerRound;
        }

        // Window
        if (this.nextWindowMics == 0)
        {
            this.nextWindowMics = currentMics;
        }
        else if (this.nextWindowMics <= currentMics)
        {
            this.nextWindowMics = currentMics + this.windowMics;

            // Update
        }

        // Send Capacity
        var roundElapsed = (currentMics - this.lastMics) * MicsPerRoundRev;
        this.sendCapacityAccumulated += this.sendCapacityPerRound * roundElapsed;
        var ceiling = Math.Ceiling(this.sendCapacityPerRound);
        if (this.sendCapacityAccumulated > ceiling)
        {
            this.sendCapacityAccumulated = ceiling;
        }

        this.lastMics = currentMics;
    }

    internal void ReportAck(long currentMics, long sentMics)
    {
        this.NetTerminal.TerminalLogger?.Information($"{currentMics - sentMics}");
    }

    internal void RentSendCapacity(out int sendCapacity)
    {// lock (NetTerminal.SyncObject)
        sendCapacity = (int)this.sendCapacityAccumulated;
        this.sendCapacityAccumulated -= sendCapacity;
    }

    internal void ReturnSendCapacity(int sendCapacity)
    {// lock (NetTerminal.SyncObject)
        this.sendCapacityAccumulated += sendCapacity;
    }

    private long lastMics;
    private long windowMics;
    private long nextWindowMics;
    private double sendCapacityAccumulated;
    private double sendCapacityPerRound;
}
