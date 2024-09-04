// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Logging;

internal class MachineLogFilter : ILogFilter
{
    public MachineLogFilter(LpBase lpBase)
    {
        this.lpBase = lpBase;
    }

    public ILogWriter? Filter(LogFilterParameter param)
    {
        /*if (param.LogSourceType == typeof(Netsphere.Machines.EssentialNetMachine))
        {
            return this.lpBase.Settings.Flags.LogEssentialNetMachine ? param.OriginalLogger : null;
        }*/

        return param.OriginalLogger;
    }

    private LpBase lpBase;
}
