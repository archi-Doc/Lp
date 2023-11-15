// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Time;

namespace Netsphere.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NtpMachine : Machine
{
    private const string TimestampFormat = "MM-dd HH:mm:ss.fff K";

    public NtpMachine(ILogger<NtpMachine> logger, NetBase netBase, NetControl netControl, NtpCorrection ntpCorrection)
    {
        this.logger = logger;
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.ntpCorrection = ntpCorrection;

        this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        await this.ntpCorrection.CorrectAsync(this.CancellationToken).ConfigureAwait(false);

        var timeoffset = this.ntpCorrection.GetTimeoffset();
        if (timeoffset.TimeoffsetCount == 0)
        {
            this.ChangeState(State.SafeHoldMode, false);
            return StateResult.Continue;
        }

        this.logger?.TryGet()?.Log($"Timeoffset {timeoffset.MeanTimeoffset} ms [{timeoffset.TimeoffsetCount}]");

        var corrected = this.ntpCorrection.TryGetCorrectedUtcNow(out var utcNow);
        this.logger?.TryGet()?.Log($"Corrected {corrected}, {utcNow.ToString()}");

        this.TimeUntilRun = TimeSpan.FromHours(1);
        return StateResult.Continue;
    }

    [StateMethod]
    protected async Task<StateResult> SafeHoldMode(StateParameter parameter)
    {
        this.logger?.TryGet(LogLevel.Warning)?.Log($"Safe-hold mode");
        if (await this.ntpCorrection.CheckConnectionAsync(this.CancellationToken).ConfigureAwait(false))
        {
            this.ntpCorrection.ResetHostnames();
            this.ChangeState(State.Initial);
            return StateResult.Continue;
        }

        return StateResult.Continue;
    }

    private ILogger<NtpMachine>? logger;
    private NtpCorrection ntpCorrection;
}
