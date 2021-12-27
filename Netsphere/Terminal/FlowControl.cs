// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class FlowControl
{
    public static readonly long MicsPerRound = Mics.FromMilliseconds(2);
    public static readonly double MicsPerRoundRev = 1d / Mics.FromMilliseconds(2);
    public static readonly double InitialSendCapacityPerRound = 1; // 1 = 5 MBits/sec.
    public static readonly long InitialWindowMics = Mics.FromMilliseconds(300);
    public static readonly long MinimumWindowMics = Mics.FromMilliseconds(50);

    private class Window
    {
        public Window(long startMics, long durationMics)
        {
            this.Reset(startMics, durationMics);
        }

        public void Reset(long startMics, long durationMics)
        {
            this.StartMics = startMics;
            this.DurationMics = durationMics;

            this.rttCount = 0;
            this.totalRTT = 0;
        }

        internal void AddRTT(long mics)
        {
            this.rttCount++;
            this.totalRTT += mics;
        }

        internal void IncrementResend() => this.ResendCount++;

        public long StartMics { get; private set; }

        public long DurationMics { get; private set; }

        public long EndMics => this.StartMics + this.DurationMics;

        public long MeanRTT
        {
            get
            {
                if (this.rttCount == 0)
                {
                    return InitialWindowMics;
                }
                else
                {
                    return this.totalRTT / this.rttCount;
                }
            }
        }

        public int ResendCount { get; private set; }

        public override string ToString() => $"{this.StartMics / 1_000_000d,0:F3} - {this.EndMics / 1_000_000d,0:F3} ({this.DurationMics / 1_000,0:F0} ms)";

        private int rttCount;
        private long totalRTT;
    }

    public FlowControl(NetTerminal netTerminal)
    {
        this.NetTerminal = netTerminal;

        this.sendCapacityAccumulated = 0;
        this.sendCapacityPerRound = InitialSendCapacityPerRound;

        this.twoPreviousWindow = new(0, 0);
        this.previousWindow = new(0, 0);
        this.currentWindow = new(Mics.GetSystem(), InitialWindowMics);
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
        this.UpdateWindow(currentMics);

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
    {// lock (this.NetTerminal.SyncObject)
        var elapsedMics = currentMics - sentMics;

        // Find window
        if (sentMics >= this.currentWindow.StartMics && sentMics < this.currentWindow.EndMics)
        {
            this.currentWindow.AddRTT(elapsedMics);
        }
        else if (sentMics >= this.previousWindow.StartMics)
        {
            this.previousWindow.AddRTT(elapsedMics);
        }
        else if (sentMics >= this.twoPreviousWindow.StartMics)
        {
            this.twoPreviousWindow.AddRTT(elapsedMics);
        }
        else
        {
            return;
        }
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

    internal bool CheckResend(long sentMics, long currentMics)
    {// lock (NetTerminal.SyncObject)
        var window = this.twoPreviousWindow;
        if (sentMics >= window.StartMics && sentMics < window.EndMics)
        {
            if ((currentMics - sentMics) > window.DurationMics * 2)
            {
                this.twoPreviousWindow.IncrementResend();
                return true;
            }
        }

        return false;
    }

    private void UpdateWindow(long currentMics)
    {
        if (currentMics < this.currentWindow.EndMics)
        {
            return;
        }

        var durationMics = this.previousWindow.MeanRTT;
        if (durationMics < MinimumWindowMics)
        {
            durationMics = MinimumWindowMics;
        }

        var window = this.twoPreviousWindow;
        this.twoPreviousWindow = this.previousWindow;
        this.previousWindow = this.currentWindow;
        window.Reset(this.previousWindow.EndMics, durationMics);
        this.currentWindow = window;

        if (!this.NetTerminal.Terminal.IsAlternative)
        {
            Console.WriteLine($"UpdateWindow: {this.currentWindow.ToString()}");
        }
    }

    private long lastMics;
    private double sendCapacityAccumulated;
    private double sendCapacityPerRound;

    private Window twoPreviousWindow;
    private Window previousWindow;
    private Window currentWindow;
}
