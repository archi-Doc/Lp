// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

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
    public static readonly double SendCapacityThreshold = 0.7;
    public static readonly double MinRTTThresholdRatio = 0.7;

    public enum ControlState
    {
        Probe,
        Go,
    }

    private class Window
    {
        public Window(FlowControl flowControl, long startMics, long durationMics)
        {
            this.FlowControl = flowControl;
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
            if (mics < this.FlowControl.minRTTThreshold)
            {
                return;
            }

            this.rttCount++;
            this.totalRTT += mics;
        }

        internal void IncrementSendCount() => this.SendCount++;

        internal void IncrementResendCount() => this.ResendCount++;

        internal void SetSendCapacityPerRound(double sendCapacityPerRound) => this.SendCapacityPerRound = sendCapacityPerRound;

        public FlowControl FlowControl { get; }

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
                    var rtt = this.totalRTT / this.rttCount;
                    if (rtt < MinimumWindowMics)
                    {
                        rtt = MinimumWindowMics;
                    }

                    return (this.rttCount, rtt);
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
        this.NetBase = netTerminal.Terminal.NetBase;
        this.NetTerminal = netTerminal;
        this.State = ControlState.Probe;

        this.sendCapacityAccumulated = 0;
        this.resendMics = InitialWindowMics * 2;
        this.minRTT = double.MaxValue;
        this.minRTTThreshold = 0;

        this.twoPreviousWindow = new(this, 0, 0);
        this.previousWindow = new(this, 0, 0);
        this.currentWindow = new(this, Mics.GetSystem(), InitialWindowMics);
    }

    public NetBase NetBase { get; }

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
        if (currentMics >= this.currentWindow.EndMics)
        {
            return;
        }
        else if (sentMics >= this.currentWindow.StartMics)
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
    }

    internal void ReportSend(long currentMics)
    {// lock (this.NetTerminal.SyncObject)
        // Find window
        if (currentMics >= this.currentWindow.EndMics)
        {
            return;
        }
        else if (currentMics >= this.currentWindow.StartMics)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CheckResend(long sentMics, long currentMics)
    {// lock (NetTerminal.SyncObject)
        if ((currentMics - sentMics) > this.resendMics)
        {
            var window = this.twoPreviousWindow;
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

        if (this.NetBase.Log.FlowControl && !this.NetTerminal.Terminal.IsAlternative)
        {
            // Logger.Console.Information($"RTT: {rtt.Mean / 1000d,0:F0} ({rtt.Count}), Resent: {resent} => {sendCapacityPerRound,0:F2}");
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
            this.minRTTThreshold = this.minRTT * MinRTTThresholdRatio;
        }

        var window = this.twoPreviousWindow;
        var sendCapacity = window.SendCapacityPerRound * window.DurationMics * MicsPerRoundRev;
        if (this.NetBase.Log.FlowControl && !this.NetTerminal.Terminal.IsAlternative)
        {
            // Logger.Console.Information($"SendPerWindow: {this.sendPerWindow}, Send: {window.SendCount}/{sendCapacity:F0}");
        }

        var sendResend = window.SendCount + window.ResendCount;
        if (sendResend == 0)
        {
            sendResend = 1;
        }

        var arr = window.ResendCount / (double)sendResend;
        var rtt = (meanRTT.Mean - this.minRTT) / this.minRTT;
        if (meanRTT.Count == 0)
        {
            rtt = 0;
        }

        if (this.State == ControlState.Probe)
        {// Probe
            if (arr > TargetARR || rtt > TargetRTT)
            {
                this.State = ControlState.Go;
                if (this.NetBase.Log.FlowControl && !this.NetTerminal.Terminal.IsAlternative)
                {
                    // Logger.Console.Information($"ControlState.Go ARR:{arr:F2}, RTT:{rtt:F2}");
                }

                return this.currentWindow.SendCapacityPerRound / ProbeRatio / ProbeRatio;
            }
        }

        if (window.SendCount < (sendCapacity * SendCapacityThreshold))
        {
            if (this.NetBase.Log.FlowControl && !this.NetTerminal.Terminal.IsAlternative)
            {
                // Logger.Console.Information("Under");
            }

            return this.currentWindow.SendCapacityPerRound;
        }

        if (this.State == ControlState.Probe)
        {// Probe
            return this.currentWindow.SendCapacityPerRound * ProbeRatio;
        }

        // ControlState.Go

        if (this.NetBase.Log.FlowControl && !this.NetTerminal.Terminal.IsAlternative)
        {
            // Logger.Console.Information($"Send: {this.twoPreviousWindow.SendCount}, Resend: {this.twoPreviousWindow.ResendCount}, Capacity: {sendCapacity:F0}");
            // Logger.Console.Information($"ARR:{arr:F2}, RTT:{rtt:F2}");
        }

        var next = this.currentWindow.SendCapacityPerRound;
        if (arr > TargetARR || rtt > TargetRTT)
        {
            next *= GoRatio2;
        }
        else if (rtt < (TargetRTT * 0.5))
        {
            next *= GoRatio;
        }

        return next;

        // return ((next * 2d) + this.twoPreviousWindow.SendCapacityPerRound + this.previousWindow.SendCapacityPerRound) * 0.25d;

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
    private double minRTTThreshold;

    private Window twoPreviousWindow;
    private Window previousWindow;
    private Window currentWindow;
    // private FlowFunction arrFunction = new(FlowFunctionType.ARR);
    // private FlowFunction rttFunction = new(FlowFunctionType.RTT);
    // private FlowFunction rttFunction2 = new(FlowFunctionType.RTT);
}
