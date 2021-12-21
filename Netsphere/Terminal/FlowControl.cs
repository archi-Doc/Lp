// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class FlowControl
{
    public static readonly long TicksPerRound = Ticks.FromMilliseconds(2);
    public static readonly double TicksPerRoundRev = 1d / Ticks.FromMilliseconds(2);
    public static readonly double InitialSendCapacityPerRound = 2;
    public static readonly long InitialWindowTicks = Ticks.FromMilliseconds(300);

    public FlowControl()
    {
        this.sendCapacityAccumulated = 0;
        this.sendCapacityPerRound = InitialSendCapacityPerRound;
        this.windowTicks = InitialWindowTicks;
    }

    internal void Update(long currentTicks)
    {// lock (NetTerminal.SyncObject)
        if (currentTicks <= this.lastTicks)
        {
            return;
        }
        else if (this.lastTicks == 0)
        {
            this.lastTicks = currentTicks - TicksPerRound;
        }

        // Window
        if (this.nextWindowTicks == 0)
        {
            this.nextWindowTicks = currentTicks;
        }
        else if (this.nextWindowTicks <= currentTicks)
        {
            this.nextWindowTicks = currentTicks + this.windowTicks;

            // Update
        }

        // Send Capacity
        var roundElapsed = (currentTicks - this.lastTicks) * TicksPerRoundRev;
        this.sendCapacityAccumulated += this.sendCapacityPerRound * roundElapsed;
        var ceiling = Math.Ceiling(this.sendCapacityPerRound);
        if (this.sendCapacityAccumulated > ceiling)
        {
            this.sendCapacityAccumulated = ceiling;
        }

        this.lastTicks = currentTicks;
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

    private long lastTicks;
    private long windowTicks;
    private long nextWindowTicks;
    private double sendCapacityAccumulated;
    private double sendCapacityPerRound;
}
