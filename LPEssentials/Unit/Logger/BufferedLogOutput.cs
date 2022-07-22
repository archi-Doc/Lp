// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public abstract class BufferedLogOutput : ILogOutput
{
    public BufferedLogOutput(UnitLogger unitLogger)
    {
        unitLogger.TryRegisterFlush(this);
    }

    /// <summary>
    /// Writes the buffered logs to the log output.
    /// </summary>
    /// <returns>The number of flushed logs.</returns>
    public abstract Task<int> Flush();

    public virtual void Output(LogOutputParameter param)
    {
        throw new NotImplementedException();
    }
}
