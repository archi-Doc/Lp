// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Data;

namespace LP.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class LpControlMachine : Machine
{
    private const int IntervalInSeconds = 10;

    public LpControlMachine(ILogger<LpControlMachine> logger, Control control, LpOptions options)
    {
        this.logger = logger;
        this.control = control;
        this.options = options;
        this.DefaultTimeout = TimeSpan.FromSeconds(IntervalInSeconds);

        this.lifespan = -1;
        if (this.options.Lifespan > 0)
        {
            this.lifespan = this.options.Lifespan;
        }
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        if (this.lifespan >= 0)
        {
            this.lifespan -= IntervalInSeconds;
            if (this.lifespan < 0)
            {
                this.logger.TryGet(LogLevel.Warning)?.Log($"LP is terminating because the specified time has elapsed.");
                _ = this.control.TryTerminate(true);
                return StateResult.Terminate;
            }
            else
            {
                var x = (this.lifespan + IntervalInSeconds) / IntervalInSeconds * IntervalInSeconds;
                this.logger.TryGet(LogLevel.Information)?.Log($"LP will terminate in {x} seconds.");
            }
        }

        return StateResult.Continue;
    }

    private readonly ILogger logger;
    private readonly Control control;
    private readonly LpOptions options;
    private long lifespan;
}
