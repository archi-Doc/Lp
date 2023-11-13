// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Data;

namespace LP.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class LPControlMachine : Machine
{
    private const int IntervalInSeconds = 10;

    public LPControlMachine(ILogger<LPControlMachine> logger, Control control, LPOptions options)
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
        if (this.lifespan > 0)
        {
            this.lifespan -= IntervalInSeconds;
            if (this.lifespan <= 0)
            {
                this.logger.TryGet(LogLevel.Warning)?.Log($"LP will be terminated as the lifespan has reached 0.");
                _ = this.control.TryTerminate(true);
                return StateResult.Terminate;
            }
            else
            {
                var x = (this.lifespan + IntervalInSeconds - 1) / IntervalInSeconds * IntervalInSeconds;
                this.logger.TryGet(LogLevel.Information)?.Log($"LP will shut down in {x} seconds.");
            }
        }

        return StateResult.Continue;
    }

    private ILogger logger;
    private Control control;
    private LPOptions options;
    private long lifespan;
}
