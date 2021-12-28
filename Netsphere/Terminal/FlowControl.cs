// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class FlowControl
{
    public static readonly long MicsPerRound = Mics.FromMilliseconds(2);
    public static readonly double MicsPerRoundRev = 1d / Mics.FromMilliseconds(2);
    public static readonly double InitialSendCapacityPerRound = 1; // 1 = 5 MBits/sec.
    public static readonly long InitialWindowMics = Mics.FromMilliseconds(300);
    public static readonly long MinimumWindowMics = Mics.FromMilliseconds(50);
    public static readonly double TargetARR = 0.05;
    public static readonly double TargetRTT = 0.2;
    public static readonly double ProbeRatio = 1.5;
    public static readonly double GoRatio = 1.05;
    public static readonly double GoRatio2 = 0.95;
    public static readonly double SendCapacityThreshold = 0.8;

    public enum ControlState
    {
        Probe,
        Go,
    }

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
            this.SendCount = 0;
            this.ResendCount = 0;
            this.SendCapacityPerRound = InitialSendCapacityPerRound;

            this.rttCount = 0;
            this.totalRTT = 0;
        }

        internal void AddRTT(long mics)
        {
            if (mics < Mics.FromMilliseconds(10))
            {// temporary
                return;
            }

            this.rttCount++;
            this.totalRTT += mics;
        }

        internal void IncrementSendCount() => this.SendCount++;

        internal void IncrementResendCount() => this.ResendCount++;

        internal void SetSendCapacityPerRound(double sendCapacityPerRound) => this.SendCapacityPerRound = sendCapacityPerRound;

        public bool IsValid => this.StartMics != 0;

        public long StartMics { get; private set; }

        public long DurationMics { get; private set; }

        public long EndMics => this.StartMics + this.DurationMics;

        public (int Count, long Mean) MeanRTT
        {
            get
            {
                if (this.rttCount == 0)
                {
                    return (0, InitialWindowMics);
                }
                else
                {
                    return (this.rttCount, this.totalRTT / this.rttCount);
                }
            }
        }

        public int SendCount { get; private set; }

        public int ResendCount { get; private set; }

        public double SendCapacityPerRound { get; private set; }

        public override string ToString() => $"{this.StartMics / 1_000_000d,0:F3} - {this.EndMics / 1_000_000d,0:F3} ({this.DurationMics / 1_000,0:F0} ms)";

        private int rttCount;
        private long totalRTT;
    }

    public FlowControl(NetTerminal netTerminal)
    {
        this.NetTerminal = netTerminal;
        this.State = ControlState.Probe;

        this.sendCapacityAccumulated = 0;
        this.resendMics = InitialWindowMics * 2;
        this.minRTT = double.MaxValue;

        this.twoPreviousWindow = new(0, 0);
        this.previousWindow = new(0, 0);
        this.currentWindow = new(Mics.GetSystem(), InitialWindowMics);
    }

    public NetTerminal NetTerminal { get; }

    public ControlState State { get; private set; }

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
        this.sendCapacityAccumulated += this.currentWindow.SendCapacityPerRound * roundElapsed;
        var ceiling = Math.Ceiling(this.currentWindow.SendCapacityPerRound);
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

    internal void ReportSend(long currentMics)
    {// lock (this.NetTerminal.SyncObject)
        // Find window
        if (currentMics >= this.currentWindow.StartMics && currentMics < this.currentWindow.EndMics)
        {
            this.currentWindow.IncrementSendCount();
        }
        else if (currentMics >= this.previousWindow.StartMics)
        {
            this.previousWindow.IncrementSendCount();
        }
        else if (currentMics >= this.twoPreviousWindow.StartMics)
        {
            this.twoPreviousWindow.IncrementSendCount();
        }
        else
        {
            return;
        }
    }

    internal void RentSendCapacity(out int sendCapacity)
    {// lock (NetTerminal.SyncObject)
        sendCapacity = (int)this.sendCapacityAccumulated;
        this.sendPerWindow += sendCapacity;
        this.sendCapacityAccumulated -= sendCapacity;
    }

    internal void ReturnSendCapacity(int sendCapacity)
    {// lock (NetTerminal.SyncObject)
        this.sendPerWindow -= sendCapacity;
        this.sendCapacityAccumulated += sendCapacity;
    }

    internal bool CheckResend(long sentMics, long currentMics)
    {// lock (NetTerminal.SyncObject)
        var window = this.twoPreviousWindow;
        if ((currentMics - sentMics) > this.resendMics)
        {
            if (sentMics >= window.StartMics && sentMics < window.EndMics)
            {
                window.IncrementResendCount();
            }

            return true;
        }

        return false;
    }

    private void UpdateWindow(long currentMics)
    {
        if (currentMics < this.currentWindow.EndMics)
        {
            return;
        }

        // Mean RTT
        var rtt = this.previousWindow.MeanRTT;
        if (rtt.Count == 0 && this.lastMeanRTT > 0)
        {
            rtt.Mean = this.lastMeanRTT;
        }

        Console.WriteLine(rtt.Mean);
        if (rtt.Mean < MinimumWindowMics)
        {
            rtt.Mean = MinimumWindowMics;
        }

        /*else if (this.previousWindow.IsValid)
        {// Add RTT sample 2
            this.rttFunction2.TryAdd(this.previousWindow.SendCapacityPerRound, rtt.Mean);
        }*/

        var sendCapacityPerRound = this.CalculateSendCapacityPerRound();
        var resent = this.twoPreviousWindow.ResendCount;

        var window = this.twoPreviousWindow;
        this.twoPreviousWindow = this.previousWindow;
        this.previousWindow = this.currentWindow;
        window.Reset(this.previousWindow.EndMics, rtt.Mean);
        window.SetSendCapacityPerRound(sendCapacityPerRound);
        this.currentWindow = window;
        if (rtt.Count > 0)
        {
            this.lastMeanRTT = rtt.Mean;
        }

        this.sendPerWindow = 0;
        this.resendMics = rtt.Mean * 2;

        if (!this.NetTerminal.Terminal.IsAlternative)
        {
            Console.WriteLine($"RTT: {rtt.Mean / 1000d,0:F0} ({rtt.Count}), Resent: {resent} => {sendCapacityPerRound,0:F2}");
            // Console.WriteLine($"UpdateWindow: {this.currentWindow.ToString()}");
        }
    }

    private double CalculateSendCapacityPerRound()
    {
        if (!this.twoPreviousWindow.IsValid)
        {
            return this.currentWindow.SendCapacityPerRound * ProbeRatio;
        }

        // minRTT
        var meanRTT = this.twoPreviousWindow.MeanRTT;
        if (meanRTT.Count > 0 && this.minRTT > meanRTT.Mean)
        {
            this.minRTT = meanRTT.Mean;
        }

        var window = this.twoPreviousWindow;
        var sendCapacity = window.SendCapacityPerRound * window.DurationMics * MicsPerRoundRev;
        if (this.sendPerWindow < (sendCapacity * SendCapacityThreshold))
        {
            Console.WriteLine($"Under, SendPerWindow: {this.sendPerWindow}, Send: {window.SendCount}, Capacity: {sendCapacity:F0}");
            return this.currentWindow.SendCapacityPerRound;
        }

        Console.WriteLine($"SendPerWindow: {this.sendPerWindow}, Send: {window.SendCount}, Capacity: {sendCapacity:F0}");

        var sendResend = window.SendCount + window.ResendCount;
        if (sendResend == 0)
        {
            sendResend = 1;
        }

        var arr = window.ResendCount / (double)sendResend;
        var rtt = (meanRTT.Mean - this.minRTT) / this.minRTT;
        if (this.State == ControlState.Probe)
        {// Probe
            if (arr > TargetARR || rtt > TargetRTT)
            {
                this.State = ControlState.Go;
                Console.WriteLine("ControlState.Go");
                return this.currentWindow.SendCapacityPerRound / ProbeRatio / ProbeRatio;
            }
            else
            {
                return this.currentWindow.SendCapacityPerRound * ProbeRatio;
            }
        }

        // ControlState.Go
        if (!this.NetTerminal.Terminal.IsAlternative)
        {
            Console.WriteLine($"Send: {this.twoPreviousWindow.SendCount}, Resend: {this.twoPreviousWindow.ResendCount}, Capacity: {sendCapacity:F0}");
        }

        var next = window.SendCapacityPerRound;
        if (arr > TargetARR || rtt > TargetRTT)
        {
            next *= GoRatio2;
        }
        else if (rtt < (TargetRTT * 0.5))
        {
            next *= GoRatio;
        }

        return ((next * 2d) + this.twoPreviousWindow.SendCapacityPerRound + this.previousWindow.SendCapacityPerRound) * 0.25d;

        /*if (arr < MinimumARR)
        {
            arr = MinimumARR;
        }*/

        // Add ARR sample
        /*this.arrFunction.TryAdd(this.twoPreviousWindow.SendCapacityPerRound, arr);

        // Add RTT sample
        var rtt = this.twoPreviousWindow.MeanRTT;
        if (rtt.Count > 0)
        {
            this.rttFunction.TryAdd(this.twoPreviousWindow.SendCapacityPerRound, rtt.Mean);
        }

        // RTT function
        if (this.rttFunction.TryGet(out var rttOptimal))
        {
        }

        if (this.rttFunction2.TryGet(out var rttOptimal2))
        {
        }

        if (this.arrFunction.TryGet(out var arrOptimal))
        {
        }*/
    }

    private long lastMics;
    private int sendPerWindow;
    private double sendCapacityAccumulated;
    private long resendMics;
    private long lastMeanRTT;
    private double minRTT;

    private Window twoPreviousWindow;
    private Window previousWindow;
    private Window currentWindow;
    // private FlowFunction arrFunction = new(FlowFunctionType.ARR);
    // private FlowFunction rttFunction = new(FlowFunctionType.RTT);
    // private FlowFunction rttFunction2 = new(FlowFunctionType.RTT);
}
