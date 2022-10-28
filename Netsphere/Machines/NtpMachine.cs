// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;

namespace Netsphere.Machines;

[MachineObject(0x5e1f81ca, Group = typeof(SingleGroup<>))]
public partial class NtpMachine : Machine<Identifier>
{
    private const string TimestampFormat = "MM-dd HH:mm:ss.fff K";

    public NtpMachine(ILogger<NtpMachine> logger, BigMachine<Identifier> bigMachine, LPBase lpBase, NetBase netBase, NetControl netControl, NtpCorrection ntpCorrection)
        : base(bigMachine)
    {
        this.logger = logger;
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.LPBase = lpBase;
        this.ntpCorrection = ntpCorrection;

        this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    public LPBase LPBase { get; }

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        await this.ntpCorrection.CorrectAsync(parameter.CancellationToken);

        var timeoffset = this.ntpCorrection.GetTimeoffset();
        if (timeoffset.TimeoffsetCount == 0)
        {
            this.ChangeState(State.SafeHoldMode, false);
            return StateResult.Continue;
        }

        this.logger?.TryGet()?.Log($"Timeoffset {timeoffset.MeanTimeoffset} ms [{timeoffset.TimeoffsetCount}]");

        var corrected = this.ntpCorrection.TryGetCorrectedUtcNow(out var utcNow);
        this.logger?.TryGet()?.Log($"Corrected {corrected}, {utcNow.ToString()}");

        this.SetTimeout(TimeSpan.FromHours(1));
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> SafeHoldMode(StateParameter parameter)
    {
        this.logger?.TryGet(LogLevel.Warning)?.Log($"Safe-hold mode");
        if (await this.ntpCorrection.CheckConnectionAsync(parameter.CancellationToken))
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
