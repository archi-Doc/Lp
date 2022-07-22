// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogFlush
{
    /// <summary>
    /// Writes the buffered logs to the log output.
    /// </summary>
    /// <returns>The number of flushed logs.</returns>
    public Task<int> Flush();
}
