// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;

namespace Lp.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class LpControlMachine : Machine
{
    private const int IntervalInSeconds = 1;
    private const int DisplayIntervalInSeconds = 10;

    #region FieldAndProperty

    private readonly ILogger logger;
    private readonly Control control;
    private readonly LpOptions options;
    private long lifespan;

    #endregion

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
        var result = this.ProcessLifespan();
        if (result != StateResult.Continue)
        {
            return result;
        }

        return StateResult.Continue;
    }

    private StateResult ProcessLifespan()
    {
        if (this.lifespan >= 0)
        {
            this.lifespan -= IntervalInSeconds;
            if (this.lifespan <= 0)
            {
                this.logger.TryGet(LogLevel.Warning)?.Log($"Lp is terminating because the specified time has elapsed.");
                _ = this.control.TryTerminate(true);
                return StateResult.Terminate;
            }
            else if (this.lifespan % DisplayIntervalInSeconds == 0)
            {
                var x = this.lifespan / DisplayIntervalInSeconds * DisplayIntervalInSeconds;
                this.logger.TryGet(LogLevel.Information)?.Log($"Lp will terminate in {x} seconds.");
            }
        }

        return StateResult.Continue;
    }
}
